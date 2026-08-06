using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Finding editor windows: which one the mouse is currently over, every open window of a kind,
    /// and the Scene View an action should act on. The mouse checks qualify a cached hover state
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

        public static bool IsProjectBrowser(EditorWindow window)
        {
            return window && ProjectWindowType != null && ProjectWindowType.IsInstanceOfType(window);
        }

        public static IEnumerable<EditorWindow> AllProjectBrowsers() => AllOfType(ProjectWindowType);

        public static IEnumerable<EditorWindow> AllInspectors() => AllOfType(InspectorWindowType);

        /// <summary>
        /// The Scene View an action should act on. SceneView.lastActiveSceneView on its own is null
        /// until one has been focused at least once in the session, so anything reaching for it
        /// bare does nothing at all on a freshly opened layout.
        /// </summary>
        /// <param name="needsFocus">True when no Scene View currently holds focus.</param>
        public static SceneView ResolveSceneView(out bool needsFocus)
        {
            needsFocus = false;

            if (SceneView.sceneViews == null || SceneView.sceneViews.Count == 0) return null;

            // One that already has focus is the one being looked at, and it needs nothing doing.
            SceneView active = SceneView.lastActiveSceneView;
            if (active && active.hasFocus) return active;

            foreach (object candidate in SceneView.sceneViews)
            {
                if (candidate is SceneView view && view.hasFocus) return view;
            }

            needsFocus = true;

            return active ? active : SceneView.sceneViews[0] as SceneView;
        }

        private static IEnumerable<EditorWindow> AllOfType(Type windowType)
        {
            List<EditorWindow> windows = new List<EditorWindow>();
            if (windowType == null) return windows;

            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(windowType))
            {
                if (candidate is EditorWindow window) windows.Add(window);
            }

            return windows;
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
