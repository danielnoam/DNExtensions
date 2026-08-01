using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Right-aligned strip of component icons on each Hierarchy row. Alt+Click opens the shared
    /// quick-edit popup for that component; dragging an icon feeds the same component drag and drop
    /// system the Inspector module owns (see DNExtensions.HelpfulEditor.Inspector.ComponentDragger).
    /// </summary>
    internal static class HierarchyComponentStrip
    {
        private const float Spacing = 1f;
        private const float OverflowPadding = 6f;

        private const double PendingTimeout = 1.0;

        private static readonly List<Component> Buffer = new List<Component>();
        private static readonly List<Rect> IconRects = new List<Rect>();
        private static readonly GUIContent OverflowContent = new GUIContent();

        private static Component _pendingQuickEdit;
        private static Vector2 _pendingScreenPosition;
        private static double _pendingTime;

        /// <summary>
        /// A dropdown window closes as soon as it loses focus, and the MouseUp that follows the
        /// opening click lands on the Hierarchy — which dismissed it in the same click. The request
        /// is queued on MouseDown and only opened once the click has finished.
        /// </summary>
        public static void ProcessPendingQuickEdit()
        {
            if (!_pendingQuickEdit) return;

            // If the release happened somewhere else entirely the request never arrives, and a
            // stranded one would fire on an unrelated click later.
            if (EditorApplication.timeSinceStartup - _pendingTime > PendingTimeout)
            {
                _pendingQuickEdit = null;
                return;
            }

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.MouseUp) return;

            Component component = _pendingQuickEdit;
            Vector2 screenPosition = _pendingScreenPosition;
            _pendingQuickEdit = null;

            evt.Use();
            EditorApplication.delayCall += () => HelpfulEditorQuickEditWindow.Open(component, screenPosition);
        }

        public static void DiscardPendingQuickEdit() => _pendingQuickEdit = null;

        /// <summary>
        /// Draws the strip right-aligned inside the row and returns the width it consumed, so the
        /// caller can place things to its left. Returning the width avoids a second pass over the
        /// component list purely to measure it.
        /// </summary>
        public static float Draw(Rect rowRect, GameObject gameObject, HierarchySettings settings)
        {
            List<Component> components = Buffer;
            HelpfulEditorGUI.GetDisplayComponents(gameObject, settings.excludedComponentTypes, components);
            if (components.Count == 0) return 0f;

            int visible = settings.componentStripMaxIcons > 0
                ? Mathf.Min(components.Count, settings.componentStripMaxIcons)
                : components.Count;

            float width = visible * (settings.componentIconSize + Spacing);
            if (visible < components.Count) width += settings.componentIconSize + OverflowPadding;

            Rect area = new Rect(rowRect.xMax - width, rowRect.y, width, rowRect.height);

            HelpfulEditorGUI.LayoutIconStrip(area, components.Count, settings.componentIconSize,
                settings.componentStripMaxIcons, IconRects, out int shown, out Rect overflowRect);

            for (int i = 0; i < shown; i++)
            {
                Component component = components[i];
                Rect iconRect = IconRects[i];

                Texture icon = HelpfulEditorGUI.GetIcon(component);
                if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                HandleIconInput(iconRect, component, settings);
            }

            if (shown < components.Count)
            {
                Event evt = Event.current;
                bool overflowHovered = evt != null && overflowRect.Contains(evt.mousePosition);

                OverflowContent.text = $"+{components.Count - shown}";
                OverflowContent.tooltip = overflowHovered ? BuildOverflowTooltip(components, shown) : string.Empty;

                GUI.Label(overflowRect, OverflowContent, HelpfulEditorGUI.BadgeStyle);
                HandleOverflowInput(overflowRect, components, shown, settings);
            }

            return width;
        }

        /// <summary>
        /// Unity starts a rename from a second click on an already-selected row, so the strip only
        /// ever claims a single, un-repeated click and stays out of the way entirely while a rename
        /// field is open.
        /// </summary>
        private static bool CanClaimClick(Event evt)
        {
            return !EditorGUIUtility.editingTextField && evt.clickCount == 1;
        }

        private static void HandleIconInput(Rect iconRect, Component component, HierarchySettings settings)
        {
            Event evt = Event.current;
            if (evt == null || !iconRect.Contains(evt.mousePosition)) return;
            if (EditorGUIUtility.editingTextField) return;

            EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);

            if (settings.componentQuickEditEnabled && evt.type == EventType.MouseDown && evt.button == 0 && evt.alt && CanClaimClick(evt))
            {
                _pendingQuickEdit = component;
                _pendingScreenPosition = HelpfulEditorQuickEditWindow.MouseScreenPosition();
                _pendingTime = EditorApplication.timeSinceStartup;
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseDrag && evt.button == 0 && component is not Transform)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { component };
                DragAndDrop.StartDrag(component.GetType().Name);
                evt.Use();
            }
        }

        /// <summary>
        /// Deferred because ShowAsDropDown tears down and rebuilds focus, which is not safe to do
        /// from inside the Hierarchy's own GUI pass.
        /// </summary>
        private static void OpenQuickEdit(Component component, Vector2 screenPosition)
        {
            EditorApplication.delayCall += () => HelpfulEditorQuickEditWindow.Open(component, screenPosition);
        }

        private static void HandleOverflowInput(Rect overflowRect, List<Component> components, int shown, HierarchySettings settings)
        {
            if (!settings.componentQuickEditEnabled) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.MouseDown || evt.button != 0) return;
            if (!overflowRect.Contains(evt.mousePosition) || !CanClaimClick(evt)) return;

            Vector2 screenPosition = HelpfulEditorQuickEditWindow.MouseScreenPosition();
            GenericMenu menu = new GenericMenu();
            for (int i = shown; i < components.Count; i++)
            {
                Component component = components[i];
                if (!component) continue;

                menu.AddItem(new GUIContent(component.GetType().Name), false, () => OpenQuickEdit(component, screenPosition));
            }

            menu.ShowAsContext();
            evt.Use();
        }

        private static string BuildOverflowTooltip(List<Component> components, int shown)
        {
            List<string> names = new List<string>();
            for (int i = shown; i < components.Count; i++)
            {
                if (components[i]) names.Add(components[i].GetType().Name);
            }

            return string.Join("\n", names);
        }
    }
}
