using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Rulers and draggable guides over a Game View. Guides are held as a fraction of the render
    /// target, so zooming, panning and resizing all leave them on the same game pixel.
    ///
    /// The rulers are pinned to the window under the toolbar and do cover the first 18 pixels of the
    /// game. That is deliberate: the Game View draws edge to edge and cannot be asked to keep clear,
    /// and every attempt to tuck the rulers into the surround around the render target only worked in
    /// Fixed Resolution — Free Aspect has no surround at all. The Rulers button in the toolbar hides
    /// them for the times that bite matters; the guides stay, and take the reclaimed strip.
    /// </summary>
    internal class GameViewGuidelinesDrawer : VisualElement
    {
        public const string OverlayName = "helpfuleditor-gameview-guidelines";

        private const float RulerSize = 18f;
        private const float LabelLength = 56f;
        private const float GrabThickness = 9f;
        private const float DragThicknessMultiplier = 3f;
        private const float ShiftIncrement = 10f;

        private readonly EditorWindow _gameView;
        private readonly GameViewGeometry _geometry = new GameViewGeometry();

        private readonly IMGUIContainer _drawLayer;
        private readonly VisualElement _corner;
        private readonly VisualElement _topRuler;
        private readonly VisualElement _leftRuler;
        private readonly VisualElement _grabLayer;

        /// <summary>
        /// Pooled rather than rebuilt. These elements carry the pointer capture that drives a drag,
        /// so destroying one to re-lay it out cancels the drag the user is in the middle of.
        /// </summary>
        private readonly List<VisualElement> _grabPool = new List<VisualElement>();

        private GUIStyle _labelStyle;

        private enum DragMode
        {
            None,
            Create,
            Move
        }

        private DragMode _dragMode = DragMode.None;
        private int _dragIndex = -1;
        private bool _dragHorizontal;
        private float _dragNormalized;
        private bool _dragAlt;
        private bool _dragShift;
        private Vector2 _lastPointer;
        private int _hoverIndex = -1;

        public GameViewGuidelinesDrawer(EditorWindow gameView)
        {
            _gameView = gameView;
            name = OverlayName;
            pickingMode = PickingMode.Ignore;
            Stretch(this);

            _grabLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            Stretch(_grabLayer);
            Add(_grabLayer);

            _corner = AddStrip(new Color(0.18f, 0.18f, 0.18f, 0.92f));
            _topRuler = AddStrip(new Color(0.22f, 0.22f, 0.22f, 0.92f));
            _leftRuler = AddStrip(new Color(0.22f, 0.22f, 0.22f, 0.92f));

            _corner.RegisterCallback<PointerDownEvent>(evt => OnRulerDown(evt, null));
            _topRuler.RegisterCallback<PointerDownEvent>(evt => OnRulerDown(evt, false));
            _leftRuler.RegisterCallback<PointerDownEvent>(evt => OnRulerDown(evt, true));

            RegisterDrag(_corner);
            RegisterDrag(_topRuler);
            RegisterDrag(_leftRuler);

            // Added last so ticks and numbers paint over the ruler strips instead of under them.
            _drawLayer = new IMGUIContainer(OnDrawGUI) { pickingMode = PickingMode.Ignore };
            Stretch(_drawLayer);
            Add(_drawLayer);

            RegisterCallback<GeometryChangedEvent>(_ => RefreshLayout());
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            _geometry.Update(_gameView, WindowRect);

            bool rulers = settings.showRulers;
            DisplayStyle display = rulers ? DisplayStyle.Flex : DisplayStyle.None;
            _corner.style.display = display;
            _topRuler.style.display = display;
            _leftRuler.style.display = display;

            if (rulers)
            {
                Rect ruler = RulerArea;

                SetRect(_corner, new Rect(ruler.x, ruler.y, RulerSize, RulerSize));
                SetRect(_topRuler, new Rect(ruler.x + RulerSize, ruler.y, Mathf.Max(0f, ruler.width - RulerSize), RulerSize));
                SetRect(_leftRuler, new Rect(ruler.x, ruler.y + RulerSize, RulerSize, Mathf.Max(0f, ruler.height - RulerSize)));
            }

            LayoutGrabTargets(settings);

            _drawLayer.MarkDirtyRepaint();
            _gameView.Repaint();
        }

        /// <summary>The overlay's own box, which is the whole window — unlike the game area, this never lags a resize.</summary>
        private Rect WindowRect
        {
            get
            {
                Rect rect = contentRect;
                return rect.width < 1f || rect.height < 1f ? layout : rect;
            }
        }

        /// <summary>
        /// Pinned to the window's drawable area — flush under the toolbar and out to the edges. The
        /// render target is not what they follow: it moves with the aspect mode and the zoom, which
        /// is what made the rulers wander in Free Aspect.
        /// </summary>
        private Rect RulerArea
        {
            get
            {
                Rect window = WindowRect;
                float top = Mathf.Clamp(_geometry.ContentRect.y, window.y, Mathf.Max(window.y, window.yMax - RulerSize));

                return new Rect(window.x, top, window.width, Mathf.Max(0f, window.yMax - top));
            }
        }

        /// <summary>Zero while the rulers are hidden, which hands their strip of the window back to the game.</summary>
        private static float RulerThickness => HelpfulEditorSettings.GameView.showRulers ? RulerSize : 0f;

        private Rect GuideArea
        {
            get
            {
                Rect ruler = RulerArea;
                float thickness = RulerThickness;

                return new Rect(ruler.x + thickness, ruler.y + thickness,
                    Mathf.Max(0f, ruler.width - thickness), Mathf.Max(0f, ruler.height - thickness));
            }
        }

        /// <summary>
        /// The part of the render target that is actually on show. Zoomed in, the game rect runs well
        /// past the window — drawing a guide across all of it puts the line over the rulers and the
        /// Game View's own toolbar, which are not ours to paint on.
        /// </summary>
        private Rect VisibleGameRect
        {
            get
            {
                Rect game = _geometry.GameRect;
                Rect area = GuideArea;

                return Rect.MinMaxRect(
                    Mathf.Max(game.xMin, area.xMin),
                    Mathf.Max(game.yMin, area.yMin),
                    Mathf.Min(game.xMax, area.xMax),
                    Mathf.Min(game.yMax, area.yMax));
            }
        }

        private void LayoutGrabTargets(GameViewSettings settings)
        {
            bool active = settings.showRulers && _geometry.HasUsableRect;
            Rect game = VisibleGameRect;

            for (int i = 0; i < settings.guides.Count; i++)
            {
                VisualElement grab = GetGrabTarget(i);
                GameViewGuide guide = settings.guides[i];

                float viewPos = _geometry.NormalizedToView(guide.isHorizontal, guide.normalizedPosition);

                // Clipped to what is on show for the same reason the line is: a grab target over the
                // rulers would take clicks meant for them.
                bool visible = active && game.width > 0f && game.height > 0f && (guide.isHorizontal
                    ? viewPos >= game.yMin && viewPos <= game.yMax
                    : viewPos >= game.xMin && viewPos <= game.xMax);

                grab.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible) continue;

                SetRect(grab, guide.isHorizontal
                    ? new Rect(game.x, viewPos - GrabThickness * 0.5f, game.width, GrabThickness)
                    : new Rect(viewPos - GrabThickness * 0.5f, game.y, GrabThickness, game.height));
            }

            for (int i = settings.guides.Count; i < _grabPool.Count; i++)
            {
                _grabPool[i].style.display = DisplayStyle.None;
            }
        }

        private VisualElement GetGrabTarget(int index)
        {
            while (_grabPool.Count <= index)
            {
                int captured = _grabPool.Count;
                VisualElement grab = new VisualElement { pickingMode = PickingMode.Position };

                grab.RegisterCallback<PointerDownEvent>(evt => OnGuideDown(evt, captured));
                grab.RegisterCallback<PointerEnterEvent>(_ => SetHover(captured));
                grab.RegisterCallback<PointerLeaveEvent>(_ => ClearHover(captured));
                RegisterDrag(grab);

                _grabPool.Add(grab);
                _grabLayer.Add(grab);
            }

            return _grabPool[index];
        }

        private void RegisterDrag(VisualElement element)
        {
            element.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            element.RegisterCallback<PointerUpEvent>(OnPointerUp);
            element.RegisterCallback<PointerCaptureOutEvent>(_ => FinishDrag(_lastPointer));
        }

        private void SetHover(int index)
        {
            _hoverIndex = index;
            _gameView.Repaint();
        }

        private void ClearHover(int index)
        {
            if (_hoverIndex != index) return;

            _hoverIndex = -1;
            _gameView.Repaint();
        }

        /// <summary>A null axis means the corner, which only offers the menu.</summary>
        private void OnRulerDown(PointerDownEvent evt, bool? horizontal)
        {
            if (evt.button == 1)
            {
                GameViewModule.ShowGuideMenu();
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0 || horizontal == null) return;

            if (!_geometry.HasUsableRect) return;

            _dragMode = DragMode.Create;
            _dragIndex = -1;
            _dragHorizontal = horizontal.Value;

            BeginDrag(evt, evt.currentTarget as VisualElement);
        }

        private void OnGuideDown(PointerDownEvent evt, int index)
        {
            if (evt.button == 1)
            {
                GameViewModule.ShowGuideMenu();
                evt.StopPropagation();
                return;
            }

            GameViewSettings settings = HelpfulEditorSettings.GameView;
            if (evt.button != 0 || index < 0 || index >= settings.guides.Count) return;

            _dragMode = DragMode.Move;
            _dragIndex = index;
            _dragHorizontal = settings.guides[index].isHorizontal;
            _dragNormalized = settings.guides[index].normalizedPosition;

            BeginDrag(evt, evt.currentTarget as VisualElement);
        }

        private void BeginDrag(PointerDownEvent evt, VisualElement capture)
        {
            _dragAlt = evt.altKey;
            _dragShift = evt.shiftKey;
            _lastPointer = this.WorldToLocal((Vector2)evt.position);

            UpdatePreview(_lastPointer);

            capture?.CapturePointer(evt.pointerId);
            evt.StopPropagation();

            _drawLayer.MarkDirtyRepaint();
            _gameView.Repaint();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_dragMode == DragMode.None) return;

            _dragAlt = evt.altKey;
            _dragShift = evt.shiftKey;
            _lastPointer = this.WorldToLocal((Vector2)evt.position);

            UpdatePreview(_lastPointer);

            _drawLayer.MarkDirtyRepaint();
            _gameView.Repaint();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_dragMode == DragMode.None || evt.button != 0) return;

            _dragAlt = evt.altKey;
            _dragShift = evt.shiftKey;
            FinishDrag(this.WorldToLocal((Vector2)evt.position));

            if (evt.currentTarget is VisualElement target && target.HasPointerCapture(evt.pointerId))
            {
                target.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        /// <summary>Dropping outside the game area deletes, which is the same gesture image editors use.</summary>
        private void FinishDrag(Vector2 pointer)
        {
            if (_dragMode == DragMode.None) return;

            UpdatePreview(pointer);

            GameViewSettings settings = HelpfulEditorSettings.GameView;
            bool delete = !GuideArea.Contains(pointer);

            if (_dragMode == DragMode.Create && !delete)
            {
                settings.guides.Add(new GameViewGuide
                {
                    isHorizontal = _dragHorizontal,
                    normalizedPosition = _dragNormalized
                });

                HelpfulEditorSettings.SaveGameView();
            }
            else if (_dragMode == DragMode.Move && _dragIndex >= 0 && _dragIndex < settings.guides.Count)
            {
                if (delete) settings.guides.RemoveAt(_dragIndex);
                else settings.guides[_dragIndex].normalizedPosition = _dragNormalized;

                HelpfulEditorSettings.SaveGameView();
            }

            _dragMode = DragMode.None;
            _dragIndex = -1;
            _hoverIndex = -1;

            RefreshLayout();
        }

        private void UpdatePreview(Vector2 pointer)
        {
            float normalized = _geometry.ViewToNormalized(_dragHorizontal, _dragHorizontal ? pointer.y : pointer.x);

            if (_dragAlt)
            {
                normalized = 0.5f;
            }
            else if (_dragShift)
            {
                float axis = Mathf.Max(1f, _dragHorizontal ? _geometry.GameSize.y : _geometry.GameSize.x);
                normalized = Mathf.Round(normalized * axis / ShiftIncrement) * ShiftIncrement / axis;
            }

            _dragNormalized = Mathf.Clamp01(normalized);
        }

        private void OnDrawGUI()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            // The toolbar button takes the guides with it: rulers off is the way to get an unobstructed
            // look at the game, which a set of guides left drawn over it would rather defeat.
            if (!settings.showRulers) return;

            _geometry.Update(_gameView, WindowRect);
            if (!_geometry.HasUsableRect) return;

            EnsureLabelStyle();

            Handles.BeginGUI();

            DrawRulers();
            DrawGuides(settings);
            ApplyCursors(settings);

            if (_dragMode != DragMode.None)
            {
                float width = Mathf.Max(0.5f, settings.guideWidth);
                DrawGuide(_dragHorizontal, _dragNormalized, settings.guideColor, Mathf.Max(width * DragThicknessMultiplier, width + 2f));
                DrawReadout();
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// Ticks are labelled in game pixels rather than window pixels, so the numbers match the
        /// resolution dropdown at any zoom. Zero and the far edge are always included, so the render
        /// target's own bounds are always readable.
        /// </summary>
        private void DrawRulers()
        {
            Rect ruler = RulerArea;
            Rect guideArea = GuideArea;
            Rect visible = _geometry.VisibleGamePixels;
            Color tick = new Color(0.85f, 0.85f, 0.85f, 0.95f);

            float stepX = NiceStep(Mathf.Max(1f, visible.width));
            for (float value = Mathf.Floor(visible.xMin / stepX) * stepX; value <= visible.xMax + stepX * 0.01f; value += stepX)
            {
                DrawTick(Mathf.Round(value), true, ruler, guideArea, stepX, tick);
            }

            DrawTick(0f, true, ruler, guideArea, stepX, tick);
            DrawTick(Mathf.Round(_geometry.GameSize.x), true, ruler, guideArea, stepX, tick);

            float stepY = NiceStep(Mathf.Max(1f, visible.height));
            for (float value = Mathf.Floor(visible.yMin / stepY) * stepY; value <= visible.yMax + stepY * 0.01f; value += stepY)
            {
                DrawTick(Mathf.Round(value), false, ruler, guideArea, stepY, tick);
            }

            DrawTick(0f, false, ruler, guideArea, stepY, tick);
            DrawTick(Mathf.Round(_geometry.GameSize.y), false, ruler, guideArea, stepY, tick);
        }

        private void DrawTick(float gamePixel, bool horizontalAxis, Rect ruler, Rect guideArea, float step, Color tickColor)
        {
            float position = horizontalAxis
                ? _geometry.GamePixelToView(new Vector2(gamePixel, 0f)).x
                : _geometry.GamePixelToView(new Vector2(0f, gamePixel)).y;

            if (horizontalAxis)
            {
                if (position < guideArea.xMin - 1f || position > guideArea.xMax + 1f) return;
            }
            else if (position < guideArea.yMin - 1f || position > guideArea.yMax + 1f)
            {
                return;
            }

            float axisSize = horizontalAxis ? _geometry.GameSize.x : _geometry.GameSize.y;
            bool endpoint = Mathf.Approximately(gamePixel, 0f) || Mathf.Approximately(gamePixel, Mathf.Round(axisSize));
            bool major = endpoint || Mathf.RoundToInt(gamePixel / step) % 2 == 0;

            float length = RulerSize * (major ? 0.65f : 0.35f);
            Color color = major ? Color.white : tickColor;

            if (horizontalAxis)
            {
                DrawLine(new Vector2(position, ruler.y + RulerSize), new Vector2(position, ruler.y + RulerSize - length), color, 1f);

                if (!major) return;

                // The far edge label is pulled inwards so it does not run off the ruler.
                float labelX = endpoint && gamePixel > 0f ? position - 40f : position + 2f;
                DrawLabel(new Rect(labelX, ruler.y, LabelLength, RulerSize), Mathf.RoundToInt(gamePixel).ToString());
                return;
            }

            DrawLine(new Vector2(ruler.x + RulerSize, position), new Vector2(ruler.x + RulerSize - length, position), color, 1f);

            if (!major) return;

            // Rotating by -90 turns the label's own left-to-right into bottom-to-top, so the rect's
            // left edge ends up below the tick and its right edge above. Every label therefore starts
            // at the pivot and reads upwards — except zero, whose tick sits at the very top of the
            // game: running upwards from there would put it in the toolbar, so it is right-aligned
            // instead and ends at the tick, growing downwards into the ruler.
            bool zero = endpoint && gamePixel <= 0f;
            float pivotX = ruler.x + RulerSize * 0.5f;
            Rect labelRect = zero
                ? new Rect(pivotX - LabelLength, position - 8f, LabelLength, 16f)
                : new Rect(pivotX + 2f, position - 8f, LabelLength, 16f);

            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(-90f, new Vector2(pivotX, position));
            DrawLabel(labelRect, Mathf.RoundToInt(gamePixel).ToString(), zero ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            GUI.matrix = previous;
        }

        /// <summary>Drawn offset in four directions first, so the number stays legible over any game content.</summary>
        private void DrawLabel(Rect rect, string text, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            _labelStyle.alignment = alignment;

            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.95f);
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, _labelStyle);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, _labelStyle);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, _labelStyle);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, _labelStyle);

            GUI.color = Color.white;
            GUI.Label(rect, text, _labelStyle);
            GUI.color = previous;
        }

        private void DrawGuides(GameViewSettings settings)
        {
            float baseWidth = Mathf.Max(0.5f, settings.guideWidth);

            for (int i = 0; i < settings.guides.Count; i++)
            {
                if (_dragMode == DragMode.Move && i == _dragIndex) continue;

                GameViewGuide guide = settings.guides[i];
                DrawGuide(guide.isHorizontal, guide.normalizedPosition, settings.guideColor, _hoverIndex == i ? baseWidth + 1f : baseWidth);
            }
        }

        private void DrawGuide(bool horizontal, float normalized, Color color, float thickness)
        {
            Rect visible = VisibleGameRect;
            if (visible.width <= 0f || visible.height <= 0f) return;

            float position = _geometry.NormalizedToView(horizontal, normalized);

            if (horizontal)
            {
                if (position < visible.yMin || position > visible.yMax) return;
                DrawLine(new Vector2(visible.xMin, position), new Vector2(visible.xMax, position), color, thickness);
                return;
            }

            if (position < visible.xMin || position > visible.xMax) return;
            DrawLine(new Vector2(position, visible.yMin), new Vector2(position, visible.yMax), color, thickness);
        }

        private void DrawReadout()
        {
            Rect guideArea = GuideArea;
            float position = _geometry.NormalizedToView(_dragHorizontal, _dragNormalized);
            int pixels = Mathf.RoundToInt(_dragNormalized * (_dragHorizontal ? _geometry.GameSize.y : _geometry.GameSize.x));

            Vector2 origin = _dragHorizontal
                ? new Vector2(guideArea.x + 8f, position + 4f)
                : new Vector2(position + 4f, guideArea.y + 8f);

            string suffix = _dragAlt ? " (centre)" : _dragShift ? $" (×{ShiftIncrement:0})" : string.Empty;
            string label = $"{pixels}px{suffix}";

            Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(label));
            GUI.Box(new Rect(origin.x, origin.y, size.x + 8f, size.y), label, EditorStyles.helpBox);
        }

        private void ApplyCursors(GameViewSettings settings)
        {
            if (_dragMode != DragMode.None)
            {
                // Over the whole window, not just the guide area: the drag is most interesting once
                // the pointer has left that area, because that is when letting go deletes.
                bool willDelete = !GuideArea.Contains(_lastPointer);

                EditorGUIUtility.AddCursorRect(WindowRect, willDelete
                    ? MouseCursor.ArrowMinus
                    : _dragHorizontal ? MouseCursor.ResizeVertical : MouseCursor.ResizeHorizontal);

                return;
            }

            if (_hoverIndex < 0 || _hoverIndex >= settings.guides.Count) return;

            GameViewGuide guide = settings.guides[_hoverIndex];
            Rect game = VisibleGameRect;
            float position = _geometry.NormalizedToView(guide.isHorizontal, guide.normalizedPosition);

            Rect hitRect = guide.isHorizontal
                ? new Rect(game.x, position - GrabThickness * 0.5f, game.width, GrabThickness)
                : new Rect(position - GrabThickness * 0.5f, game.y, GrabThickness, game.height);

            EditorGUIUtility.AddCursorRect(hitRect, guide.isHorizontal ? MouseCursor.ResizeVertical : MouseCursor.ResizeHorizontal);
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, from, to);
        }

        /// <summary>Spacing rounded to 1, 2 or 5 times a power of ten, so labels read cleanly at any zoom.</summary>
        private static float NiceStep(float size)
        {
            if (size <= 0f) return 50f;

            float rough = size / 8f;
            float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(rough, 1f))));
            float normalized = rough / magnitude;
            float nice = normalized < 1.5f ? 1f : normalized < 3.5f ? 2f : normalized < 7.5f ? 5f : 10f;

            return Mathf.Max(5f, nice * magnitude);
        }

        private void EnsureLabelStyle()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow
            };

            _labelStyle.normal.textColor = Color.white;
        }

        private VisualElement AddStrip(Color background)
        {
            VisualElement strip = new VisualElement { pickingMode = PickingMode.Position };

            strip.style.position = Position.Absolute;
            strip.style.backgroundColor = background;
            Add(strip);

            return strip;
        }

        private static void Stretch(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
        }

        private static void SetRect(VisualElement element, Rect rect)
        {
            element.style.position = Position.Absolute;
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
        }
    }
}
