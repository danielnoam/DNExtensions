using System;
using System.Collections.Generic;
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

        /// <summary>How long a reorderable row has to be held still before it lifts, so a plain click still activates it.</summary>
        private const double LongPressSeconds = 0.3;

        /// <summary>How far a press may wander and still be a click. Past it, the row lifts and follows the cursor.</summary>
        private const float PressSlop = 4f;

        /// <summary>How quickly rows slide into place, as a rate for exponential easing. Higher is snappier.</summary>
        private const float SlideRate = 18f;

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

        private static Color DraggedRowColor => EditorGUIUtility.isProSkin
            ? new Color(0.27f, 0.27f, 0.27f)
            : new Color(0.84f, 0.84f, 0.84f);

        private static Color DraggedRowShadow => new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.45f : 0.2f);

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

        private int _controlId;
        private int _pressIndex = -1;
        private double _pressTime;
        private Vector2 _pressPosition;

        private int _dragIndex = -1;
        private int _dragTarget;
        private int _runStart;
        private int _runEnd;

        /// <summary>Where in the dragged row the cursor took hold of it, so the row does not jump to centre itself under the cursor.</summary>
        private float _dragGrab;

        private float _dragY;

        /// <summary>How far each row is currently drawn from its slot, eased toward where the drag wants it.</summary>
        private float[] _offsets = Array.Empty<float>();

        private float[] _rowTops = Array.Empty<float>();
        private double _lastSlideTime;
        private bool _ticking;

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

        /// <summary>
        /// A key press while a row is highlighted, offered before the list's own navigation so a row
        /// can take a key the list would otherwise use. Return true to consume it.
        /// </summary>
        protected virtual bool HandleRowKey(int index, Event evt) => false;

        /// <summary>
        /// Rows that say yes can be dragged among their neighbours, the way Unity's own reorderable
        /// lists work: pulled straight off, or held still until they lift. A drag stays inside the
        /// unbroken run of reorderable rows it started in. A click on such a row activates on release
        /// rather than on press.
        /// </summary>
        protected virtual bool IsRowReorderable(int index) => false;

        /// <summary>
        /// A drag ended with the row at <paramref name="from"/> dropped into the slot at
        /// <paramref name="to"/>. The subclass reorders its rows so the moved one ends up at
        /// <paramref name="to"/>.
        /// </summary>
        protected virtual void MoveRow(int from, int to)
        {
        }

        protected bool IsDragging => _dragIndex >= 0;

        /// <summary>Escape cancels a drag rather than closing the list out from under it.</summary>
        protected override bool CloseOnEscape => !IsDragging && _pressIndex < 0;

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

            // The keyboard sits out a drag, apart from Escape calling it off — a Return or an arrow
            // acting on the list while a row hangs under the cursor would leave the two disagreeing.
            if (IsDragging || _pressIndex >= 0)
            {
                if (evt.keyCode == KeyCode.Escape) CancelPress();

                evt.Use();
                return true;
            }

            if (_highlighted >= 0 && _highlighted < RowCount && HandleRowKey(_highlighted, evt))
            {
                evt.Use();
                _reveal = true;
                Repaint();
                return true;
            }

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
            _controlId = GUIUtility.GetControlID(FocusType.Passive);

            float contentHeight = ListHeight();
            bool scrolls = contentHeight > listRect.height;

            if (_reveal && evt.type == EventType.Repaint) Reveal(listRect);

            Rect viewRect = new Rect(0f, 0f, listRect.width - (scrolls ? ScrollBarWidth : 0f), contentHeight);
            _scroll = GUI.BeginScrollView(listRect, _scroll, viewRect);

            MeasureRowTops();

            if (evt.type == EventType.Repaint) Slide();

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

                Rect rowRect = new Rect(0f, y + _offsets[i], viewRect.width, RowHeight);
                y += RowHeight;

                // The lifted row is drawn last, over everything, wherever the cursor has it.
                if (i == _dragIndex) continue;

                bool hovered = rowRect.Contains(evt.mousePosition) && !IsDragging;
                if (hovered && evt.type == EventType.MouseMove) _highlighted = i;

                if (evt.type == EventType.Repaint && i == _highlighted && !IsDragging) EditorGUI.DrawRect(rowRect, RowHighlightColor);

                DrawRow(rowRect, i);

                if (!hovered || evt.type != EventType.MouseDown) continue;

                // Held rather than activated, since the press may yet become a drag. Taken whether or
                // not the row is enabled: moving it changes nothing that being disabled guards.
                if (evt.button == 0 && IsRowReorderable(i))
                {
                    evt.Use();
                    BeginPress(i, evt.mousePosition);
                    continue;
                }

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

            if (IsDragging) DrawDraggedRow(viewRect.width, evt);

            int clicked = HandlePress(evt);

            if (clicked >= 0)
            {
                // Closed out first, since activating can close the window — the same order as a
                // press on an ordinary row.
                GUI.EndScrollView();
                Activate(clicked, evt.shift);
                return;
            }

            GUI.EndScrollView();

            // Driven from here rather than each row so the keyboard and the cursor agree on what is
            // previewed, whichever of the two moved last.
            if (_highlighted >= 0 && _highlighted < RowCount) OnHighlightChanged(_highlighted);
        }

        /// <summary>
        /// Each row's resting top, separators included, which a drag measures against. A list that
        /// changed size under a drag has nothing left to drag through, so the drag is dropped.
        /// </summary>
        private void MeasureRowTops()
        {
            if (_rowTops.Length != RowCount)
            {
                _rowTops = new float[RowCount];
                _offsets = new float[RowCount];

                // Dropped outright rather than through CancelPress, which eases the lifted row back
                // from a slot measured against the list as it was.
                _pressIndex = -1;
                _dragIndex = -1;

                if (GUIUtility.hotControl == _controlId) GUIUtility.hotControl = 0;
            }

            float y = 0f;

            for (int i = 0; i < RowCount; i++)
            {
                y += SeparatorBefore(i);
                _rowTops[i] = y;
                y += RowHeight;
            }
        }

        private void BeginPress(int index, Vector2 position)
        {
            _pressIndex = index;
            _pressTime = EditorApplication.timeSinceStartup;
            _pressPosition = position;
            _highlighted = index;

            // Held as the hot control so the release still arrives if the cursor leaves the window.
            GUIUtility.hotControl = _controlId;

            StartTicking();
        }

        /// <summary>
        /// Runs a held press: lifting it once it is pulled away or held long enough, dragging it,
        /// dropping it, or — released before either — returning the row it clicked for the caller to
        /// activate. -1 for no click.
        /// </summary>
        private int HandlePress(Event evt)
        {
            if (_pressIndex < 0 && !IsDragging) return -1;

            switch (evt.GetTypeForControl(_controlId))
            {
                case EventType.MouseDrag:
                    // Pulling the row away lifts it straight off, as in Unity's own lists; the hold
                    // is only the other way in, for a press that stays put.
                    if (!IsDragging && Vector2.Distance(evt.mousePosition, _pressPosition) > PressSlop) BeginDrag();

                    if (IsDragging)
                    {
                        _dragY = evt.mousePosition.y;
                        UpdateDragTarget();
                        Repaint();
                    }

                    evt.Use();
                    return -1;

                case EventType.MouseUp:
                    evt.Use();

                    if (IsDragging)
                    {
                        Drop();
                        return -1;
                    }

                    int pressed = _pressIndex;
                    CancelPress();

                    bool stillOver = evt.mousePosition.y >= _rowTops[pressed] && evt.mousePosition.y < _rowTops[pressed] + RowHeight;
                    return stillOver && IsRowEnabled(pressed) ? pressed : -1;

                // Layout rather than Repaint, so the row lifts between frames instead of halfway
                // through drawing one.
                case EventType.Layout:
                    if (!IsDragging && EditorApplication.timeSinceStartup - _pressTime >= LongPressSeconds) BeginDrag();
                    return -1;

                default:
                    return -1;
            }
        }

        private void BeginDrag()
        {
            _dragIndex = _pressIndex;
            _pressIndex = -1;

            _runStart = _dragIndex;
            while (_runStart > 0 && IsRowReorderable(_runStart - 1) && SeparatorBefore(_runStart) <= 0f) _runStart--;

            _runEnd = _dragIndex;
            while (_runEnd < RowCount - 1 && IsRowReorderable(_runEnd + 1) && SeparatorBefore(_runEnd + 1) <= 0f) _runEnd++;

            _dragGrab = _pressPosition.y - _rowTops[_dragIndex];
            _dragY = _pressPosition.y;
            _dragTarget = _dragIndex;

            Repaint();
        }

        /// <summary>Where the lifted row is drawn: under the cursor, kept within the run it can move through.</summary>
        private float DragTop => Mathf.Clamp(_dragY - _dragGrab, _rowTops[_runStart], _rowTops[_runEnd]);

        /// <summary>The slot whose middle the lifted row's middle has passed, so rows swap at the halfway point as in Unity's lists.</summary>
        private void UpdateDragTarget()
        {
            float centre = DragTop + RowHeight * 0.5f;
            int target = _runStart + Mathf.FloorToInt((centre - _rowTops[_runStart]) / RowHeight);

            _dragTarget = Mathf.Clamp(target, _runStart, _runEnd);
        }

        /// <summary>Rows between the lifted row's slot and where it is headed step one row toward the gap it left.</summary>
        private float TargetOffset(int index)
        {
            if (!IsDragging || index == _dragIndex) return 0f;

            if (_dragTarget > _dragIndex && index > _dragIndex && index <= _dragTarget) return -RowHeight;
            if (_dragTarget < _dragIndex && index >= _dragTarget && index < _dragIndex) return RowHeight;

            return 0f;
        }

        /// <summary>Eases every row toward its target offset, by time rather than by frame so the speed holds however often it repaints.</summary>
        private void Slide()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - _lastSlideTime), 0f, 0.1f);
            _lastSlideTime = now;

            float blend = 1f - Mathf.Exp(-SlideRate * deltaTime);

            for (int i = 0; i < _offsets.Length; i++)
            {
                float target = TargetOffset(i);

                _offsets[i] = Mathf.Abs(target - _offsets[i]) < 0.1f ? target : Mathf.Lerp(_offsets[i], target, blend);
            }
        }

        private bool IsSliding
        {
            get
            {
                for (int i = 0; i < _offsets.Length; i++)
                {
                    if (!Mathf.Approximately(_offsets[i], TargetOffset(i))) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Commits the move, then carries every row's on-screen position across the reorder as an
        /// offset, so nothing jumps: the dropped row eases from where it was let go into its slot,
        /// and the rows that had not finished making room finish from where they were.
        /// </summary>
        private void Drop()
        {
            int from = _dragIndex;
            int to = _dragTarget;

            List<float> drawnTops = new List<float>(RowCount);
            for (int i = 0; i < RowCount; i++) drawnTops.Add(_rowTops[i] + _offsets[i]);

            drawnTops.RemoveAt(from);
            drawnTops.Insert(to, DragTop);

            _dragIndex = -1;
            if (GUIUtility.hotControl == _controlId) GUIUtility.hotControl = 0;

            if (from != to) MoveRow(from, to);

            _highlighted = to;

            if (drawnTops.Count == RowCount && _offsets.Length == RowCount)
            {
                for (int i = 0; i < RowCount; i++) _offsets[i] = drawnTops[i] - _rowTops[i];
            }

            StartTicking();
            Repaint();
        }

        /// <summary>Lets go of a press or a drag without moving anything. A lifted row eases back to its slot.</summary>
        private void CancelPress()
        {
            if (IsDragging && _dragIndex < _offsets.Length) _offsets[_dragIndex] = DragTop - _rowTops[_dragIndex];

            _pressIndex = -1;
            _dragIndex = -1;

            if (GUIUtility.hotControl == _controlId) GUIUtility.hotControl = 0;

            StartTicking();
            Repaint();
        }

        private void DrawDraggedRow(float width, Event evt)
        {
            Rect rect = new Rect(0f, DragTop, width, RowHeight);

            if (evt.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax, rect.width, 2f), DraggedRowShadow);
                EditorGUI.DrawRect(rect, DraggedRowColor);
                EditorGUI.DrawRect(rect, RowHighlightColor);
            }

            DrawRow(rect, _dragIndex);
        }

        /// <summary>
        /// Repaints every editor tick while something is moving or a press is waiting to become a
        /// drag — IMGUI only repaints on input, and a hold or a slide has none.
        /// </summary>
        private void StartTicking()
        {
            if (_ticking) return;

            _ticking = true;
            _lastSlideTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void Tick()
        {
            if (!this)
            {
                EditorApplication.update -= Tick;
                return;
            }

            Repaint();

            if (_pressIndex >= 0 || IsDragging || IsSliding) return;

            _ticking = false;
            EditorApplication.update -= Tick;
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
