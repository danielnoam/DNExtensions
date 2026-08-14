using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Hover keybinds for the Inspector. The per-component actions target whichever component the
    /// cursor is over in the Inspector body, falling back to the header bar button when the cursor is
    /// up in the bar itself — so they work whether or not the header bar is switched on.
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorKeybinds
    {
        static InspectorKeybinds()
        {
            GlobalKeyCapture.KeyEvent -= OnKeyEvent;
            GlobalKeyCapture.KeyEvent += OnKeyEvent;
        }

        private static void OnKeyEvent()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled) return;
            if (!HelpfulEditorWindows.MouseOverInspector) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown) return;

            if (settings.focusSearchKey.Matches(evt))
            {
                if (settings.fieldSearchEnabled) InspectorHeaderBar.FocusSearchField();
                evt.Use();
                return;
            }

            if (EditorGUIUtility.editingTextField) return;

            // Acts on the whole object, so unlike the per-component actions below it does not need
            // anything under the cursor.
            if (settings.collapseAllKey.Matches(evt))
            {
                ToggleExpandedAll();
                evt.Use();
                return;
            }

            // The component under the cursor in the Inspector body wins; the header bar button is
            // the fallback for when the cursor is up in the bar itself.
            Component hovered = InspectorComponentHover.HoveredComponent
                ? InspectorComponentHover.HoveredComponent
                : InspectorHeaderBar.HoveredComponent;

            if (!hovered) return;

            if (settings.expandCollapseKey.Matches(evt))
            {
                ToggleExpanded(hovered);
                evt.Use();
                return;
            }

            if (settings.toggleEnabledKey.Matches(evt))
            {
                ToggleEnabled(hovered);
                evt.Use();
            }
        }

        private static void ToggleExpanded(Component component)
        {
            InternalEditorUtility.SetIsInspectorExpanded(component, !InternalEditorUtility.GetIsInspectorExpanded(component));
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        /// <summary>
        /// Collapses every component, or expands them all when none is open. Anything expanded counts
        /// as "not collapsed yet", so the first press always closes things — which is what the key is
        /// usually reached for, and it matches how the Hierarchy and Project read the same chord.
        /// </summary>
        private static void ToggleExpandedAll()
        {
            ActiveEditorTracker tracker = ActiveEditorTracker.sharedTracker;

            bool anyExpanded = false;
            foreach (Editor editor in tracker.activeEditors)
            {
                if (!editor || !(editor.target is Component component)) continue;
                if (!InternalEditorUtility.GetIsInspectorExpanded(component)) continue;

                anyExpanded = true;
                break;
            }

            foreach (Editor editor in tracker.activeEditors)
            {
                if (!editor || !(editor.target is Component component)) continue;

                InternalEditorUtility.SetIsInspectorExpanded(component, !anyExpanded);
            }

            tracker.ForceRebuild();
        }

        private static void ToggleEnabled(Component component)
        {
            HelpfulEditorComponents.Toggle(component);
        }
    }
}
