using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// Rulers and draggable guides, pinned to the Scene View window itself.
    ///
    /// They are a VisualElement rather than something drawn in the scene GUI pass because that is the
    /// only way they hold still. Anything drawn with Handles lives in the scene, so it pans and zooms
    /// with the canvas — and a ruler that slides about is not a ruler. The geometry they lay out
    /// against is pushed in from the scene GUI pass instead, which is the only place HandleUtility can
    /// project the canvas onto the window.
    ///
    /// The strips do cover the top-left 18 pixels of the view, and whatever Scene View overlay is
    /// sitting there with them. That is the same bite the Game View rulers take, and it is settled the
    /// same way: the toolbar toggle takes them off when it matters.
    /// </summary>
    internal class SceneViewGuidesDrawer : VisualElement
    {
        public const string OverlayName = "helpfuleditor-sceneview-guidelines";

        private const float RulerSize = 18f;
        private const float LabelLength = 56f;
        private const float GrabThickness = 9f;
        private const float DragThicknessMultiplier = 3f;
        private const float ShiftIncrement = 10f;

        private readonly SceneView _sceneView;
        private readonly IMGUIContainer _drawLayer;
        private readonly VisualElement _corner;
        private readonly VisualElement _topRuler;
        private readonly VisualElement _leftRuler;
        private readonly VisualElement _grabLayer;

        /// <summary>
        /// Pooled rather than rebuilt. These carry the pointer capture that drives a drag, so throwing
        /// one away to re-lay it out cancels the drag the user is in the middle of.
        /// </summary>
        private readonly List<VisualElement> _grabPool = new List<VisualElement>();

        private GUIStyle _labelStyle;

        private Rect _canvasRect;
        private Vector2 _referenceSize = Vector2.one;
        private bool _axisAligned;
        private bool _hasTarget;

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

        public SceneViewGuidesDrawer(SceneView sceneView)
        {
            _sceneView = sceneView;
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

            // Added last so the ticks and numbers paint over the strips rather than under them.
            //
            // Not focusable, and that is not a detail. An IMGUIContainer takes keyboard focus by
            // default, and this one is stretched across the whole Scene View — with it focusable it
            // takes the key events that the suite's keybinds are routed through, so every hover
            // keybind in the suite stops working the moment this window has focus.
            _drawLayer = new IMGUIContainer(OnDrawGUI) { pickingMode = PickingMode.Ignore, focusable = false };
            Stretch(_drawLayer);
            Add(_drawLayer);

            RegisterCallback<GeometryChangedEvent>(_ => RefreshLayout());
        }

        /// <summary>
        /// Where the canvas landed on the window, handed over from the scene GUI pass. Nothing here can
        /// work that out for itself — HandleUtility only projects inside that pass.
        /// </summary>
        public void SetGeometry(Rect canvasRect, Vector2 referenceSize, bool axisAligned, bool hasTarget)
        {
            bool changed = _canvasRect != canvasRect
                           || _referenceSize != referenceSize
                           || _axisAligned != axisAligned
                           || _hasTarget != hasTarget;

            _canvasRect = canvasRect;
            _referenceSize = referenceSize;
            _axisAligned = axisAligned;
            _hasTarget = hasTarget;

            if (changed) RefreshLayout();
        }

        public void RefreshLayout()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;

            // Off angle there is nothing the rulers could be numbered in, so they go rather than
            // showing a scale that does not describe anything on screen.
            bool active = settings.showRulers && _hasTarget && _axisAligned;
            DisplayStyle display = active ? DisplayStyle.Flex : DisplayStyle.None;

            _corner.style.display = display;
            _topRuler.style.display = display;
            _leftRuler.style.display = display;

            if (active)
            {
                Rect window = WindowRect;

                SetRect(_corner, new Rect(window.x, window.y, RulerSize, RulerSize));
                SetRect(_topRuler, new Rect(window.x + RulerSize, window.y, Mathf.Max(0f, window.width - RulerSize), RulerSize));
                SetRect(_leftRuler, new Rect(window.x, window.y + RulerSize, RulerSize, Mathf.Max(0f, window.height - RulerSize)));
            }

            LayoutGrabTargets(settings, active);

            _drawLayer.MarkDirtyRepaint();
        }

        /// <summary>The overlay's own box, which is the whole window — unlike a laid-out child it never lags a resize.</summary>
        private Rect WindowRect
        {
            get
            {
                Rect rect = contentRect;
                return rect.width < 1f || rect.height < 1f ? layout : rect;
            }
        }

        private static float RulerThickness => HelpfulEditorSettings.SceneView.showRulers ? RulerSize : 0f;

        /// <summary>The window minus the strips, which is everything a guide is allowed to be dropped in.</summary>
        private Rect GuideArea
        {
            get
            {
                Rect window = WindowRect;
                float thickness = RulerThickness;

                return new Rect(window.x + thickness, window.y + thickness,
                    Mathf.Max(0f, window.width - thickness), Mathf.Max(0f, window.height - thickness));
            }
        }

        /// <summary>
        /// The part of the canvas actually on show. Zoomed in, the canvas runs well past the window —
        /// grab targets stretched across all of it would take clicks meant for the rulers.
        /// </summary>
        private Rect VisibleCanvasRect
        {
            get
            {
                Rect area = GuideArea;

                return Rect.MinMaxRect(
                    Mathf.Max(_canvasRect.xMin, area.xMin),
                    Mathf.Max(_canvasRect.yMin, area.yMin),
                    Mathf.Min(_canvasRect.xMax, area.xMax),
                    Mathf.Min(_canvasRect.yMax, area.yMax));
            }
        }

        private void LayoutGrabTargets(SceneViewSettings settings, bool active)
        {
            Rect canvas = VisibleCanvasRect;

            for (int i = 0; i < settings.guides.Count; i++)
            {
                VisualElement grab = GetGrabTarget(i);
                SceneViewGuide guide = settings.guides[i];

                float viewPos = NormalizedToView(guide.isHorizontal, guide.normalizedPosition);

                bool visible = active && canvas.width > 0f && canvas.height > 0f && (guide.isHorizontal
                    ? viewPos >= canvas.yMin && viewPos <= canvas.yMax
                    : viewPos >= canvas.xMin && viewPos <= canvas.xMax);

                grab.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible) continue;

                SetRect(grab, guide.isHorizontal
                    ? new Rect(canvas.x, viewPos - GrabThickness * 0.5f, canvas.width, GrabThickness)
                    : new Rect(viewPos - GrabThickness * 0.5f, canvas.y, GrabThickness, canvas.height));
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
            Repaint();
        }

        private void ClearHover(int index)
        {
            if (_hoverIndex != index) return;

            _hoverIndex = -1;
            Repaint();
        }

        /// <summary>A null axis means the corner, which only offers the menu.</summary>
        private void OnRulerDown(PointerDownEvent evt, bool? horizontal)
        {
            if (evt.button == 1)
            {
                SceneViewGuides.ShowGuideMenu();
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0 || horizontal == null) return;
            if (!_hasTarget || !_axisAligned) return;

            _dragMode = DragMode.Create;
            _dragIndex = -1;
            _dragHorizontal = horizontal.Value;

            BeginDrag(evt, evt.currentTarget as VisualElement);
        }

        private void OnGuideDown(PointerDownEvent evt, int index)
        {
            if (evt.button == 1)
            {
                SceneViewGuides.ShowGuideMenu();
                evt.StopPropagation();
                return;
            }

            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
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
            Repaint();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_dragMode == DragMode.None) return;

            _dragAlt = evt.altKey;
            _dragShift = evt.shiftKey;
            _lastPointer = this.WorldToLocal((Vector2)evt.position);

            UpdatePreview(_lastPointer);

            _drawLayer.MarkDirtyRepaint();
            Repaint();
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

        /// <summary>Dropping outside the canvas deletes, which is the gesture image editors already use.</summary>
        private void FinishDrag(Vector2 pointer)
        {
            if (_dragMode == DragMode.None) return;

            UpdatePreview(pointer);

            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            bool delete = !GuideArea.Contains(pointer) || !_canvasRect.Contains(pointer);

            if (_dragMode == DragMode.Create && !delete)
            {
                settings.guides.Add(new SceneViewGuide
                {
                    isHorizontal = _dragHorizontal,
                    normalizedPosition = _dragNormalized
                });

                HelpfulEditorSettings.SaveSceneView();
            }
            else if (_dragMode == DragMode.Move && _dragIndex >= 0 && _dragIndex < settings.guides.Count)
            {
                if (delete) settings.guides.RemoveAt(_dragIndex);
                else settings.guides[_dragIndex].normalizedPosition = _dragNormalized;

                HelpfulEditorSettings.SaveSceneView();
            }

            _dragMode = DragMode.None;
            _dragIndex = -1;
            _hoverIndex = -1;

            RefreshLayout();
            Repaint();
        }

        private void UpdatePreview(Vector2 pointer)
        {
            float normalized = ViewToNormalized(_dragHorizontal, _dragHorizontal ? pointer.y : pointer.x);

            if (_dragAlt)
            {
                normalized = 0.5f;
            }
            else if (_dragShift)
            {
                float axis = Mathf.Max(1f, _dragHorizontal ? _referenceSize.y : _referenceSize.x);
                normalized = Mathf.Round(normalized * axis / ShiftIncrement) * ShiftIncrement / axis;
            }

            _dragNormalized = Mathf.Clamp01(normalized);
        }

        /// <summary>The guide being dragged, which the scene pass draws in place of its stored position.</summary>
        public bool TryGetPreview(out bool horizontal, out float normalized, out int movedIndex)
        {
            horizontal = _dragHorizontal;
            normalized = _dragNormalized;
            movedIndex = _dragMode == DragMode.Move ? _dragIndex : -1;

            return _dragMode != DragMode.None;
        }

        public int HoverIndex => _dragMode == DragMode.None ? _hoverIndex : -1;

        private float NormalizedToView(bool horizontal, float normalized)
        {
            return horizontal
                ? _canvasRect.y + normalized * _canvasRect.height
                : _canvasRect.x + normalized * _canvasRect.width;
        }

        private float ViewToNormalized(bool horizontal, float viewPos)
        {
            float size = horizontal ? _canvasRect.height : _canvasRect.width;
            if (Mathf.Abs(size) < 0.0001f) return 0.5f;

            return (viewPos - (horizontal ? _canvasRect.y : _canvasRect.x)) / size;
        }

        private float ReferencePixelsToView(bool horizontal, float pixels)
        {
            float size = Mathf.Max(1f, horizontal ? _referenceSize.y : _referenceSize.x);

            return NormalizedToView(horizontal, pixels / size);
        }

        private float ViewToReferencePixels(bool horizontal, float viewPos)
        {
            return ViewToNormalized(horizontal, viewPos) * (horizontal ? _referenceSize.y : _referenceSize.x);
        }

        private void OnDrawGUI()
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            if (!settings.showRulers || !_hasTarget || !_axisAligned) return;

            EnsureLabelStyle();

            Handles.BeginGUI();

            DrawRulers();
            ApplyCursors(settings);

            if (_dragMode != DragMode.None) DrawReadout();

            Handles.EndGUI();
        }

        /// <summary>
        /// Ticks are numbered in the canvas' own units, so they match the reference resolution rather
        /// than the zoom. Zero and the far edge are always drawn, so the canvas' bounds stay readable.
        /// </summary>
        private void DrawRulers()
        {
            Rect window = WindowRect;
            Rect guideArea = GuideArea;
            Rect visible = VisibleCanvasRect;
            Color tick = new Color(0.85f, 0.85f, 0.85f, 0.95f);

            float minX = ViewToReferencePixels(false, visible.xMin);
            float maxX = ViewToReferencePixels(false, visible.xMax);
            float stepX = NiceStep(maxX - minX);

            for (float value = Mathf.Floor(minX / stepX) * stepX; value <= maxX + stepX * 0.01f; value += stepX)
            {
                DrawTick(Mathf.Round(value), true, window, guideArea, stepX, tick);
            }

            DrawTick(0f, true, window, guideArea, stepX, tick);
            DrawTick(Mathf.Round(_referenceSize.x), true, window, guideArea, stepX, tick);

            float minY = ViewToReferencePixels(true, visible.yMin);
            float maxY = ViewToReferencePixels(true, visible.yMax);
            float stepY = NiceStep(maxY - minY);

            for (float value = Mathf.Floor(minY / stepY) * stepY; value <= maxY + stepY * 0.01f; value += stepY)
            {
                DrawTick(Mathf.Round(value), false, window, guideArea, stepY, tick);
            }

            DrawTick(0f, false, window, guideArea, stepY, tick);
            DrawTick(Mathf.Round(_referenceSize.y), false, window, guideArea, stepY, tick);
        }

        private void DrawTick(float canvasPixel, bool horizontalAxis, Rect window, Rect guideArea, float step, Color tickColor)
        {
            float position = ReferencePixelsToView(!horizontalAxis, canvasPixel);

            if (horizontalAxis)
            {
                if (position < guideArea.xMin - 1f || position > guideArea.xMax + 1f) return;
            }
            else if (position < guideArea.yMin - 1f || position > guideArea.yMax + 1f)
            {
                return;
            }

            float axisSize = horizontalAxis ? _referenceSize.x : _referenceSize.y;
            bool endpoint = Mathf.Approximately(canvasPixel, 0f) || Mathf.Approximately(canvasPixel, Mathf.Round(axisSize));
            bool major = endpoint || Mathf.RoundToInt(canvasPixel / step) % 2 == 0;

            float length = RulerSize * (major ? 0.65f : 0.35f);
            Color color = major ? Color.white : tickColor;

            if (horizontalAxis)
            {
                DrawLine(new Vector2(position, window.y + RulerSize), new Vector2(position, window.y + RulerSize - length), color);

                if (!major) return;

                // The far edge label is pulled inwards so it does not run off the end of the ruler.
                float labelX = endpoint && canvasPixel > 0f ? position - 40f : position + 2f;
                DrawLabel(new Rect(labelX, window.y, LabelLength, RulerSize), Mathf.RoundToInt(canvasPixel).ToString());
                return;
            }

            DrawLine(new Vector2(window.x + RulerSize, position), new Vector2(window.x + RulerSize - length, position), color);

            if (!major) return;

            // Rotating by -90 turns the label's own left-to-right into bottom-to-top, so it starts at
            // the pivot and reads upwards. Zero is the exception: its tick sits at the top of the
            // canvas, and running upwards from there would take it off the ruler, so it is right
            // aligned instead and ends at the tick.
            bool zero = endpoint && canvasPixel <= 0f;
            float pivotX = window.x + RulerSize * 0.5f;

            Rect labelRect = zero
                ? new Rect(pivotX - LabelLength, position - 8f, LabelLength, 16f)
                : new Rect(pivotX + 2f, position - 8f, LabelLength, 16f);

            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(-90f, new Vector2(pivotX, position));
            DrawLabel(labelRect, Mathf.RoundToInt(canvasPixel).ToString(), zero ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            GUI.matrix = previous;
        }

        /// <summary>Drawn offset in four directions first, so the number stays legible over any content.</summary>
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

        private void DrawReadout()
        {
            Rect guideArea = GuideArea;
            float position = NormalizedToView(_dragHorizontal, _dragNormalized);
            int pixels = Mathf.RoundToInt(_dragNormalized * (_dragHorizontal ? _referenceSize.y : _referenceSize.x));

            Vector2 origin = _dragHorizontal
                ? new Vector2(guideArea.x + 8f, position + 4f)
                : new Vector2(position + 4f, guideArea.y + 8f);

            string suffix = _dragAlt ? " (centre)" : _dragShift ? $" (×{ShiftIncrement:0})" : string.Empty;
            string label = $"{pixels}px{suffix}";

            Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(label));
            GUI.Box(new Rect(origin.x, origin.y, size.x + 8f, size.y), label, EditorStyles.helpBox);
        }

        private void ApplyCursors(SceneViewSettings settings)
        {
            if (_dragMode != DragMode.None)
            {
                // Over the whole window, not just the canvas: the drag is most interesting once the
                // pointer has left it, because that is when letting go throws the guide away.
                bool willDelete = !GuideArea.Contains(_lastPointer) || !_canvasRect.Contains(_lastPointer);

                EditorGUIUtility.AddCursorRect(WindowRect, willDelete ? MouseCursor.ArrowMinus : MouseCursor.MoveArrow);

                return;
            }

            // A plus rather than a resize arrow: dragging off a ruler makes a new guide, it does not
            // stretch anything — and it pairs with the minus the drag turns into once letting go would
            // throw one away.
            EditorGUIUtility.AddCursorRect(_topRuler.layout, MouseCursor.ArrowPlus);
            EditorGUIUtility.AddCursorRect(_leftRuler.layout, MouseCursor.ArrowPlus);

            if (_hoverIndex < 0 || _hoverIndex >= settings.guides.Count) return;

            SceneViewGuide guide = settings.guides[_hoverIndex];
            Rect canvas = VisibleCanvasRect;
            float position = NormalizedToView(guide.isHorizontal, guide.normalizedPosition);

            Rect hitRect = guide.isHorizontal
                ? new Rect(canvas.x, position - GrabThickness * 0.5f, canvas.width, GrabThickness)
                : new Rect(position - GrabThickness * 0.5f, canvas.y, GrabThickness, canvas.height);

            EditorGUIUtility.AddCursorRect(hitRect, MouseCursor.MoveArrow);
        }

        private void Repaint()
        {
            if (_sceneView) _sceneView.Repaint();
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(1f, from, to);
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
