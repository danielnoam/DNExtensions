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
        private const float AlignmentTolerance = 0.75f;
        private const float MinimumScreenSize = 4f;

        private static readonly Vector3[] CornerBuffer = new Vector3[4];

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
            if (Mathf.Abs(topLeft.x - bottomLeft.x) > AlignmentTolerance) return false;
            if (Mathf.Abs(topRight.x - bottomRight.x) > AlignmentTolerance) return false;
            if (Mathf.Abs(topLeft.y - topRight.y) > AlignmentTolerance) return false;
            if (Mathf.Abs(bottomLeft.y - bottomRight.y) > AlignmentTolerance) return false;

            // Seen from behind or upside down the corners still form an upright rectangle, but every
            // axis runs the wrong way — which would have the rulers counting backwards and a guide
            // dragged left travelling right.
            return topRight.x - topLeft.x > MinimumScreenSize && bottomLeft.y - topLeft.y > MinimumScreenSize;
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
