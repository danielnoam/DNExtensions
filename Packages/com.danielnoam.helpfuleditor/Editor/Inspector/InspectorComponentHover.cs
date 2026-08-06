using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Tracks which component the cursor is over in the Inspector body — its header or its fields —
    /// so the hover keybinds act where you are looking rather than only on the header bar buttons.
    ///
    /// Unity exposes no callback for the inline component titlebars, and the attribute that would
    /// register one (EditorHeaderItem) is internal. The Inspector's editor list is UI Toolkit
    /// though, so each component's editor lives in its own VisualElement: the element under the
    /// pointer is picked and walked up until one of its ancestors turns out to own an Editor.
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorComponentHover
    {
        private const double ScanInterval = 0.5;

        private static readonly Dictionary<VisualElement, EditorWindow> Registered = new Dictionary<VisualElement, EditorWindow>();
        private static readonly List<VisualElement> Stale = new List<VisualElement>();

        private static double _lastScan;

        public static Component HoveredComponent { get; private set; }

        /// <summary>
        /// Raised while the cursor moves inside an Inspector, naming the window it moved in. The
        /// Inspector does not repaint on mouse move, so anything caching a hover from its own
        /// drawing has no reason to redraw otherwise — and a stationary cursor is not a reason.
        /// </summary>
        public static event Action<EditorWindow> PointerMoved;

        /// <summary>Raised when the cursor leaves an Inspector, naming the window it left.</summary>
        public static event Action<EditorWindow> PointerLeft;

        static InspectorComponentHover()
        {
            EditorApplication.update -= Scan;
            EditorApplication.update += Scan;
        }

        /// <summary>Inspector windows come and go, so the callback is re-attached to any root that lacks it.</summary>
        private static void Scan()
        {
            if (!HelpfulEditorSettings.Inspector.moduleEnabled) return;
            if (EditorApplication.timeSinceStartup - _lastScan < ScanInterval) return;

            _lastScan = EditorApplication.timeSinceStartup;
            PruneRegistrations();

            // PointerLeave is not guaranteed — a window closing or the cursor jumping straight out
            // can skip it — so a stale hover is dropped here as a backstop. Only on positive
            // evidence though: mouseOverWindow is null whenever the editor cannot say, and treating
            // that as "not over the Inspector" would clear the hover between pointer moves.
            if (HoveredComponent && EditorWindow.mouseOverWindow && !HelpfulEditorWindows.MouseOverInspector)
            {
                HoveredComponent = null;
            }

            foreach (EditorWindow window in HelpfulEditorWindows.AllInspectors())
            {
                if (!window) continue;

                VisualElement root = window.rootVisualElement;
                if (root == null || Registered.ContainsKey(root)) continue;

                root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

                // The window is kept alongside its root so the events below can name it. Reading
                // mouseOverWindow instead would be right for a move and wrong for a leave, which
                // fires once the cursor is already somewhere else.
                Registered[root] = window;
            }
        }

        private static void PruneRegistrations()
        {
            Stale.Clear();

            foreach (KeyValuePair<VisualElement, EditorWindow> entry in Registered)
            {
                if (entry.Key.panel == null || !entry.Value) Stale.Add(entry.Key);
            }

            foreach (VisualElement root in Stale)
            {
                Registered.Remove(root);
            }
        }

        private static void OnPointerLeave(PointerLeaveEvent evt)
        {
            HoveredComponent = null;

            if (evt.currentTarget is VisualElement root && Registered.TryGetValue(root, out EditorWindow window)) PointerLeft?.Invoke(window);
        }

        private static void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.currentTarget is not VisualElement root || root.panel == null) return;

            HoveredComponent = FindComponentAt(root.panel, evt.position);

            if (Registered.TryGetValue(root, out EditorWindow window)) PointerMoved?.Invoke(window);
        }

        private static Component FindComponentAt(IPanel panel, Vector2 panelPosition)
        {
            Editor editor = InspectorElementLookup.FindEditorInAncestors(panel.Pick(panelPosition));
            return editor && editor.target is Component component ? component : null;
        }
    }
}
