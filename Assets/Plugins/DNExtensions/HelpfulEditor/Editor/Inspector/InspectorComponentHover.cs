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

        private static readonly List<VisualElement> Registered = new List<VisualElement>();

        private static double _lastScan;

        public static Component HoveredComponent { get; private set; }

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
            Registered.RemoveAll(element => element?.panel == null);

            // PointerLeave is not guaranteed — a window closing or the cursor jumping straight out
            // can skip it — so a stale hover is dropped here as a backstop. Only on positive
            // evidence though: mouseOverWindow is null whenever the editor cannot say, and treating
            // that as "not over the Inspector" would clear the hover between pointer moves.
            if (HoveredComponent && EditorWindow.mouseOverWindow && !HelpfulEditorWindows.MouseOverInspector)
            {
                HoveredComponent = null;
            }

            Type inspectorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor")
                                 ?? typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType == null) return;

            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(inspectorType))
            {
                if (candidate is not EditorWindow window) continue;

                VisualElement root = window.rootVisualElement;
                if (root == null || Registered.Contains(root)) continue;

                root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
                Registered.Add(root);
            }
        }

        private static void OnPointerLeave(PointerLeaveEvent evt) => HoveredComponent = null;

        private static void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.currentTarget is not VisualElement root || root.panel == null) return;

            HoveredComponent = FindComponentAt(root.panel, evt.position);
        }

        private static Component FindComponentAt(IPanel panel, Vector2 panelPosition)
        {
            Editor editor = InspectorElementLookup.FindEditorInAncestors(panel.Pick(panelPosition));
            return editor && editor.target is Component component ? component : null;
        }
    }
}
