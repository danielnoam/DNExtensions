using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Opens a folder as its own tab, the way a middle-click opens a link in a new browser tab.
    ///
    /// The tab is a ProjectFolderWindow — one folder and nothing else. On a Unity version whose object
    /// view cannot be hosted, this falls back to what it used to do: a second Project window, locked
    /// to the folder so it does not follow the selection.
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
            EditorApplication.delayCall += () => Create(folderPath, dockArea, dock: true);
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

        /// <summary>
        /// Opens the folder floating. For the Quick Object Window, which is a look rather than a place
        /// to work — everything else it opens floats too, and docking a tab into the strip you
        /// happened to be hovering is not what that gesture means.
        /// </summary>
        public static void OpenFloating(string folderPath)
        {
            if (!IsFolder(folderPath)) return;

            EditorApplication.delayCall += () => Create(folderPath, null, dock: false);
        }

        private static bool IsFolder(string folderPath)
        {
            return !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);
        }

        private static void Create(string folderPath, Object dockArea, bool dock)
        {
            EditorWindow window = ProjectFolderWindow.Supported
                ? ProjectFolderWindow.Create(folderPath)
                : CreateProjectBrowser();

            if (!window) return;

            if (!dock || !HelpfulEditorDockArea.AddTab(dockArea, window)) window.Show();

            window.Focus();

            // Only the fallback needs the second hop. A folder window is told its folder before it is
            // shown and holds it as its own state; a browser builds its trees on its first OnGUI, and
            // being asked for a folder before that leaves it on whatever the last window looked at.
            if (window is not ProjectFolderWindow) EditorApplication.delayCall += () => ShowFolder(window, folderPath);
        }

        private static EditorWindow CreateProjectBrowser()
        {
            if (ProjectBrowserType == null) return null;

            return ScriptableObject.CreateInstance(ProjectBrowserType) as EditorWindow;
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

            // A borrowed Project window has no identity of its own, so it is locked to stop it
            // drifting off with the next selection — which is also what lets the window titles name
            // it after the folder rather than leaving it reading "Project".
            HelpfulEditorProjectWindow.SetLocked(window, true);

            // Driven by polling, and this is the moment it is about to be wrong — so it is told
            // rather than left to notice.
            ProjectCreateFolderButton.RequestRefresh();
        }
    }
}
