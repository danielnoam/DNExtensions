using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Dropping a folder on a dock area's tab strip opens it there as its own Project tab, the way
    /// dragging a file onto a browser's tab bar opens it in a new tab.
    ///
    /// The tab strip is drawn by the dock area itself and has no IMGUI callback to hook, so the drag
    /// is caught on the host's visual tree and filtered by position. Handlers are registered once per
    /// dock area — every tab in one shares a panel — and re-registered as dock areas come and go.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectFolderDrop
    {
        /// <summary>Extra reach below the tabs, so the drop does not demand pixel accuracy.</summary>
        private const float DropZonePadding = 6f;

        private const double RefreshInterval = 1.0;

        private static readonly Dictionary<Object, VisualElement> Registered = new Dictionary<Object, VisualElement>();

        private static double _lastRefresh;
        private static bool _warned;

        static ProjectFolderDrop()
        {
            EditorApplication.update -= Refresh;
            EditorApplication.update += Refresh;
        }

        private static void Refresh()
        {
            if (EditorApplication.timeSinceStartup - _lastRefresh < RefreshInterval) return;
            _lastRefresh = EditorApplication.timeSinceStartup;

            PruneDeadDockAreas();

            if (!HelpfulEditorSettings.Project.moduleEnabled) return;
            if (!HelpfulEditorSettings.Project.folderDropCreatesTabEnabled) return;

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
            if (!IsFolderDrop(evt.currentTarget as VisualElement, evt.mousePosition)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private static void OnDragPerform(DragPerformEvent evt)
        {
            VisualElement tree = evt.currentTarget as VisualElement;
            if (!IsFolderDrop(tree, evt.mousePosition)) return;

            Object dockArea = DockAreaOf(tree);
            if (!dockArea) return;

            DragAndDrop.AcceptDrag();
            evt.StopPropagation();

            foreach (Object dragged in DragAndDrop.objectReferences)
            {
                string path = AssetDatabase.GetAssetPath(dragged);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) continue;

                ProjectFolderTab.OpenInDockArea(path, dockArea);
            }
        }

        private static bool IsFolderDrop(VisualElement tree, Vector2 mousePosition)
        {
            if (tree == null) return false;
            if (!HelpfulEditorSettings.Project.moduleEnabled) return false;
            if (!HelpfulEditorSettings.Project.folderDropCreatesTabEnabled) return false;

            Object dockArea = DockAreaOf(tree);
            if (!dockArea) return false;

            // The visual tree starts at the top of the dock area, so the tab strip is the band from
            // its origin down.
            if (mousePosition.y > HelpfulEditorDockArea.TabStripHeight(dockArea) + DropZonePadding) return false;

            return HasFolder();
        }

        private static bool HasFolder()
        {
            foreach (Object dragged in DragAndDrop.objectReferences)
            {
                string path = AssetDatabase.GetAssetPath(dragged);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path)) return true;
            }

            return false;
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
            Debug.LogWarning($"[HelpfulEditor] Dropping folders onto tab strips is unavailable on this Unity version. ({e.Message})");
        }
    }
}
