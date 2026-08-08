using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Dropping something on a dock area's tab strip opens it there as its own tab, the way dragging
    /// a file onto a browser's tab bar opens it in a new tab. A folder becomes a Project window
    /// showing it; anything else becomes a Properties window for it.
    ///
    /// The tab strip is drawn by the dock area itself and has no IMGUI callback to hook, so the drag
    /// is caught on the host's visual tree and filtered by position. Handlers are registered once per
    /// dock area — every tab in one shares a panel — and re-registered as dock areas come and go.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectTabDrop
    {
        /// <summary>Extra reach below the tabs, so the drop does not demand pixel accuracy.</summary>
        private const float DropZonePadding = 6f;

        private const double RefreshInterval = 1.0;

        private static readonly Dictionary<Object, VisualElement> Registered = new Dictionary<Object, VisualElement>();

        private static double _lastRefresh;
        private static bool _warned;

        static ProjectTabDrop()
        {
            EditorApplication.update -= Refresh;
            EditorApplication.update += Refresh;
        }

        private static void Refresh()
        {
            if (EditorApplication.timeSinceStartup - _lastRefresh < RefreshInterval) return;
            _lastRefresh = EditorApplication.timeSinceStartup;

            PruneDeadDockAreas();

            if (!AnyDropEnabled()) return;

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                Object dockArea = HelpfulEditorDockArea.Of(window);
                if (!dockArea || Registered.ContainsKey(dockArea)) continue;

                Register(dockArea);
            }
        }

        private static void Register(Object dockArea)
        {
            try
            {
                EditorWindow host = HelpfulEditorDockArea.ActiveTab(dockArea);

                VisualElement tree = host ? host.rootVisualElement?.panel?.visualTree : null;
                if (tree == null) return;

                // Trickled down so the drop cursor is set before the window under the tabs claims the
                // drag and shows its own.
                tree.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);

                // Bubbled, so a window that wants the drop for itself gets first refusal — the drop
                // zone overlaps whatever is directly beneath the tabs.
                tree.RegisterCallback<DragPerformEvent>(OnDragPerform);

                Registered[dockArea] = tree;
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// A dock area dies when its last tab closes, taking its visual tree with it. The entry is
        /// dropped so the same key cannot go stale and block a genuinely new dock area later.
        /// </summary>
        private static void PruneDeadDockAreas()
        {
            List<Object> dead = null;

            foreach (KeyValuePair<Object, VisualElement> entry in Registered)
            {
                if (entry.Key) continue;

                dead ??= new List<Object>();
                dead.Add(entry.Key);
            }

            if (dead == null) return;

            foreach (Object key in dead) Registered.Remove(key);
        }

        private static void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (!IsTabStripDrop(evt.currentTarget as VisualElement, evt.mousePosition)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private static void OnDragPerform(DragPerformEvent evt)
        {
            VisualElement tree = evt.currentTarget as VisualElement;
            if (!IsTabStripDrop(tree, evt.mousePosition)) return;

            Object dockArea = DockAreaOf(tree);
            if (!dockArea) return;

            ProjectModuleSettings settings = HelpfulEditorSettings.Project;

            DragAndDrop.AcceptDrag();
            evt.StopPropagation();

            foreach (Object dragged in DragAndDrop.objectReferences)
            {
                if (!dragged) continue;

                if (TryGetFolderPath(dragged, out string folderPath))
                {
                    if (settings.folderDropCreatesTabEnabled) ProjectFolderTab.OpenInDockArea(folderPath, dockArea);
                    continue;
                }

                if (settings.objectDropOpensPropertiesEnabled) ProjectPropertiesTab.OpenInDockArea(dragged, dockArea);
            }
        }

        private static bool IsTabStripDrop(VisualElement tree, Vector2 mousePosition)
        {
            if (tree == null) return false;
            if (!AnyDropEnabled()) return false;

            Object dockArea = DockAreaOf(tree);
            if (!dockArea) return false;

            // The visual tree starts at the top of the dock area, so the tab strip is the band from
            // its origin down.
            if (mousePosition.y > HelpfulEditorDockArea.TabStripHeight(dockArea) + DropZonePadding) return false;

            return HasHandledObject();
        }

        private static bool AnyDropEnabled()
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;

            return settings.moduleEnabled && (settings.folderDropCreatesTabEnabled || settings.objectDropOpensPropertiesEnabled);
        }

        /// <summary>
        /// Whether the drag holds something this would act on, so a drag of only folders with folder
        /// drops turned off still passes through to whatever is underneath.
        /// </summary>
        private static bool HasHandledObject()
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;

            foreach (Object dragged in DragAndDrop.objectReferences)
            {
                if (!dragged) continue;

                bool folder = TryGetFolderPath(dragged, out string _);

                if (folder && settings.folderDropCreatesTabEnabled) return true;
                if (!folder && settings.objectDropOpensPropertiesEnabled) return true;
            }

            return false;
        }

        /// <summary>
        /// False for anything that is not a project folder, which includes scene objects — they have
        /// no asset path at all, and a Properties window is exactly what they want.
        /// </summary>
        private static bool TryGetFolderPath(Object dragged, out string path)
        {
            path = AssetDatabase.GetAssetPath(dragged);

            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }

        private static Object DockAreaOf(VisualElement tree)
        {
            foreach (KeyValuePair<Object, VisualElement> entry in Registered)
            {
                if (entry.Value == tree && entry.Key) return entry.Key;
            }

            return null;
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Dropping onto tab strips is unavailable on this Unity version. ({e.Message})");
        }
    }
}
