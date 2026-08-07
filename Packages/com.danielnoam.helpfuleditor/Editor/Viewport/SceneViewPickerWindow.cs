using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// The list of overlapping objects, shown where the click landed. Rows read like Hierarchy rows —
    /// icon, name, component strip — and previewing the hovered one in the Scene View is what this
    /// gives that Unity's own menu does not.
    /// </summary>
    internal class SceneViewPickerWindow : EditorWindow
    {
        private const float RowHeight = 20f;
        private const float FramePadding = 6f;
        private const float IconWidth = 18f;
        private const float HeaderHeight = 18f;

        /// <summary>Size of a component icon in the strip, matching the Hierarchy's own default.</summary>
        private const float IconSize = 14f;

        /// <summary>Gap kept between the longest name and the component strip so the two never touch.</summary>
        private const float LabelStripGap = 12f;

        private const float ScrollBarWidth = 14f;

        /// <summary>Gap between the count header and the first row.</summary>
        private const float HeaderGap = 4f;

        // Bounds on the content-driven size. Min keeps a one-short-name list from being a sliver;
        // max stops a long name or a deep stack turning it into a full-screen panel, at which point
        // names ellipsise and the list scrolls instead. The height cap is deliberately generous —
        // scrolling a handful of objects defeats the point of listing them.
        private const float MinWidth = 200f;
        private const float MaxWidth = 520f;
        private const float MinHeight = 56f;
        private const float MaxHeight = 640f;

        private static Color OuterBorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.08f, 0.08f, 0.08f)
            : new Color(0.35f, 0.35f, 0.35f);

        private static Color InnerBorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.42f, 0.42f, 0.42f)
            : new Color(0.72f, 0.72f, 0.72f);

        private static Color RowHighlightColor => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.09f)
            : new Color(0f, 0f, 0f, 0.08f);

        private readonly List<GameObject> _candidates = new List<GameObject>();
        private readonly List<Component> _components = new List<Component>();
        private readonly List<Rect> _iconRects = new List<Rect>();
        private readonly GUIContent _rowContent = new GUIContent();
        private readonly GUIContent _overflowContent = new GUIContent();

        private Vector2 _scroll;
        private int _highlighted;
        private bool _sized;
        private bool _revealHighlighted;

        public static void Open(List<GameObject> candidates, Vector2 screenPosition, SceneView origin)
        {
            if (candidates == null || candidates.Count == 0) return;

            SceneViewPickerWindow window = CreateInstance<SceneViewPickerWindow>();
            window.Initialize(candidates);

            Rect activator = new Rect(screenPosition.x, screenPosition.y, 1f, 1f);

            // Opened at the narrowest allowed size and corrected to fit on the first layout pass,
            // which is the earliest point EditorStyles can measure a string. The height is already
            // exact here because rows are a fixed height, so only the width ever moves.
            window.ShowAsDropDown(activator, new Vector2(MinWidth, ContentHeight(candidates.Count)));

            if (origin) origin.Repaint();
        }

        /// <summary>
        /// The exact height the list needs. This has to stay the same arithmetic the drawing uses,
        /// which is why neither side goes through GUILayout.
        /// </summary>
        private static float ContentHeight(int rows)
        {
            return Mathf.Clamp(FramePadding * 2f + HeaderHeight + HeaderGap + rows * RowHeight, MinHeight, MaxHeight);
        }

        /// <summary>
        /// Grows the window to whatever its rows actually need, within the bounds. Runs on the first
        /// layout pass rather than from Open: measuring a name needs EditorStyles, which is only
        /// valid inside a GUI pass, and Open is reached from delayCall.
        /// </summary>
        private void ApplyContentSize(SceneViewSettings settings)
        {
            _sized = true;

            float widest = 0f;

            foreach (GameObject candidate in _candidates)
            {
                HelpfulEditorGUI.GetDisplayComponents(candidate, settings.pickerExcludedComponentTypes, _components);

                int icons = settings.pickerMaxIcons > 0
                    ? Mathf.Min(_components.Count, settings.pickerMaxIcons)
                    : _components.Count;

                float strip = icons * (IconSize + 1f);
                if (icons < _components.Count) strip += IconSize + 6f;

                _rowContent.text = candidate.name;
                _rowContent.tooltip = string.Empty;

                widest = Mathf.Max(widest, IconWidth + EditorStyles.label.CalcSize(_rowContent).x + LabelStripGap + strip);
            }

            float height = ContentHeight(_candidates.Count);

            // A clamped list scrolls, and the bar takes its width out of the rows rather than the
            // window — so it has to be asked for on top, or the strip loses an icon to it.
            bool scrolls = FramePadding * 2f + HeaderHeight + HeaderGap + _candidates.Count * RowHeight > MaxHeight;
            float width = Mathf.Clamp(widest + FramePadding * 2f + (scrolls ? ScrollBarWidth : 0f), MinWidth, MaxWidth);

            if (Mathf.Approximately(width, position.width) && Mathf.Approximately(height, position.height)) return;

            Vector2 size = new Vector2(width, height);

            // A dropdown takes its size from these rather than from position alone, and pinning both
            // to the same value is what keeps it from being dragged out of shape afterwards.
            minSize = size;
            maxSize = size;
            position = new Rect(position.x, position.y, width, height);

            Repaint();
        }

        private void Initialize(List<GameObject> candidates)
        {
            foreach (GameObject candidate in candidates)
            {
                if (candidate) _candidates.Add(candidate);
            }

            _highlighted = 0;

            // Without this the hover only updates when something else forces a repaint, which leaves
            // the Scene View preview stuck on whichever row was entered first.
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            if (_candidates.Count == 0)
            {
                Close();
                return;
            }

            Event evt = Event.current;
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;

            if (!_sized && evt.type == EventType.Layout) ApplyContentSize(settings);

            if (evt.type == EventType.MouseMove) Repaint();
            if (HandleKeys(evt)) return;

            DrawFrame();

            Rect inner = new Rect(FramePadding, FramePadding,
                position.width - FramePadding * 2f, position.height - FramePadding * 2f);

            Rect headerRect = new Rect(inner.x, inner.y, inner.width, HeaderHeight);
            GUI.Label(headerRect, $"{_candidates.Count} under cursor", EditorStyles.miniLabel);

            // Laid out by hand rather than through GUILayout: the layout helpers add their own
            // spacing and margins on top of what is asked for, which the window's own height
            // arithmetic cannot see — the list then overflowed and scrolled while the window still
            // believed every row fitted.
            DrawList(evt, settings, Rect.MinMaxRect(inner.x, headerRect.yMax + HeaderGap, inner.xMax, inner.yMax));
        }

        /// <summary>
        /// A dropdown window gets no chrome from Unity, so the panel edge is drawn here: a dark
        /// outer stroke to separate it from the Scene View behind, and a lighter inner one so the
        /// edge stays visible against a dark viewport too. Same treatment as the quick edit popup.
        /// </summary>
        private void DrawFrame()
        {
            if (Event.current.type != EventType.Repaint) return;

            Rect frame = new Rect(0f, 0f, position.width, position.height);

            EditorGUI.DrawRect(frame, HelpfulEditorGUI.WindowBackground);
            HelpfulEditorGUI.DrawBorder(frame, OuterBorderColor);
            HelpfulEditorGUI.DrawBorder(new Rect(frame.x + 1f, frame.y + 1f, frame.width - 2f, frame.height - 2f), InnerBorderColor);
        }

        private bool HandleKeys(Event evt)
        {
            if (evt.type != EventType.KeyDown) return false;

            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    evt.Use();
                    Close();
                    return true;

                case KeyCode.DownArrow:
                    MoveHighlight(1);
                    evt.Use();
                    return true;

                case KeyCode.UpArrow:
                    MoveHighlight(-1);
                    evt.Use();
                    return true;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    evt.Use();
                    Choose(_highlighted, false);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveHighlight(int delta)
        {
            _highlighted = Mathf.Clamp(_highlighted + delta, 0, _candidates.Count - 1);
            _revealHighlighted = true;

            SceneViewPicker.SetHoverTarget(_candidates[_highlighted]);
            Repaint();
        }

        private void DrawList(Event evt, SceneViewSettings settings, Rect listRect)
        {
            float contentHeight = _candidates.Count * RowHeight;
            bool scrolls = contentHeight > listRect.height;

            if (_revealHighlighted && evt.type == EventType.Repaint) Reveal(listRect);

            Rect viewRect = new Rect(0f, 0f, listRect.width - (scrolls ? ScrollBarWidth : 0f), contentHeight);
            _scroll = GUI.BeginScrollView(listRect, _scroll, viewRect);

            for (int i = 0; i < _candidates.Count; i++)
            {
                GameObject candidate = _candidates[i];
                if (!candidate) continue;

                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);

                bool hovered = rowRect.Contains(evt.mousePosition);
                if (hovered && evt.type == EventType.MouseMove) _highlighted = i;

                if (evt.type == EventType.Repaint && i == _highlighted)
                {
                    EditorGUI.DrawRect(rowRect, RowHighlightColor);
                }

                DrawRow(rowRect, candidate, settings);

                if (hovered && evt.type == EventType.MouseDown)
                {
                    evt.Use();

                    // Closed out before choosing, since selecting can close the window and leave the
                    // scroll view without its matching end call.
                    GUI.EndScrollView();
                    Choose(i, evt.shift);
                    return;
                }
            }

            GUI.EndScrollView();

            // Driven from the row loop rather than each row so the keyboard and the cursor agree on
            // what is previewed, whichever of the two moved last.
            if (_highlighted >= 0 && _highlighted < _candidates.Count)
            {
                SceneViewPicker.SetHoverTarget(_candidates[_highlighted]);
            }
        }

        /// <summary>Keeps the arrow keys from walking the highlight off the visible part of a scrolling list.</summary>
        private void Reveal(Rect listRect)
        {
            _revealHighlighted = false;

            float top = _highlighted * RowHeight;
            float bottom = top + RowHeight;

            if (top < _scroll.y) _scroll.y = top;
            else if (bottom > _scroll.y + listRect.height) _scroll.y = bottom - listRect.height;
        }

        private void DrawRow(Rect rowRect, GameObject candidate, SceneViewSettings settings)
        {
            Rect iconRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - 16f) * 0.5f, 16f, 16f);
            Texture icon = EditorGUIUtility.ObjectContent(candidate, typeof(GameObject)).image;
            if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            _rowContent.text = candidate.name;
            _rowContent.tooltip = HierarchyPath(candidate);

            float stripWidth = DrawComponentStrip(rowRect, candidate, settings);

            Rect labelRect = Rect.MinMaxRect(rowRect.x + IconWidth, rowRect.y, rowRect.xMax - stripWidth, rowRect.yMax);
            if (labelRect.width > 0f) GUI.Label(labelRect, _rowContent, EditorStyles.label);
        }

        /// <summary>Right-aligned component icons, matching the Hierarchy strip so rows read the same in both.</summary>
        private float DrawComponentStrip(Rect rowRect, GameObject candidate, SceneViewSettings settings)
        {
            HelpfulEditorGUI.GetDisplayComponents(candidate, settings.pickerExcludedComponentTypes, _components);
            if (_components.Count == 0) return 0f;

            Rect area = Rect.MinMaxRect(rowRect.x + IconWidth, rowRect.y, rowRect.xMax, rowRect.yMax);

            HelpfulEditorGUI.LayoutIconStrip(area, _components.Count, IconSize,
                settings.pickerMaxIcons, _iconRects, out int shown, out Rect overflowRect);

            if (shown == 0) return 0f;

            Color previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * HelpfulEditorGUI.IconStripOpacity);

            for (int i = 0; i < shown; i++)
            {
                Texture icon = HelpfulEditorGUI.GetIcon(_components[i]);
                if (icon) GUI.DrawTexture(_iconRects[i], icon, ScaleMode.ScaleToFit);
            }

            if (shown < _components.Count)
            {
                _overflowContent.text = $"+{_components.Count - shown}";
                GUI.Label(overflowRect, _overflowContent, HelpfulEditorGUI.BadgeStyle);
            }

            GUI.color = previousColor;

            return area.xMax - _iconRects[0].x;
        }

        private static string HierarchyPath(GameObject candidate)
        {
            string path = candidate.name;

            for (Transform parent = candidate.transform.parent; parent; parent = parent.parent)
            {
                path = $"{parent.name}/{path}";
            }

            return path;
        }

        /// <param name="additive">
        /// Shift, not Ctrl: the picker is opened with Ctrl held by default, so a Ctrl-click here
        /// would fire the moment someone clicked a row without having let go first.
        /// </param>
        private void Choose(int index, bool additive)
        {
            if (index < 0 || index >= _candidates.Count) return;

            GameObject target = _candidates[index];
            if (!target) return;

            if (additive)
            {
                List<Object> selection = new List<Object>(Selection.objects);

                if (selection.Contains(target)) selection.Remove(target);
                else selection.Add(target);

                Selection.objects = selection.ToArray();
                Repaint();
                return;
            }

            Selection.activeGameObject = target;
            Close();
        }

        private void OnDisable()
        {
            SceneViewPicker.SetHoverTarget(null);
        }
    }
}
