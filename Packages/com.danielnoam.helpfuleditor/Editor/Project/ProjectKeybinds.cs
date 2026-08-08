using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Hover keybinds for the Project window. Every action targets the asset under the mouse rather
    /// than the selection, and the expand/collapse defaults intentionally match the Hierarchy's.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectKeybinds
    {
        private static string _pendingQuickObjectPath;

        static ProjectKeybinds()
        {
            GlobalKeyCapture.KeyEvent -= OnKeyEvent;
            GlobalKeyCapture.KeyEvent += OnKeyEvent;
        }

        private static void OnKeyEvent()
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled) return;
            if (EditorGUIUtility.editingTextField) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown) return;

            // Closing and reopening target whatever has focus, so they are handled before the
            // mouse-over-the-Project-window gate the row actions need.
            if (settings.closeWindowKey.Matches(evt))
            {
                if (CloseFocusedWindow()) evt.Use();
                return;
            }

            if (settings.reopenWindowKey.Matches(evt))
            {
                if (HelpfulEditorWindowHistory.ReopenLast()) evt.Use();
                return;
            }

            if (!HelpfulEditorWindows.MouseOverProject) return;

            if (settings.collapseAllKey.Matches(evt))
            {
                ProjectExpandQueue.CollapseAll();
                evt.Use();
                return;
            }

            // Every remaining action needs a row under the cursor. Without a target the event is
            // deliberately left unconsumed, so Unity's own shortcut on that key still runs —
            // otherwise Ctrl+R over empty Project space would swallow the domain reload.
            string path = ProjectModule.HoveredPath;

            if (string.IsNullOrEmpty(path))
            {
                // A row with no asset — the Packages root, say — can still be expanded by position.
                bool expandPressed = settings.expandCollapseKey.Matches(evt) || settings.expandCollapseRecursiveKey.Matches(evt);
                if (expandPressed && HelpfulEditorTreeReflection.ToggleProjectExpandedAtRow(ProjectModule.HoveredRowY)) evt.Use();

                return;
            }

            // Closes every branch that is not on the way to the hovered row. Unlike Collapse
            // Everything, what it closes keeps its expanded state, so reopening a branch later finds
            // it as it was left.
            if (settings.isolateKey.Matches(evt))
            {
                ProjectExpandQueue.Isolate(path);
                evt.Use();
                return;
            }

            // Not gated on folders: an FBX, a prefab or any asset with sub-assets is an expandable
            // row too. Rows with nothing under them are already a no-op further down.
            if (settings.expandCollapseRecursiveKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.ToggleProjectExpanded(path, true, ProjectModule.HoveredInListArea);
                evt.Use();
                return;
            }

            if (settings.expandCollapseKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.ToggleProjectExpanded(path, false, ProjectModule.HoveredInListArea);
                evt.Use();
                return;
            }

            if (settings.revealInFinderKey.Matches(evt))
            {
                RevealInFileManager(path);
                evt.Use();
                return;
            }

            if (settings.quickObjectWindowKey.Matches(evt))
            {
                OpenQuickObjectWindow(path);
                evt.Use();
            }
        }

        /// <summary>
        /// The window is snapshotted before it goes, so Reopen Closed Window can bring it back where
        /// it was. That is what makes the exclusion list optional rather than a safety net — it now
        /// defaults to empty, and exists only for anyone who would rather a given window never
        /// respond to the shortcut at all.
        /// </summary>
        private static bool CloseFocusedWindow()
        {
            EditorWindow window = EditorWindow.focusedWindow ? EditorWindow.focusedWindow : EditorWindow.mouseOverWindow;
            if (!window) return false;

            string typeName = window.GetType().Name;
            foreach (string excluded in HelpfulEditorSettings.Project.closeWindowExcludedTypes)
            {
                if (string.Equals(typeName, excluded, StringComparison.OrdinalIgnoreCase)) return false;
            }

            HelpfulEditorWindowHistory.Remember(window);

            window.Close();
            return true;
        }

        /// <summary>
        /// Mouse bindings never reach the global key hook, so they are matched here against the row
        /// actually under the click. That also sidesteps hover staleness — the row rect is the
        /// target, not a cached hover from the previous repaint.
        /// </summary>
        public static void HandleRowInput(string path, Rect rowRect)
        {
            ProcessPendingQuickObjectWindow();

            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            if (EditorGUIUtility.editingTextField) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.MouseDown) return;

            // Navigation applies to the window, not to a row, so it is matched before the rect test.
            // The first row to see the event consumes it; the rest then see EventType.Used.
            if (settings.navigateBackKey.Matches(evt))
            {
                if (ProjectFolderHistory.Back()) evt.Use();
                return;
            }

            if (settings.navigateForwardKey.Matches(evt))
            {
                if (ProjectFolderHistory.Forward()) evt.Use();
                return;
            }

            if (evt.clickCount != 1) return;
            if (!rowRect.Contains(evt.mousePosition)) return;
            if (string.IsNullOrEmpty(path)) return;

            // A folder gets a second Project window showing it; anything else has nothing to show
            // that way, so it gets its Properties window instead — the same split the tab strip drop
            // makes, reached with the wheel button rather than a drag.
            if (settings.openInNewTabKey.IsMouseButton && settings.openInNewTabKey.Matches(evt))
            {
                if (AssetDatabase.IsValidFolder(path)) ProjectFolderTab.Open(path);
                else ProjectPropertiesTab.Open(AssetDatabase.LoadAssetAtPath<Object>(path));

                evt.Use();
                return;
            }

            if (!settings.quickObjectWindowKey.IsMouseButton) return;
            if (!settings.quickObjectWindowKey.Matches(evt)) return;

            _pendingQuickObjectPath = path;
            evt.Use();
        }

        /// <summary>
        /// Opening on MouseDown meant the MouseUp that followed landed back on the Project window
        /// and dismissed the popup in the same click, so the request waits for the click to finish.
        /// </summary>
        private static void ProcessPendingQuickObjectWindow()
        {
            if (string.IsNullOrEmpty(_pendingQuickObjectPath)) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.MouseUp) return;

            string path = _pendingQuickObjectPath;
            _pendingQuickObjectPath = null;

            evt.Use();
            OpenQuickObjectWindow(path);
        }

        /// <summary>
        /// Prefers Unity's own Properties window, which is a real dockable editor window with
        /// proper chrome, and falls back to the suite's popup only if that menu item is missing.
        /// For a prefab this shows the root GameObject's properties rather than entering Prefab Mode
        /// — LoadMainAssetAtPath is what makes that the case.
        /// </summary>
        private static void OpenQuickObjectWindow(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!asset) return;

            // Deferred: opening a window reshuffles focus, which is not safe from inside the event
            // that triggered it.
            Vector2 screenPosition = HelpfulEditorQuickEditWindow.MouseScreenPosition();
            EditorApplication.delayCall += () =>
            {
                if (TryOpenNativePropertiesWindow(asset)) return;

                HelpfulEditorQuickEditWindow.Open(asset, screenPosition);
            };
        }

        /// <summary>
        /// The Properties menu item acts on the selection, so the current selection is put back
        /// afterwards — opening a preview should not be a selection change.
        /// </summary>
        private static bool TryOpenNativePropertiesWindow(Object asset)
        {
            Object[] previousSelection = Selection.objects;

            try
            {
                Selection.activeObject = asset;
                return EditorApplication.ExecuteMenuItem("Assets/Properties...");
            }
            finally
            {
                Selection.objects = previousSelection;
            }
        }

        /// <summary>
        /// Reveals the whole selection when the hovered asset is part of it, otherwise just the
        /// hovered one — same rule the Hierarchy's Toggle Active uses.
        /// </summary>
        private static void RevealInFileManager(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            foreach (Object selected in Selection.objects)
            {
                if (AssetDatabase.GetAssetPath(selected) != path) continue;

                foreach (Object asset in Selection.objects)
                {
                    string selectedPath = AssetDatabase.GetAssetPath(asset);
                    if (!string.IsNullOrEmpty(selectedPath)) EditorUtility.RevealInFinder(selectedPath);
                }

                return;
            }

            EditorUtility.RevealInFinder(path);
        }

    }
}
