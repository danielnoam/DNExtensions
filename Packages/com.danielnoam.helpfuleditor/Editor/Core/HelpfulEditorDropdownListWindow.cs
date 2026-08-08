using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// A dropdown panel whose content is a list of fixed-height rows: scrolling, hover, keyboard
    /// navigation and sizing to the widest row are all handled here, so a subclass only has to say
    /// how many rows there are, how wide one wants to be, how to draw it and what activating it does.
    ///
    /// Sizing happens on the first layout pass rather than when the window opens, because measuring a
    /// string needs EditorStyles and that is only valid inside a GUI pass.
    /// </summary>
    internal abstract class HelpfulEditorDropdownListWindow : HelpfulEditorDropdownWindow
    {
        private const float ScrollBarWidth = 14f;

        private static Color RowHighlightColor => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.09f)
            : new Color(0f, 0f, 0f, 0.08f);

        private Vector2 _scroll;
        private int _highlighted;
        private bool _sized;
        private bool _reveal;

        protected virtual float RowHeight => 20f;
        protected virtual float MinWidth => 190f;
        protected virtual float MaxWidth => 460f;
        protected virtual float MinHeight => 40f;
        protected virtual float MaxHeight => 460f;

        protected abstract int RowCount { get; }
        protected abstract void DrawRow(Rect rowRect, int index);
        protected abstract void Activate(int index, bool additive);

        /// <summary>How wide this row wants the content area to be, ignoring the frame and any scrollbar.</summary>
        protected abstract float MeasureRowWidth(int index);

        /// <summary>Rows that say no are drawn but cannot be clicked. Keyboard activation is refused too.</summary>
        protected virtual bool IsRowEnabled(int index) => true;

        /// <summary>Called every pass with the row under the cursor or keyboard, for previewing it elsewhere.</summary>
        protected virtual void OnHighlightChanged(int index)
        {
        }

        /// <summary>
        /// Extra space above this row, with a divider drawn through it — how a list breaks itself into
        /// groups. Zero for no break, which is every row of an ungrouped list.
        /// </summary>
        protected virtual float SeparatorBefore(int index) => 0f;

        /// <summary>A sensible gap for SeparatorBefore to return, so grouped lists space alike.</summary>
        protected const float SeparatorHeight = 7f;

        protected override bool IsValid => RowCount > 0;

        protected int Highlighted => _highlighted;

        protected void SetHighlight(int index)
        {
            _highlighted = Mathf.Max(0, index);
        }

        /// <summary>Opens at the row count's exact height and the narrowest allowed width, then fits itself.</summary>
        protected void ShowList(Rect activator)
        {
            ShowAnchored(activator, new Vector2(MinWidth, DesiredHeight()));
        }

        private float DesiredHeight()
        {
            return Mathf.Clamp(ChromeHeight + ListHeight(), MinHeight, MaxHeight);
        }

        /// <summary>Every row plus whatever the separators add, which is what the rows are laid out against.</summary>
        private float ListHeight()
        {
            float total = 0f;
            for (int i = 0; i < RowCount; i++) total += SeparatorBefore(i) + RowHeight;

            return total;
        }

        protected override void DrawContent(Rect content, Event evt)
        {
            if (!_sized && evt.type == EventType.Layout) ApplySize();
            if (HandleKeys(evt)) return;

            DrawList(content, evt);
        }

        private void ApplySize()
        {
            _sized = true;

            float widest = 0f;
            for (int i = 0; i < RowCount; i++) widest = Mathf.Max(widest, MeasureRowWidth(i));

            // A clamped list scrolls, and the bar takes its width out of the rows rather than the
            // window — so it has to be asked for on top, or the widest row loses its tail to it.
            bool scrolls = ChromeHeight + ListHeight() > MaxHeight;

            float width = Mathf.Clamp(widest + ChromeWidth + (scrolls ? ScrollBarWidth : 0f), MinWidth, MaxWidth);

            Resize(new Vector2(width, DesiredHeight()));
        }

        private bool HandleKeys(Event evt)
        {
            if (evt.type != EventType.KeyDown) return false;

            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    Move(1);
                    evt.Use();
                    return true;

                case KeyCode.UpArrow:
                    Move(-1);
                    evt.Use();
                    return true;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    evt.Use();
                    if (IsRowEnabled(_highlighted)) Activate(_highlighted, evt.shift);
                    return true;

                default:
                    return false;
            }
        }

        private void Move(int delta)
        {
            _highlighted = Mathf.Clamp(_highlighted + delta, 0, RowCount - 1);
            _reveal = true;

            Repaint();
        }

        private void DrawList(Rect listRect, Event evt)
        {
            float contentHeight = ListHeight();
            bool scrolls = contentHeight > listRect.height;

            if (_reveal && evt.type == EventType.Repaint) Reveal(listRect);

            Rect viewRect = new Rect(0f, 0f, listRect.width - (scrolls ? ScrollBarWidth : 0f), contentHeight);
            _scroll = GUI.BeginScrollView(listRect, _scroll, viewRect);

            float y = 0f;

            for (int i = 0; i < RowCount; i++)
            {
                float gap = SeparatorBefore(i);

                if (gap > 0f)
                {
                    // Centred in its own gap rather than butted against a row, so the break reads as
                    // belonging to neither side.
                    if (evt.type == EventType.Repaint)
                    {
                        HelpfulEditorGUI.DrawHorizontalLine(0f, viewRect.width, Mathf.Round(y + gap * 0.5f),
                            HelpfulEditorGUI.PanelSeparator, LineStyle.Solid);
                    }

                    y += gap;
                }

                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                y += RowHeight;

                bool hovered = rowRect.Contains(evt.mousePosition);
                if (hovered && evt.type == EventType.MouseMove) _highlighted = i;

                if (evt.type == EventType.Repaint && i == _highlighted) EditorGUI.DrawRect(rowRect, RowHighlightColor);

                DrawRow(rowRect, i);

                if (!hovered || evt.type != EventType.MouseDown || !IsRowEnabled(i)) continue;

                evt.Use();

                // Closed out before activating, since activating can close the window and leave the
                // scroll view without its matching end call.
                GUI.EndScrollView();
                Activate(i, evt.shift);
                return;
            }

            GUI.EndScrollView();

            // Driven from here rather than each row so the keyboard and the cursor agree on what is
            // previewed, whichever of the two moved last.
            if (_highlighted >= 0 && _highlighted < RowCount) OnHighlightChanged(_highlighted);
        }

        /// <summary>Keeps the arrow keys from walking the highlight off the visible part of a scrolling list.</summary>
        private void Reveal(Rect listRect)
        {
            _reveal = false;

            float top = 0f;
            for (int i = 0; i < _highlighted && i < RowCount; i++) top += SeparatorBefore(i) + RowHeight;
            top += SeparatorBefore(_highlighted);

            float bottom = top + RowHeight;

            if (top < _scroll.y) _scroll.y = top;
            else if (bottom > _scroll.y + listRect.height) _scroll.y = bottom - listRect.height;
        }
    }
}
