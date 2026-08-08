using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Right-aligned strip of component icons on each Hierarchy row. Alt+Click opens the shared
    /// quick-edit popup for that component, and the strip brightens while Alt is held so that is
    /// discoverable rather than something you have to be told about.
    ///
    /// Icons here are deliberately not draggable: moving and copying components is an Inspector
    /// gesture (see DNExtensions.HelpfulEditor.Inspector.ComponentDragger), and a row that starts a
    /// component drag competes with the Hierarchy's own reparenting drag.
    /// </summary>
    internal static class HierarchyComponentStrip
    {
        /// <summary>Gap kept between the end of the row's name and the first icon.</summary>
        private const float LabelPadding = 6f;

        private const double PendingTimeout = 1.0;

        private static readonly List<Component> Buffer = new List<Component>();
        private static readonly List<Rect> IconRects = new List<Rect>();
        private static readonly GUIContent OverflowContent = new GUIContent();
        private static readonly GUIContent MeasureContent = new GUIContent();

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
        /// caller can place things to its left.
        ///
        /// The strip gives way to the row's own name: it is only allowed the space to the right of
        /// the label, and drops icons rather than covering it. The width returned is therefore what
        /// was actually drawn, not what was wanted.
        /// </summary>
        public static float Draw(Rect rowRect, GameObject gameObject, HierarchySettings settings)
        {
            List<Component> components = Buffer;
            HelpfulEditorGUI.GetDisplayComponents(gameObject, settings.excludedComponentTypes, components);
            if (components.Count == 0) return 0f;

            float labelEnd = rowRect.x + HierarchyModule.IconWidth + LabelWidth(gameObject.name) + LabelPadding;
            if (labelEnd >= rowRect.xMax) return 0f;

            Rect area = Rect.MinMaxRect(labelEnd, rowRect.y, rowRect.xMax, rowRect.yMax);

            HelpfulEditorGUI.LayoutIconStrip(area, components.Count, settings.componentIconSize,
                settings.componentStripMaxIcons, IconRects, out int shown, out Rect overflowRect);

            if (shown == 0) return 0f;

            Event evt = Event.current;

            // Without this the Hierarchy only repaints when something asks it to, so both the lift
            // below and the link cursor lag behind the pointer by however long that takes.
            HelpfulEditorGUI.MarkInteractive(Rect.MinMaxRect(IconRects[0].x, rowRect.y, area.xMax, rowRect.yMax));

            bool armed = settings.componentQuickEditEnabled && evt != null && evt.alt;

            Color previousColor = GUI.color;

            for (int i = 0; i < shown; i++)
            {
                Component component = components[i];
                Rect iconRect = IconRects[i];

                SetTint(previousColor, armed && Hovered(iconRect, evt));

                Texture icon = HelpfulEditorGUI.GetIcon(component);
                if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                HandleIconInput(iconRect, component, settings);
            }

            if (shown < components.Count)
            {
                bool overflowHovered = Hovered(overflowRect, evt);

                OverflowContent.text = $"+{components.Count - shown}";
                OverflowContent.tooltip = overflowHovered ? BuildOverflowTooltip(components, shown) : string.Empty;

                SetTint(previousColor, armed && overflowHovered);

                GUI.Label(overflowRect, OverflowContent, HelpfulEditorGUI.BadgeStyle);
                HandleOverflowInput(overflowRect, components, shown, settings);
            }

            GUI.color = previousColor;

            // Measured from the leftmost icon that survived rather than from the intended width, so
            // the child count badge sits against the strip that is really there.
            return area.xMax - IconRects[0].x;
        }

        /// <summary>
        /// Only the icon under the cursor comes up to full strength while the modifier is held.
        /// Lifting the whole strip said every icon was about to become a button, when the click can
        /// only ever land on one of them.
        /// </summary>
        private static void SetTint(Color baseColor, bool lifted)
        {
            float opacity = lifted ? 1f : HelpfulEditorGUI.IconStripOpacity;
            GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * opacity);
        }

        private static bool Hovered(Rect rect, Event evt)
        {
            return evt != null && rect.Contains(evt.mousePosition);
        }

        private static float LabelWidth(string name)
        {
            MeasureContent.text = name;
            return EditorStyles.label.CalcSize(MeasureContent).x;
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

            // Only while the click would actually do something. Offering the link cursor whenever the
            // pointer crosses an icon promises a button that is not there until the modifier is held.
            if (settings.componentQuickEditEnabled && evt.alt) EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);

            if (settings.componentQuickEditEnabled && evt.type == EventType.MouseDown && evt.button == 0 && evt.alt && CanClaimClick(evt))
            {
                _pendingQuickEdit = component;
                _pendingScreenPosition = HelpfulEditorQuickEditWindow.MouseScreenPosition();
                _pendingTime = EditorApplication.timeSinceStartup;
                evt.Use();
                return;
            }

            // Unity starts its own drag from anywhere on the row, so removing the component drag
            // just handed the icons over to the GameObject drag instead. The strip is a control
            // surface rather than a grab handle: swallowing the drag keeps a slipped cursor from
            // picking the object up while aiming at an icon.
            if (evt.type == EventType.MouseDrag && evt.button == 0) evt.Use();
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
            if (evt == null || evt.button != 0) return;
            if (!overflowRect.Contains(evt.mousePosition)) return;

            // Same as the icons: the badge is part of the strip, not somewhere to grab the row.
            if (evt.type == EventType.MouseDrag)
            {
                evt.Use();
                return;
            }

            if (evt.type != EventType.MouseDown || !CanClaimClick(evt)) return;

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
