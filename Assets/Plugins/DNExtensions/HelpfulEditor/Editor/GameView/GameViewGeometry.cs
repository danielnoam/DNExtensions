using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// The only place that reaches into GameView's internals, and the only thing that knows where the
    /// game is on screen. Everything the overlay draws is expressed against the render target's rect,
    /// so resolving that once per refresh is enough — and it reports whether it managed, because an
    /// overlay drawing confidently in the wrong place is worse than one that admits it is lost.
    /// </summary>
    internal class GameViewGeometry
    {
        private const float ToolbarHeight = 19f;

        private static FieldInfo _zoomAreaField;
        private static PropertyInfo _drawRect;
        private static PropertyInfo _shownArea;
        private static MethodInfo _drawingToViewPoint;
        private static MethodInfo _drawingToViewVector;
        private static PropertyInfo _targetSize;
        private static bool _reflectionResolved;

        /// <summary>Whether the last update came from GameView itself rather than a guess at the window.</summary>
        public bool Resolved { get; private set; }

        /// <summary>Where the render target lands, in overlay-local coordinates.</summary>
        public Rect GameRect { get; private set; }

        /// <summary>
        /// The window's drawable area — everything below the toolbar. Unlike GameRect this does not
        /// move with the aspect mode or the zoom, which is what makes it the one thing worth pinning
        /// the rulers to.
        /// </summary>
        public Rect ContentRect { get; private set; }

        /// <summary>Size of the render target, in game pixels.</summary>
        public Vector2 GameSize { get; private set; } = Vector2.one;

        /// <summary>The span of game pixels currently on screen, which is what the rulers are labelled with.</summary>
        public Rect VisibleGamePixels { get; private set; }

        public void Update(EditorWindow gameView, Rect windowRect)
        {
            ResolveReflection();

            object zoomArea = GetZoomArea(gameView);

            GameSize = ResolveGameSize(gameView, windowRect);
            ContentRect = ResolveDrawRect(zoomArea, windowRect);
            Rect drawRect = ContentRect;
            Rect targetInContent = TargetInContent(GameSize);

            GameRect = ResolveGameRect(zoomArea, drawRect, targetInContent);
            VisibleGamePixels = ResolveVisiblePixels(zoomArea, targetInContent);

            Resolved = zoomArea != null && _targetSize != null;
        }

        public Vector2 GamePixelToView(Vector2 gamePixel)
        {
            return new Vector2(
                GameRect.x + gamePixel.x / Mathf.Max(1f, GameSize.x) * GameRect.width,
                GameRect.y + gamePixel.y / Mathf.Max(1f, GameSize.y) * GameRect.height);
        }

        public Vector2 ViewToGamePixel(Vector2 viewPos)
        {
            float nx = GameRect.width > 0.0001f ? (viewPos.x - GameRect.x) / GameRect.width : 0.5f;
            float ny = GameRect.height > 0.0001f ? (viewPos.y - GameRect.y) / GameRect.height : 0.5f;

            return new Vector2(nx * GameSize.x, ny * GameSize.y);
        }

        /// <summary>Where a guide at this fraction of the render target sits on screen.</summary>
        public float NormalizedToView(bool horizontal, float normalized)
        {
            float t = Mathf.Clamp01(normalized);
            return horizontal ? GameRect.y + t * GameRect.height : GameRect.x + t * GameRect.width;
        }

        public float ViewToNormalized(bool horizontal, float viewPos)
        {
            Vector2 gamePixel = ViewToGamePixel(new Vector2(viewPos, viewPos));
            float axis = Mathf.Max(1f, horizontal ? GameSize.y : GameSize.x);

            return Mathf.Clamp01((horizontal ? gamePixel.y : gamePixel.x) / axis);
        }

        /// <summary>Whether the game is being drawn at a size worth putting guides on at all.</summary>
        public bool HasUsableRect => GameRect.width > 0.5f && GameRect.height > 0.5f;

        private Vector2 ResolveGameSize(EditorWindow gameView, Rect windowRect)
        {
            if (_targetSize != null)
            {
                try
                {
                    if (_targetSize.GetValue(gameView, null) is Vector2 size && size.x > 1f && size.y > 1f) return size;
                }
                catch (Exception)
                {
                    // Falls through to the window size below.
                }
            }

            return new Vector2(Mathf.Max(1f, windowRect.width), Mathf.Max(1f, windowRect.height - ToolbarHeight));
        }

        private static Rect ResolveDrawRect(object zoomArea, Rect windowRect)
        {
            if (zoomArea != null && _drawRect != null)
            {
                try
                {
                    Rect rect = (Rect)_drawRect.GetValue(zoomArea, null);
                    if (rect.width > 1f && rect.height > 1f) return rect;
                }
                catch (Exception)
                {
                    // Falls through.
                }
            }

            return new Rect(0f, ToolbarHeight, windowRect.width, Mathf.Max(0f, windowRect.height - ToolbarHeight));
        }

        /// <summary>
        /// GameView can draw with a negative Y scale, which arrives here as a negative height, so the
        /// rect is normalised before anyone downstream tries to use its edges.
        /// </summary>
        private static Rect ResolveGameRect(object zoomArea, Rect drawRect, Rect targetInContent)
        {
            Vector2 position = DrawingToView(zoomArea, _drawingToViewPoint, targetInContent.position, targetInContent, drawRect, true);
            Vector2 size = DrawingToView(zoomArea, _drawingToViewVector, targetInContent.size, targetInContent, drawRect, false);

            Rect local = new Rect(position, size);

            if (local.width < 0f)
            {
                local.x += local.width;
                local.width = -local.width;
            }

            if (local.height < 0f)
            {
                local.y += local.height;
                local.height = -local.height;
            }

            return new Rect(local.x + drawRect.x, local.y + drawRect.y, local.width, local.height);
        }

        private static Vector2 DrawingToView(object zoomArea, MethodInfo method, Vector2 value, Rect targetInContent, Rect drawRect, bool isPoint)
        {
            if (zoomArea != null && method != null)
            {
                try
                {
                    return (Vector2)method.Invoke(zoomArea, new object[] { value });
                }
                catch (Exception)
                {
                    // Falls through to the unzoomed approximation.
                }
            }

            // Without the zoom transform the best available answer is the unzoomed one: the target
            // fills the draw rect. Correct at 1x, wrong the moment the view is zoomed or panned.
            float nx = targetInContent.width > 0.0001f ? (value.x - (isPoint ? targetInContent.x : 0f)) / targetInContent.width : 0.5f;
            float ny = targetInContent.height > 0.0001f ? (value.y - (isPoint ? targetInContent.y : 0f)) / targetInContent.height : 0.5f;

            return new Vector2(nx * drawRect.width, ny * drawRect.height);
        }

        private Rect ResolveVisiblePixels(object zoomArea, Rect targetInContent)
        {
            Rect shown = targetInContent;

            if (zoomArea != null && _shownArea != null)
            {
                try
                {
                    Rect value = (Rect)_shownArea.GetValue(zoomArea, null);
                    if (value.width > 0.01f && value.height > 0.01f) shown = value;
                }
                catch (Exception)
                {
                    // Falls through to the whole target being visible.
                }
            }

            float xMin = ToGamePixel(shown.xMin, targetInContent.x, targetInContent.width, GameSize.x);
            float xMax = ToGamePixel(shown.xMax, targetInContent.x, targetInContent.width, GameSize.x);
            float yMin = ToGamePixel(shown.yMin, targetInContent.y, targetInContent.height, GameSize.y);
            float yMax = ToGamePixel(shown.yMax, targetInContent.y, targetInContent.height, GameSize.y);

            if (xMin > xMax) (xMin, xMax) = (xMax, xMin);
            if (yMin > yMax) (yMin, yMax) = (yMax, yMin);

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static float ToGamePixel(float content, float contentMin, float contentSize, float gameSize)
        {
            if (Mathf.Abs(contentSize) < 0.0001f) return 0f;

            return (content - contentMin) / contentSize * gameSize;
        }

        /// <summary>Mirrors GameView.targetInContent: the render target is centred on the origin in zoom space.</summary>
        private static Rect TargetInContent(Vector2 gameSize)
        {
            return EditorGUIUtility.PixelsToPoints(new Rect(-0.5f * gameSize, gameSize));
        }

        private static object GetZoomArea(EditorWindow gameView)
        {
            if (_zoomAreaField == null || !gameView) return null;

            try
            {
                return _zoomAreaField.GetValue(gameView);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ResolveReflection()
        {
            if (_reflectionResolved) return;
            _reflectionResolved = true;

            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;

            const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            _targetSize = gameViewType.GetProperty("targetSize", instance);
            _zoomAreaField = gameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_zoomAreaField == null) return;

            Type zoomAreaType = _zoomAreaField.FieldType;

            _drawRect = zoomAreaType.GetProperty("drawRect", instance);
            _shownArea = zoomAreaType.GetProperty("shownArea", instance) ?? zoomAreaType.GetProperty("shownAreaInsideMargins", instance);
            _drawingToViewPoint = zoomAreaType.GetMethod("DrawingToViewTransformPoint", instance, null, new[] { typeof(Vector2) }, null);
            _drawingToViewVector = zoomAreaType.GetMethod("DrawingToViewTransformVector", instance, null, new[] { typeof(Vector2) }, null);
        }
    }
}
