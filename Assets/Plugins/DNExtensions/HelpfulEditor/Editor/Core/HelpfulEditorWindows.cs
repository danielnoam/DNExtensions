using System;
using UnityEditor;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Which editor window the mouse is currently over. Used to qualify the cached hover state
    /// before a global keybind acts on it, so a stale hover from another window can't be targeted.
    /// Types are resolved once and compared by identity rather than by name, so a same-named type
    /// from another assembly cannot masquerade as one of Unity's windows.
    /// </summary>
    internal static class HelpfulEditorWindows
    {
        private static readonly Type HierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        private static readonly Type ProjectWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");

        // PropertyEditor is InspectorWindow's base on modern Unity, and is also the floating
        // Properties window's own type — matching on it covers both, and falls back for versions
        // old enough that only InspectorWindow exists.
        private static readonly Type InspectorWindowType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor")
            ?? typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

        public static bool MouseOverHierarchy => IsMouseOver(HierarchyWindowType);
        public static bool MouseOverProject => IsMouseOver(ProjectWindowType);
        public static bool MouseOverInspector => IsMouseOver(InspectorWindowType);

        public static bool IsInspector(EditorWindow window)
        {
            return window && InspectorWindowType != null && InspectorWindowType.IsInstanceOfType(window);
        }

        /// <summary>
        /// Subclasses count. Exact identity looked safer but is wrong here: Unity's windows are
        /// routinely instances of a derived type, and a mismatch silently disables every keybind
        /// gated on that window.
        /// </summary>
        private static bool IsMouseOver(Type windowType)
        {
            if (windowType == null) return false;

            EditorWindow window = EditorWindow.mouseOverWindow;
            return window && windowType.IsInstanceOfType(window);
        }
    }
}
