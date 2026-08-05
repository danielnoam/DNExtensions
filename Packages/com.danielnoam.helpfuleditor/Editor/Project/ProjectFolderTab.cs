using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Opens a folder in a second Project window, the way a middle-click opens a link in a new tab.
    /// Unity has the machinery for this — every Project window can be told which folder to show — but
    /// no menu item or shortcut reaches it, and ProjectBrowser is internal.
    /// </summary>
    internal static class ProjectFolderTab
    {
        private static readonly Type ProjectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");

        public static void Open(string folderPath)
        {
            if (!IsFolder(folderPath)) return;

            // Captured now rather than in the deferred call: by then the mouse has moved on and the
            // window that was clicked is no longer the one under the cursor.
            EditorWindow source = EditorWindow.mouseOverWindow ? EditorWindow.mouseOverWindow : EditorWindow.focusedWindow;
            Object dockArea = HelpfulEditorDockArea.Of(source);

            // Deferred: creating and focusing a window from inside the click that asked for it
            // reshuffles focus while the event is still being dispatched.
            EditorApplication.delayCall += () => Create(folderPath, dockArea, HelpfulEditorSettings.Project.autoDock);
        }

        /// <summary>
        /// Opens the folder as a tab of a particular dock area. Used by the drop handler, where the
        /// dock area was chosen by the user rather than inferred — so it docks regardless of the
        /// auto-dock preference, which is about where a middle-click should put things.
        /// </summary>
        public static void OpenInDockArea(string folderPath, Object dockArea)
        {
            if (!IsFolder(folderPath)) return;

            EditorApplication.delayCall += () => Create(folderPath, dockArea, dock: true);
        }

        private static bool IsFolder(string folderPath)
        {
            return ProjectBrowserType != null && !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);
        }

        private static void Create(string folderPath, Object dockArea, bool dock)
        {
            if (ScriptableObject.CreateInstance(ProjectBrowserType) is not EditorWindow window) return;

            if (!dock || !HelpfulEditorDockArea.AddTab(dockArea, window)) window.Show();

            window.Focus();

            // A second hop: the browser builds its trees on its first OnGUI, and asking it to show a
            // folder before that leaves it on whatever the last window was looking at.
            EditorApplication.delayCall += () => ShowFolder(window, folderPath);
        }

        private static void ShowFolder(EditorWindow window, string folderPath)
        {
            if (!window || string.IsNullOrEmpty(folderPath)) return;

            if (!HelpfulEditorProjectWindow.ShowFolder(window, folderPath))
            {
                // Without the internal call the folder can at least be selected, which lands the new
                // window on it in one-column mode.
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
                if (!folder) return;

                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
                return;
            }

            // Locked, so the window stays the folder's window rather than drifting off with the next
            // selection — which is also what lets it be named after the folder rather than reading
            // "Project" like every other one.
            if (HelpfulEditorSettings.Project.lockFolderWindows) HelpfulEditorProjectWindow.SetLocked(window, true);

            // The title and the new-folder button are both driven by polling, and this is the moment
            // they are both about to be wrong — so they are told rather than left to notice.
            HelpfulEditorWindowTitles.RequestRefresh();
            ProjectCreateFolderButton.RequestRefresh();
        }
    }
}
