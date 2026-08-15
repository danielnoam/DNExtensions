using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// Resolves the canvas the guides belong to, and maps between the three spaces a guide is
    /// expressed in: a fraction of the canvas rect, a point on the canvas plane, and a point on screen.
    ///
    /// The screen rect is built from the canvas' own world corners rather than from the camera, which
    /// is what makes the face-on test cheap — if the four corners land on an upright rectangle then
    /// the view is square to the canvas, and every mapping below collapses to a linear rect map. That
    /// is also the only condition under which rulers along the window edges mean anything, so one
    /// test gates both the arithmetic and the furniture.
    /// </summary>
    internal class SceneViewGuideGeometry
    {
        /// <summary>How far off square the canvas may sit, as a slope rather than a distance.</summary>
        private const float SkewTolerance = 0.015f;

        private const float MinimumScreenSize = 4f;

        private static readonly Vector3[] CornerBuffer = new Vector3[4];
        private static readonly Vector3[] BoundsBuffer = new Vector3[4];

        private RectTransform _canvasRect;

        /// <summary>The root canvas of whatever was last selected, kept until a different one is picked.</summary>
        public RectTransform CanvasRect => _canvasRect;

        public bool HasTarget => _canvasRect;

        /// <summary>Where the canvas lands in Scene View GUI points. Only dependable while axis aligned.</summary>
        public Rect ScreenRect { get; private set; }

        /// <summary>Whether the view is square to the canvas — front on, unrolled and the right way up.</summary>
        public bool IsAxisAligned { get; private set; }

        /// <summary>The canvas rect's own size, which for a scaled canvas is its reference resolution.</summary>
        public Vector2 ReferenceSize { get; private set; } = Vector2.one;

        public void Update()
        {
            RectTransform resolved = ResolveCanvasRect();

            // Sticky: clicking empty space or a non-UI object leaves the guides where they were rather
            // than making them vanish, which is what deselecting would otherwise do mid-layout.
            if (resolved) _canvasRect = resolved;

            if (!_canvasRect)
            {
                IsAxisAligned = false;
                return;
            }

            Rect local = _canvasRect.rect;
            ReferenceSize = new Vector2(Mathf.Max(1f, local.width), Mathf.Max(1f, local.height));

            _canvasRect.GetWorldCorners(CornerBuffer);

            Vector2 bottomLeft = HandleUtility.WorldToGUIPoint(CornerBuffer[0]);
            Vector2 topLeft = HandleUtility.WorldToGUIPoint(CornerBuffer[1]);
            Vector2 topRight = HandleUtility.WorldToGUIPoint(CornerBuffer[2]);
            Vector2 bottomRight = HandleUtility.WorldToGUIPoint(CornerBuffer[3]);

            ScreenRect = Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
            IsAxisAligned = IsUpright(bottomLeft, topLeft, topRight, bottomRight);
        }

        public float NormalizedToScreen(bool horizontal, float normalized)
        {
            return horizontal
                ? ScreenRect.y + normalized * ScreenRect.height
                : ScreenRect.x + normalized * ScreenRect.width;
        }

        public float ScreenToNormalized(bool horizontal, float screenPosition)
        {
            float size = horizontal ? ScreenRect.height : ScreenRect.width;
            if (Mathf.Abs(size) < 0.0001f) return 0.5f;

            return (screenPosition - (horizontal ? ScreenRect.y : ScreenRect.x)) / size;
        }

        /// <summary>
        /// A horizontal guide's height is measured from the top of the canvas rather than from the
        /// bottom its rect counts from, so a guide at 0 sits where a screen coordinate of 0 would.
        /// </summary>
        public float NormalizedToLocal(bool horizontal, float normalized)
        {
            Rect local = _canvasRect.rect;

            return horizontal
                ? Mathf.Lerp(local.yMax, local.yMin, normalized)
                : Mathf.Lerp(local.xMin, local.xMax, normalized);
        }

        public float LocalToNormalized(bool horizontal, float local)
        {
            Rect rect = LocalRect;

            return horizontal
                ? Mathf.InverseLerp(rect.yMax, rect.yMin, local)
                : Mathf.InverseLerp(rect.xMin, rect.xMax, local);
        }

        /// <summary>The canvas' own rect, which everything on the canvas plane is measured against.</summary>
        public Rect LocalRect => _canvasRect ? _canvasRect.rect : new Rect(0f, 0f, 1f, 1f);

        /// <summary>
        /// The grid line nearest a point on the canvas plane. Counted from the same corner the rulers
        /// count from — the left edge and the top edge — so a line at 100 is where the ruler says 100.
        /// </summary>
        public float NearestGridLine(bool horizontal, float local, float spacing)
        {
            if (spacing <= 0.0001f) return local;

            Rect rect = LocalRect;
            float origin = horizontal ? rect.yMax : rect.xMin;
            float step = horizontal ? -spacing : spacing;

            return origin + Mathf.Round((local - origin) / step) * step;
        }

        /// <summary>
        /// A rect's extent in the canvas' own space. Taken from the world corners rather than from the
        /// rect directly so that a rotated or nested child still measures as the box it visually
        /// occupies, which is the thing being lined up.
        /// </summary>
        public bool TryGetLocalBounds(RectTransform target, out Bounds bounds)
        {
            bounds = default;
            if (!_canvasRect || !target) return false;

            target.GetWorldCorners(BoundsBuffer);

            bounds = new Bounds(_canvasRect.InverseTransformPoint(BoundsBuffer[0]), Vector3.zero);

            for (int i = 1; i < BoundsBuffer.Length; i++)
            {
                bounds.Encapsulate(_canvasRect.InverseTransformPoint(BoundsBuffer[i]));
            }

            return true;
        }

        public void GetWorldEndpoints(bool horizontal, float normalized, out Vector3 from, out Vector3 to)
        {
            Rect local = _canvasRect.rect;
            float position = NormalizedToLocal(horizontal, normalized);

            Vector2 start = horizontal ? new Vector2(local.xMin, position) : new Vector2(position, local.yMin);
            Vector2 end = horizontal ? new Vector2(local.xMax, position) : new Vector2(position, local.yMax);

            from = _canvasRect.TransformPoint(start);
            to = _canvasRect.TransformPoint(end);
        }

        public float NormalizedToReferencePixels(bool horizontal, float normalized)
        {
            return normalized * (horizontal ? ReferenceSize.y : ReferenceSize.x);
        }

        public float ScreenToReferencePixels(bool horizontal, float screenPosition)
        {
            return ScreenToNormalized(horizontal, screenPosition) * (horizontal ? ReferenceSize.y : ReferenceSize.x);
        }

        public float ReferencePixelsToScreen(bool horizontal, float pixels)
        {
            float size = horizontal ? ReferenceSize.y : ReferenceSize.x;

            return NormalizedToScreen(horizontal, pixels / size);
        }

        /// <summary>Screen points per canvas unit, which is what turns a distance on the canvas into a felt one.</summary>
        public float ScreenScale(bool horizontal)
        {
            return horizontal
                ? ScreenRect.height / ReferenceSize.y
                : ScreenRect.width / ReferenceSize.x;
        }

        private static bool IsUpright(Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight)
        {
            Vector2 top = topRight - topLeft;
            Vector2 bottom = bottomRight - bottomLeft;
            Vector2 left = bottomLeft - topLeft;
            Vector2 right = bottomRight - topRight;

            // Seen from behind or upside down every axis runs the wrong way, which would have the
            // rulers counting backwards and a guide dragged left travelling right.
            if (top.x < MinimumScreenSize || bottom.x < MinimumScreenSize) return false;
            if (left.y < MinimumScreenSize || right.y < MinimumScreenSize) return false;

            // Measured as slopes rather than as gaps in pixels. The same small angle off square throws
            // the corners further apart the more the view is zoomed in, so a tolerance counted in
            // pixels quietly tightens as you zoom — and took the rulers away part way into a zoom,
            // which is exactly when the work wants them.
            if (Mathf.Abs(top.y) / top.x > SkewTolerance) return false;
            if (Mathf.Abs(bottom.y) / bottom.x > SkewTolerance) return false;
            if (Mathf.Abs(left.x) / left.y > SkewTolerance) return false;
            if (Mathf.Abs(right.x) / right.y > SkewTolerance) return false;

            // Opposite edges within a hair of the same length. Turning the canvas about its own
            // vertical leaves all four edges still running square on screen but makes the far side
            // shorter, and a ruler numbered evenly across a foreshortened canvas would be lying.
            if (Mathf.Abs(top.x - bottom.x) / top.x > SkewTolerance) return false;

            return Mathf.Abs(left.y - right.y) / left.y <= SkewTolerance;
        }

        /// <summary>
        /// The root canvas rather than the nearest one: a nested canvas is a rendering detail, and
        /// guides placed against one would move the moment its parent laid it out somewhere else.
        /// </summary>
        private static RectTransform ResolveCanvasRect()
        {
            GameObject active = Selection.activeGameObject;
            if (!active) return null;

            Canvas canvas = active.GetComponentInParent<Canvas>();
            if (!canvas) return null;

            Canvas root = canvas.rootCanvas;

            return root ? root.transform as RectTransform : null;
        }
    }
}
