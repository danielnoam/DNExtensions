using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Opens an object's Properties window as a tab of a particular dock area, which is what dropping
    /// it on a tab strip asks for. The folder equivalent is ProjectFolderTab.
    /// </summary>
    internal static class ProjectPropertiesTab
    {
        /// <summary>
        /// Opens beside the window the click came from, following the auto-dock preference — the
        /// object equivalent of middle-clicking a folder.
        /// </summary>
        public static void Open(Object target)
        {
            if (!target) return;

            // Captured now rather than in the deferred call: by then the mouse has moved on and the
            // window that was clicked is no longer the one under the cursor.
            EditorWindow source = EditorWindow.mouseOverWindow ? EditorWindow.mouseOverWindow : EditorWindow.focusedWindow;
            Object dockArea = HelpfulEditorDockArea.Of(source);

            EditorApplication.delayCall += () => Create(target, dockArea, dock: true);
        }

        /// <summary>
        /// Opens as a tab of a particular dock area. Used by the drop handler, where the dock area
        /// was chosen by the user rather than inferred — so it docks regardless of the auto-dock
        /// preference, which is about where a middle-click should put things.
        /// </summary>
        public static void OpenInDockArea(Object target, Object dockArea)
        {
            if (!target) return;

            // Deferred: creating and focusing a window from inside the drop that asked for it
            // reshuffles focus while the event is still being dispatched.
            EditorApplication.delayCall += () => Create(target, dockArea, dock: true);
        }

        private static void Create(Object target, Object dockArea, bool dock)
        {
            if (!target) return;

            EditorWindow window = HelpfulEditorPropertyEditor.CreateHidden(target);

            // Nothing to dock, so the object at least gets its window — floating, the way Unity's own
            // Properties menu item opens it.
            if (!window)
            {
                HelpfulEditorPropertyEditor.OpenFloating(target);
                return;
            }

            if (!dock || !HelpfulEditorDockArea.AddTab(dockArea, window)) window.Show();

            window.Focus();
        }
    }
}
