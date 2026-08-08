using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Shared shell for the suite's dropdown panels: the frame Unity does not draw for them, a
    /// position that survives being resized, and an optional header that can carry a title, a close
    /// button and a drag grip.
    ///
    /// Everything past the frame is opt-in. A panel with no HeaderTitle has no header at all, which
    /// is what the list dropdowns want; overriding it turns on the bar, and ShowCloseButton and
    /// Movable decide what the bar carries.
    /// </summary>
    internal abstract class HelpfulEditorDropdownWindow : EditorWindow
    {
        protected const float FramePadding = 6f;
        protected const float HeaderGap = 4f;

        private static readonly GUIContent CloseContent = new GUIContent("×", "Close");

        private static GUIStyle _closeStyle;

        private static GUIStyle CloseStyle => _closeStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            padding = new RectOffset(0, 0, 0, 0)
        };

        private static Color HeaderColor => EditorGUIUtility.isProSkin
            ? new Color(0.27f, 0.27f, 0.27f)
            : new Color(0.70f, 0.70f, 0.70f);

        private static Color CloseHoverColor => EditorGUIUtility.isProSkin
            ? new Color(0.75f, 0.25f, 0.25f, 0.85f)
            : new Color(0.85f, 0.35f, 0.35f, 0.85f);

        /// <summary>
        /// Where the panel is meant to sit, in screen space. Resizing has to restate the position as
        /// well as the size, and reading it back off the window is not dependable on a dropdown —
        /// early on it can still be the origin, which is enough to pin the panel to the corner of the
        /// screen for good. Held here instead, and kept current by the drag.
        /// </summary>
        private Vector2 _anchor;

        private bool _dragging;
        private Vector2 _dragOffset;

        /// <summary>Text in the header. Null or empty means the panel has no header at all.</summary>
        protected virtual string HeaderTitle => null;

        protected virtual bool ShowCloseButton => true;
        protected virtual bool Movable => true;
        protected virtual float HeaderHeight => 20f;
        protected virtual bool CloseOnEscape => true;

        protected bool HasHeader => !string.IsNullOrEmpty(HeaderTitle);

        /// <summary>False once the panel has nothing left to show, which closes it on the next pass.</summary>
        protected virtual bool IsValid => true;

        /// <summary>Space the frame and header take, so a subclass sizing to its content can add it on.</summary>
        protected float ChromeHeight => HeaderTop + FramePadding;

        protected float ChromeWidth => FramePadding * 2f;

        protected Rect ContentRect => Rect.MinMaxRect(FramePadding, HeaderTop,
            position.width - FramePadding, position.height - FramePadding);

        private float HeaderTop
        {
            get
            {
                if (!HasHeader) return FramePadding;

                return HelpfulEditorGUI.BorderWeight() * 2f + HeaderHeight + HeaderGap;
            }
        }

        /// <param name="activator">
        /// In screen space. The panel is placed against its lower-left corner, which is where
        /// ShowAsDropDown puts it and what the anchor has to agree with for a resize to stay put.
        /// </param>
        protected void ShowAnchored(Rect activator, Vector2 size)
        {
            _anchor = new Vector2(activator.x, activator.yMax);

            // Hover states in the header and in any row list only update on a repaint, and a window
            // is not given one as the pointer moves unless it asks.
            wantsMouseMove = true;

            ShowAsDropDown(activator, size);
        }

        /// <summary>Resizes about the anchor. Ignores changes under a pixel, which is what stops a resize-repaint loop.</summary>
        protected void Resize(Vector2 size)
        {
            size = new Vector2(Mathf.Ceil(size.x), Mathf.Ceil(size.y));

            if (Mathf.Abs(size.x - position.width) <= 1f && Mathf.Abs(size.y - position.height) <= 1f) return;

            // A dropdown takes its size from these rather than from position alone.
            minSize = size;
            maxSize = size;
            position = new Rect(_anchor, size);

            Repaint();
        }

        protected abstract void DrawContent(Rect content, Event evt);

        private void OnGUI()
        {
            if (!IsValid)
            {
                Close();
                return;
            }

            Event evt = Event.current;

            if (evt.type == EventType.MouseMove) Repaint();

            if (CloseOnEscape && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                evt.Use();
                Close();
                return;
            }

            if (evt.type == EventType.Repaint)
            {
                HelpfulEditorGUI.DrawPanelFrame(new Rect(0f, 0f, position.width, position.height));
            }

            // A closed window must not be drawn into, so the pass ends here when the header took it.
            if (HasHeader && DrawHeader(evt)) return;

            DrawContent(ContentRect, evt);
        }

        /// <summary>Returns true once the header has closed the window, which ends the GUI pass.</summary>
        private bool DrawHeader(Event evt)
        {
            float edge = HelpfulEditorGUI.BorderWeight() * 2f;
            Rect bar = new Rect(edge, edge, position.width - edge * 2f, HeaderHeight);

            if (evt.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bar, HeaderColor);
                HelpfulEditorGUI.DrawHorizontalLine(bar.x, bar.xMax, bar.yMax, HelpfulEditorGUI.PanelSeparator, LineStyle.Solid);
            }

            Rect closeRect = ShowCloseButton
                ? new Rect(bar.xMax - bar.height, bar.y, bar.height, bar.height)
                : new Rect(bar.xMax, bar.y, 0f, bar.height);

            Rect gripRect = Rect.MinMaxRect(bar.x, bar.y, closeRect.x, bar.yMax);

            GUI.Label(Rect.MinMaxRect(gripRect.x + 4f, gripRect.y, gripRect.xMax - 4f, gripRect.yMax),
                HeaderTitle, EditorStyles.boldLabel);

            if (ShowCloseButton && DrawCloseButton(closeRect, evt))
            {
                Close();
                return true;
            }

            if (Movable) HandleDrag(gripRect, evt);

            return false;
        }

        private static bool DrawCloseButton(Rect rect, Event evt)
        {
            bool hovered = rect.Contains(evt.mousePosition);

            if (evt.type == EventType.Repaint && hovered) EditorGUI.DrawRect(rect, CloseHoverColor);

            GUI.Label(rect, CloseContent, CloseStyle);

            if (evt.type != EventType.MouseDown || !hovered || evt.button != 0) return false;

            evt.Use();
            return true;
        }

        private void HandleDrag(Rect grip, Event evt)
        {
            EditorGUIUtility.AddCursorRect(grip, MouseCursor.MoveArrow);

            switch (evt.type)
            {
                case EventType.MouseDown when evt.button == 0 && grip.Contains(evt.mousePosition):
                    _dragging = true;

                    // Captured once, against the window's position at the time of the press. It stays
                    // correct for the whole drag because the cursor's screen position is absolute —
                    // following the event delta instead would compound the movement the window has
                    // already made and send it running away from the pointer.
                    _dragOffset = GUIUtility.GUIToScreenPoint(evt.mousePosition) - position.position;
                    evt.Use();
                    break;

                case EventType.MouseDrag when _dragging:
                    _anchor = GUIUtility.GUIToScreenPoint(evt.mousePosition) - _dragOffset;
                    position = new Rect(_anchor, position.size);
                    evt.Use();
                    break;

                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    evt.Use();
                    break;
            }
        }
    }
}
