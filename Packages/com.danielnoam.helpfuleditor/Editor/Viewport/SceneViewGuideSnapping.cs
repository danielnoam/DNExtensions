using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// Pulls a rect being dragged onto a nearby guide, the way an image editor pulls a marquee onto one.
    ///
    /// The correction runs after Unity's own tools have already moved the rect rather than replacing
    /// the move handle with one of our own: a custom tool would take the Rect tool's corner handles
    /// away with it, and those are most of the reason anyone lays UI out in the Scene View at all. The
    /// threshold is measured in screen points, so the pull feels the same however far the view is
    /// zoomed in.
    ///
    /// A snap holds on until it has been pulled well clear of where it would have been made — see
    /// <see cref="ReleaseMultiplier"/>. Capturing and releasing at the same distance sounds right and
    /// is horrible to use: at exactly that distance the smallest wobble of the hand flicks the snap on
    /// and off, which is precisely where the hand is while sliding a rect along a guide.
    /// </summary>
    internal static class SceneViewGuideSnapping
    {
        private const string UndoLabel = "Snap To Guide";
        private const float MovementEpsilon = 1e-10f;
        private const float ResizeEpsilon = 0.0001f;
        private const float ReleaseMultiplier = 2.25f;
        private const int EdgeCount = 3;

        private static readonly List<SnapTarget> Targets = new List<SnapTarget>();
        private static readonly HashSet<int> SnappedGuides = new HashSet<int>();
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        private static bool _dragging;

        /// <summary>
        /// One rect under the drag, and which guide each of its axes is currently held by. The hold has
        /// to outlive the frame — it is the whole difference between a snap that sticks and one that
        /// is re-decided from scratch every time the pointer twitches.
        /// </summary>
        private struct SnapTarget
        {
            public RectTransform rect;
            public Vector2 startSize;
            public Vector3 lastPosition;
            public int guideX;
            public int edgeX;
            public int guideY;
            public int edgeY;
        }

        /// <summary>Whether this guide is currently holding a rect, which is what draws it heavier and recoloured.</summary>
        public static bool IsSnapped(int index) => SnappedGuides.Contains(index);

        public static void Process(SceneViewSettings settings, SceneViewGuideGeometry geometry)
        {
            if (!settings.guideSnapEnabled) return;

            Event evt = Event.current;
            if (evt == null) return;

            switch (evt.type)
            {
                case EventType.MouseDown when evt.button == 0:
                    BeginDrag();
                    break;

                case EventType.MouseDrag when evt.button == 0 && _dragging:
                    Apply(settings, geometry);
                    break;

                case EventType.MouseUp:
                    EndDrag();
                    break;
            }
        }

        private static void BeginDrag()
        {
            Targets.Clear();
            SnappedGuides.Clear();

            foreach (Transform transform in Selection.transforms)
            {
                if (!(transform is RectTransform rectTransform)) continue;

                Targets.Add(new SnapTarget
                {
                    rect = rectTransform,
                    startSize = rectTransform.rect.size,
                    lastPosition = rectTransform.position,
                    guideX = -1,
                    guideY = -1
                });
            }

            _dragging = Targets.Count > 0;
        }

        private static void EndDrag()
        {
            _dragging = false;

            Targets.Clear();
            SnappedGuides.Clear();
        }

        private static void Apply(SceneViewSettings settings, SceneViewGuideGeometry geometry)
        {
            if (settings.guides.Count == 0 || !geometry.HasTarget || !geometry.IsAxisAligned) return;
            if (Tools.current != Tool.Move && Tools.current != Tool.Rect && Tools.current != Tool.Transform) return;

            RectTransform canvasRect = geometry.CanvasRect;
            float capture = Mathf.Max(1f, settings.guideSnapDistance);
            float release = capture * ReleaseMultiplier;

            SnappedGuides.Clear();

            for (int i = 0; i < Targets.Count; i++)
            {
                SnapTarget target = Targets[i];
                if (!target.rect) continue;

                // A rect whose size has changed is being resized rather than moved, and pulling its
                // position about mid-resize would fight the handle the user has hold of.
                if ((target.rect.rect.size - target.startSize).sqrMagnitude > ResizeEpsilon)
                {
                    target.guideX = -1;
                    target.guideY = -1;
                    Targets[i] = target;
                    continue;
                }

                // Nothing moved it this frame, so there is nothing to correct. This is what keeps a
                // camera pan, a guide drag or a rotation from quietly pulling the selection onto a
                // guide behind the user's back — every one of those also arrives as a MouseDrag.
                if ((target.rect.position - target.lastPosition).sqrMagnitude >= MovementEpsilon)
                {
                    // The position read here is the one Unity's tool just set from its own cached drag
                    // origin, so it is the uncorrected position — which is what the hold has to be
                    // measured against. Measuring from last frame's corrected one would have the rect
                    // hold onto a guide it had long since been dragged away from.
                    Bounds local = LocalBounds(canvasRect, target.rect);

                    float offsetX = ResolveAxis(settings, geometry, local, false, ref target.guideX, ref target.edgeX, capture, release);
                    float offsetY = ResolveAxis(settings, geometry, local, true, ref target.guideY, ref target.edgeY, capture, release);

                    if (offsetX != 0f || offsetY != 0f)
                    {
                        Undo.RecordObject(target.rect, UndoLabel);
                        target.rect.position += canvasRect.TransformVector(new Vector3(offsetX, offsetY, 0f));
                    }

                    target.lastPosition = target.rect.position;
                }

                // Reported whether or not anything moved this frame, so the highlight stays lit through
                // the frames where the pointer happened to sit still.
                if (target.guideX >= 0) SnappedGuides.Add(target.guideX);
                if (target.guideY >= 0) SnappedGuides.Add(target.guideY);

                Targets[i] = target;
            }
        }

        /// <summary>
        /// One axis' correction. Whatever is already held wins until it is dragged past the release
        /// distance; only once nothing is held does every guide and every edge compete again, at the
        /// tighter capture distance.
        /// </summary>
        private static float ResolveAxis(SceneViewSettings settings, SceneViewGuideGeometry geometry, Bounds local,
            bool horizontal, ref int capturedGuide, ref int capturedEdge, float capture, float release)
        {
            float scale = Mathf.Abs(geometry.ScreenScale(horizontal));

            if (capturedGuide >= 0 && capturedGuide < settings.guides.Count)
            {
                SceneViewGuide held = settings.guides[capturedGuide];

                if (held.isHorizontal == horizontal)
                {
                    float holdDelta = GuideDelta(geometry, local, held, capturedEdge, horizontal);
                    if (Mathf.Abs(holdDelta) * scale <= release) return holdDelta;
                }
            }

            capturedGuide = -1;
            capturedEdge = 0;

            float best = capture;
            float offset = 0f;

            for (int i = 0; i < settings.guides.Count; i++)
            {
                SceneViewGuide guide = settings.guides[i];
                if (guide.isHorizontal != horizontal) continue;

                for (int edge = 0; edge < EdgeCount; edge++)
                {
                    float delta = GuideDelta(geometry, local, guide, edge, horizontal);
                    float distance = Mathf.Abs(delta) * scale;

                    if (distance >= best) continue;

                    best = distance;
                    offset = delta;
                    capturedGuide = i;
                    capturedEdge = edge;
                }
            }

            return offset;
        }

        private static float GuideDelta(SceneViewGuideGeometry geometry, Bounds local, SceneViewGuide guide, int edge, bool horizontal)
        {
            return geometry.NormalizedToLocal(guide.isHorizontal, guide.normalizedPosition) - EdgeValue(local, edge, horizontal);
        }

        /// <summary>Leading edge, centre, then trailing edge — all three compete for a guide.</summary>
        private static float EdgeValue(Bounds local, int edge, bool horizontal)
        {
            if (horizontal)
            {
                return edge == 0 ? local.min.y : edge == 1 ? local.center.y : local.max.y;
            }

            return edge == 0 ? local.min.x : edge == 1 ? local.center.x : local.max.x;
        }

        /// <summary>
        /// The rect's extent in the canvas' own space. Taken from the world corners rather than from
        /// the rect directly so that a rotated or nested child still measures as the box it visually
        /// occupies, which is the thing being lined up against a guide.
        /// </summary>
        private static Bounds LocalBounds(RectTransform canvasRect, RectTransform target)
        {
            target.GetWorldCorners(CornerBuffer);

            Bounds bounds = new Bounds(canvasRect.InverseTransformPoint(CornerBuffer[0]), Vector3.zero);

            for (int i = 1; i < CornerBuffer.Length; i++)
            {
                bounds.Encapsulate(canvasRect.InverseTransformPoint(CornerBuffer[i]));
            }

            return bounds;
        }
    }
}
