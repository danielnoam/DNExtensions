using System;
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
        private const float SearchHeight = 18f;
        private const float SearchGap = 4f;

        /// <summary>
        /// Unique per window so two dropdowns open at once cannot focus each other's field. Focus is
        /// addressed by name in IMGUI, and a shared name is a shared address.
        /// </summary>
        private readonly string _searchControlName = $"helpfuleditor-dropdown-search-{Guid.NewGuid():N}";

        private static Color RowHighlightColor => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.09f)
            : new Color(0f, 0f, 0f, 0.08f);

        private static Color EmptyTextColor => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.45f)
            : new Color(0f, 0f, 0f, 0.45f);

        private static GUIStyle _emptyStyle;

        private static GUIStyle EmptyStyle => _emptyStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter
        };

        private Vector2 _scroll;
        private int _highlighted;
        private bool _sized;
        private bool _reveal;
        private string _search = string.Empty;
        private bool _searchFocused;

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

        /// <summary>
        /// Middle-click on a row. Nothing by default — a list whose rows stand for something findable
        /// elsewhere overrides it. Unlike <see cref="Activate"/> it is offered on disabled rows too,
        /// since pointing at a thing is not doing anything to it.
        /// </summary>
        protected virtual void AlternateActivate(int index)
        {
        }

        /// <summary>A search field above the list, which filters it. Off unless a subclass asks for it.</summary>
        protected virtual bool ShowSearchField => false;

        /// <summary>What is typed in the search field, empty when there is none or nothing is typed.</summary>
        protected string SearchQuery => _search;

        /// <summary>
        /// Called when the query changes, for the subclass to rebuild its rows against it. The list's
        /// highlight and scroll are reset around this, so the rebuilt set is read from the top.
        /// </summary>
        protected virtual void OnSearchChanged(string query)
        {
        }

        /// <summary>Shown in place of the list when a search matches nothing.</summary>
        protected virtual string EmptySearchText => "No matches";

        private bool IsSearching => ShowSearchField && !string.IsNullOrEmpty(_search);

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

        /// <summary>
        /// A search that matches nothing empties the list without emptying the window — closing on it
        /// would take the field away mid-typo and leave no way to correct it.
        /// </summary>
        protected override bool IsValid => RowCount > 0 || IsSearching;

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
            return Mathf.Clamp(ChromeHeight + SearchStripHeight + ListHeight(), MinHeight, MaxHeight);
        }

        private float SearchStripHeight => ShowSearchField ? SearchHeight + SearchGap : 0f;

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

            // Ahead of the field being drawn, because a focused text field claims the arrow keys and
            // Return for its own caret — and in a list with a search box those still belong to the
            // list. Taken here they never reach it.
            bool keyUsed = HandleKeys(evt);

            Rect list = content;

            if (ShowSearchField)
            {
                DrawSearchField(new Rect(content.x, content.y, content.width, SearchHeight));

                list = Rect.MinMaxRect(content.x, content.y + SearchStripHeight, content.xMax, content.yMax);
            }

            if (keyUsed) return;

            if (RowCount == 0)
            {
                Color previous = GUI.color;
                GUI.color = EmptyTextColor;

                GUI.Label(list, EmptySearchText, EmptyStyle);

                GUI.color = previous;
                return;
            }

            DrawList(list, evt);
        }

        /// <summary>
        /// Focused as soon as it exists, so the list can be narrowed by typing without clicking into
        /// anything first — which is the only reason to put a search box on a dropdown that is
        /// already under the cursor.
        /// </summary>
        private void DrawSearchField(Rect rect)
        {
            GUI.SetNextControlName(_searchControlName);

            string query = EditorGUI.TextField(rect, _search, EditorStyles.toolbarSearchField);

            // After the first draw rather than on open: a control cannot be focused before it exists.
            if (!_searchFocused)
            {
                _searchFocused = true;
                EditorGUI.FocusTextInControl(_searchControlName);
            }

            if (query == _search) return;

            _search = query;

            // Reset around the rebuild, not after it: the rebuilt list is a different set of rows,
            // and carrying a highlight or a scroll offset into it lands on whatever happens to sit
            // at that index now.
            _highlighted = 0;
            _scroll = Vector2.zero;

            OnSearchChanged(query);
            Repaint();
        }

        private void ApplySize()
        {
            _sized = true;

            float widest = 0f;
            for (int i = 0; i < RowCount; i++) widest = Mathf.Max(widest, MeasureRowWidth(i));

            // A clamped list scrolls, and the bar takes its width out of the rows rather than the
            // window — so it has to be asked for on top, or the widest row loses its tail to it.
            bool scrolls = ChromeHeight + SearchStripHeight + ListHeight() > MaxHeight;

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

                    // Range-checked because a search that matches nothing leaves the window open over
                    // an empty list, and the highlight then names a row that is not there.
                    if (_highlighted >= 0 && _highlighted < RowCount && IsRowEnabled(_highlighted))
                    {
                        Activate(_highlighted, evt.shift);
                    }

                    return true;

                default:
                    return false;
            }
        }

        private void Move(int delta)
        {
            // Nothing to move between, and the clamp below would settle on -1 rather than on a row.
            if (RowCount == 0) return;

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

                if (!hovered || evt.type != EventType.MouseDown) continue;

                // Middle-click points at a row rather than acting on it, so it is allowed even where
                // activation is not — the same reasoning that keeps a scene's star live in play mode.
                if (evt.button == 2)
                {
                    evt.Use();

                    // Closed out first, since either call below can close the window and leave the
                    // scroll view without its matching end call.
                    GUI.EndScrollView();
                    AlternateActivate(i);
                    return;
                }

                // Right-click is nobody's here. It used to activate along with every other button,
                // which made a stray one open a scene.
                if (evt.button != 0 || !IsRowEnabled(i)) continue;

                evt.Use();

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
