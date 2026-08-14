using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Repaints the window under the cursor when a modifier key is pressed or released.
    ///
    /// Holding a modifier raises no event of its own, so anything that changes appearance while Alt
    /// is down would otherwise not appear until the mouse moved or something else forced a redraw —
    /// which reads as the affordance being broken rather than late.
    /// </summary>
    [InitializeOnLoad]
    internal static class HelpfulEditorModifierWatch
    {
        // Event.current is null outside a GUI callback, so the last event the editor processed is
        // read directly. Without it there is nothing to compare against from the update loop.
        private static readonly FieldInfo CurrentEvent =
            typeof(Event).GetField("s_Current", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool _altHeld;
        private static bool _warned;

        static HelpfulEditorModifierWatch()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (CurrentEvent == null) return;
            if (!AnyAltAffordance()) return;

            try
            {
                if (CurrentEvent.GetValue(null) is not Event evt) return;
                if (evt.alt == _altHeld) return;

                _altHeld = evt.alt;

                // Only the window under the cursor can be showing a hover affordance, so only it
                // has a reason to redraw.
                EditorWindow window = EditorWindow.mouseOverWindow;
                if (window) window.Repaint();
            }
            catch (Exception e)
            {
                if (_warned) return;

                _warned = true;
                Debug.LogWarning($"[HelpfulEditor] Modifier-key highlights will not update until the mouse moves. ({e.Message})");
            }
        }

        /// <summary>
        /// Everything that changes appearance on Alt belongs to one of these three — the Hierarchy's
        /// quick edit, the Inspector's dragger, the Project's quick object window. With all of them
        /// off nothing looks different for holding the key, so nothing needs redrawing.
        /// </summary>
        private static bool AnyAltAffordance()
        {
            return HelpfulEditorSettings.Hierarchy.moduleEnabled
                   || HelpfulEditorSettings.Inspector.moduleEnabled
                   || HelpfulEditorSettings.Project.moduleEnabled;
        }
    }
}
