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
        private const float ResizeTolerance = 0.01f;

        /// <summary>Size change tolerated before a move reads as a resize, as a fraction of the rect.</summary>
        private const float RelativeResizeTolerance = 0.001f;

        private const float RotationEpsilon = 0.01f;
        private const float ScaleEpsilon = 0.000001f;
        private const float ReleaseMultiplier = 2.25f;

        /// <summary>
        /// Extra release distance earned by travelling along the guide rather than away from it. A rect
        /// being slid down a guide is being deliberately kept on it, so it takes a real pull sideways
        /// to shake off; one dragged straight at the guide and past it is trying to leave.
        /// </summary>
        private const float SlideReleaseBonus = 2.5f;

        private const float SlideSmoothing = 0.25f;
        private const float SlideFloor = 0.0001f;

        /// <summary>
        /// Smallest correction worth writing, in screen points. A rect sitting on a guide never
        /// measures as exactly on it — the distance goes out through the canvas transform and comes
        /// back rounded, so it reads as a fraction of nothing rather than zero. Writing that back every
        /// frame nudged the rect by sub-pixel amounts and dirtied the canvas on each one, for the whole
        /// time a snap was held. That was the judder.
        /// </summary>
        private const float MinCorrection = 0.05f;
        private const int EdgeCount = 3;

        private const int HoldNone = -1;
        private const int HoldGrid = -2;

        private static readonly List<SnapTarget> Targets = new List<SnapTarget>();
        private static readonly HashSet<int> SnappedGuides = new HashSet<int>();

        private static bool _dragging;
        private static bool _suspended;

        /// <summary>
        /// One rect under the drag, and what each of its axes is currently held by. The hold has to
        /// outlive the frame — it is the whole difference between a snap that sticks and one that is
        /// re-decided from scratch every time the pointer twitches.
        ///
        /// The hold is kept as the coordinate itself rather than only as an index, so a grid line —
        /// which has no index to keep — holds on exactly as firmly as a guide does.
        /// </summary>
        private struct SnapTarget
        {
            public RectTransform rect;
            public Vector2 startSize;
            public Quaternion startRotation;
            public Vector3 startScale;
            /// <summary>
            /// Where Unity's tool last put it, before our correction. Comparing tool output against
            /// tool output is the only way the "did anything move this frame" test stays honest: with
            /// the corrected position stored instead, a mouse move that happened to cancel the last
            /// correction read as no movement at all, and the frame went unsnapped.
            /// </summary>
            public Vector3 lastRawPosition;

            /// <summary>
            /// Where we left it after correcting. Layout runs every frame but the handle only writes
            /// on a mouse move, so on the still frames the position still reads as ours — and treating
            /// that as fresh input fed the correction back into itself, which is what made the drag
            /// judder. Matching this means nothing external has happened and there is nothing to do.
            /// </summary>
            public Vector3 lastAppliedPosition;

            public float slideX;
            public float slideY;
            public bool recorded;
            public int sourceX;
            public int edgeX;
            public float holdX;
            public int sourceY;
            public int edgeY;
            public float holdY;
        }

        /// <summary>Whether this guide is currently holding a rect, which is what draws it heavier and recoloured.</summary>
        public static bool IsSnapped(int index) => SnappedGuides.Contains(index);

        /// <summary>
        /// Hidden guides do not snap. Something being pulled onto a line that is not on screen reads
        /// as the rect refusing to go where it was put.
        /// </summary>
        private static bool GuidesSnap(SceneViewSettings settings)
        {
            return settings.guideSnapEnabled && settings.showRulers && settings.guides.Count > 0;
        }

        private static bool GridSnaps(SceneViewSettings settings)
        {
            return settings.gridSnapEnabled && settings.gridEnabled;
        }

        public static void Process(SceneViewSettings settings, SceneViewGuideGeometry geometry)
        {
            if (!GuidesSnap(settings) && !GridSnaps(settings)) return;

            Event evt = Event.current;
            if (evt == null) return;

            // Modifiers are taken from whatever input event arrives, because a Layout event does not
            // carry a dependable modifier state of its own.
            if (evt.type != EventType.Layout && evt.type != EventType.Repaint)
            {
                _suspended = evt.control || evt.command;
            }

            // Only the passes where the handle itself just acted.
            //
            // MouseDrag never arrives as itself — the handle calls Use() on it, so it reaches here as
            // EventType.Used, in the same OnGUI pass, immediately after the position was written. That
            // pass is the only race-free moment there is: no repaint can slip between the handle's
            // write and this correction. Correcting on Layout as well, which an earlier attempt did,
            // adds writes in passes a repaint *can* fall between, and every one of those is a frame
            // that gets drawn from the uncorrected position.
            //
            // MouseDrag and MouseUp are kept as well for the paths where nothing consumed them first.
            switch (evt.type)
            {
                case EventType.Used:
                case EventType.MouseDown:
                case EventType.MouseDrag:
                case EventType.MouseUp:
                    break;

                default:
                    return;
            }

            bool handleActive = GUIUtility.hotControl != 0;

            if (handleActive)
            {
                if (!_dragging) BeginDrag();
                if (_dragging) Apply(settings, geometry);

                return;
            }

            if (!_dragging) return;

            // Corrected once more on the way out, so the position left behind is the snapped one.
            Apply(settings, geometry);
            EndDrag();
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
                    startRotation = rectTransform.rotation,
                    startScale = rectTransform.lossyScale,
                    lastRawPosition = rectTransform.position,
                    lastAppliedPosition = rectTransform.position,
                    sourceX = HoldNone,
                    sourceY = HoldNone
                });
            }

            _dragging = Targets.Count > 0;
        }

        /// <summary>
        /// How much of this frame's travel ran along each held axis rather than across it, smoothed so
        /// one twitchy frame cannot decide it. A vertical guide is slid along by moving in Y, and a
        /// horizontal one by moving in X, so each axis watches the other for its slide.
        /// </summary>
        private static void TrackSlide(RectTransform canvasRect, Vector3 worldDelta, ref SnapTarget target)
        {
            Vector3 localDelta = canvasRect.InverseTransformVector(worldDelta);

            float acrossX = Mathf.Abs(localDelta.x);
            float acrossY = Mathf.Abs(localDelta.y);
            float total = acrossX + acrossY;

            if (total < SlideFloor) return;

            target.slideX = Mathf.Lerp(target.slideX, acrossY / total, SlideSmoothing);
            target.slideY = Mathf.Lerp(target.slideY, acrossX / total, SlideSmoothing);
        }

        /// <summary>Whether the rect is being reshaped rather than simply carried somewhere else.</summary>
        private static bool IsReshaping(SnapTarget target)
        {
            // Relative, not absolute. A rect the size of a canvas carries enough float noise in its
            // size to trip a fixed hair's-breadth tolerance, and reading that as a resize switches
            // snapping off for the rest of the drag — which looks exactly like the snap not working.
            float allowed = Mathf.Max(ResizeTolerance, target.startSize.magnitude * RelativeResizeTolerance);

            if ((target.rect.rect.size - target.startSize).sqrMagnitude > allowed * allowed) return true;
            if (Quaternion.Angle(target.rect.rotation, target.startRotation) > RotationEpsilon) return true;

            return (target.rect.lossyScale - target.startScale).sqrMagnitude > ScaleEpsilon;
        }

        /// <summary>Lets go of every hold without moving anything, so nothing is re-grabbed from stale state.</summary>
        private static void ReleaseHolds()
        {
            SnappedGuides.Clear();

            for (int i = 0; i < Targets.Count; i++)
            {
                SnapTarget target = Targets[i];

                target.sourceX = HoldNone;
                target.sourceY = HoldNone;

                if (target.rect)
                {
                    target.lastRawPosition = target.rect.position;
                    target.lastAppliedPosition = target.rect.position;
                }

                Targets[i] = target;
            }
        }

        private static void EndDrag()
        {
            _dragging = false;

            Targets.Clear();
            SnappedGuides.Clear();
        }

        private static void Apply(SceneViewSettings settings, SceneViewGuideGeometry geometry)
        {
            if (!geometry.HasTarget || !geometry.IsAxisAligned) return;
            if (Tools.current != Tool.Move && Tools.current != Tool.Rect && Tools.current != Tool.Transform) return;

            bool guideSnaps = GuidesSnap(settings);
            bool gridSnaps = GridSnaps(settings);
            if (!guideSnaps && !gridSnaps) return;

            // Ctrl — Cmd on a Mac — is how Unity's own grid snapping is asked for mid-drag. Two
            // snapping systems pulling at one rect do not take turns, they fight, and the rect ends up
            // going wherever the last one to run decided. Ours stands down and lets the built-in have
            // it, which doubles as the suspend modifier this was missing.
            if (_suspended)
            {
                ReleaseHolds();
                return;
            }

            RectTransform canvasRect = geometry.CanvasRect;
            float capture = Mathf.Max(1f, settings.guideSnapDistance);

            SnappedGuides.Clear();

            for (int i = 0; i < Targets.Count; i++)
            {
                SnapTarget target = Targets[i];
                if (!target.rect) continue;

                // Only a rect being moved gets corrected. Resizing was already excluded by its size
                // changing, but rotating and scaling both move a rect's corners about without touching
                // its size at all — under the Transform tool, and under the Rect tool's own rotation
                // corners, that had the thing being turned yanked onto a guide as it went.
                if (IsReshaping(target))
                {
                    target.sourceX = HoldNone;
                    target.sourceY = HoldNone;
                    Targets[i] = target;
                    continue;
                }

                Vector3 raw = target.rect.position;

                // Still sitting exactly where we left it, so the handle has not written anything since
                // and there is no new input to react to. Re-running the correction on our own output
                // is what made the drag judder, and it poisoned the slide tracking with a delta that
                // was really just last frame's correction read back.
                if ((raw - target.lastAppliedPosition).sqrMagnitude < MovementEpsilon)
                {
                    if (target.sourceX >= 0) SnappedGuides.Add(target.sourceX);
                    if (target.sourceY >= 0) SnappedGuides.Add(target.sourceY);

                    Targets[i] = target;
                    continue;
                }

                Vector3 worldDelta = raw - target.lastRawPosition;

                // This keeps a camera pan or a rotation from quietly pulling the selection onto a guide
                // behind the user's back — those move the rect without the handle having done it.
                if (worldDelta.sqrMagnitude >= MovementEpsilon)
                {
                    TrackSlide(canvasRect, worldDelta, ref target);

                    // The position read here is the one Unity's handle just wrote from its own cached
                    // drag origin, so it is the uncorrected position — which is what the hold has to be
                    // measured against. Measuring from last frame's corrected one would have the rect
                    // hold onto a guide it had long since been dragged away from.
                    if (!geometry.TryGetLocalBounds(target.rect, out Bounds local)) continue;

                    float releaseX = capture * (ReleaseMultiplier + SlideReleaseBonus * target.slideX);
                    float releaseY = capture * (ReleaseMultiplier + SlideReleaseBonus * target.slideY);

                    float offsetX = ResolveAxis(settings, geometry, local, false, guideSnaps, gridSnaps, ref target.sourceX, ref target.edgeX, ref target.holdX, capture, releaseX);
                    float offsetY = ResolveAxis(settings, geometry, local, true, guideSnaps, gridSnaps, ref target.sourceY, ref target.edgeY, ref target.holdY, capture, releaseY);

                    // Anything under half a tenth of a screen point is rounding, not a correction, and
                    // is dropped rather than written. Holding still has to mean writing nothing at all,
                    // otherwise every held frame costs a transform write and a canvas rebuild.
                    if (Mathf.Abs(offsetX) * Mathf.Abs(geometry.ScreenScale(false)) < MinCorrection) offsetX = 0f;
                    if (Mathf.Abs(offsetY) * Mathf.Abs(geometry.ScreenScale(true)) < MinCorrection) offsetY = 0f;

                    if (offsetX != 0f || offsetY != 0f)
                    {
                        // Once per drag, not once per frame. Recording every frame buried the drag
                        // under an undo entry per mouse move, so backing out of a move meant holding
                        // Ctrl+Z down rather than pressing it.
                        if (!target.recorded)
                        {
                            Undo.RecordObject(target.rect, UndoLabel);
                            target.recorded = true;
                        }

                        target.rect.position += canvasRect.TransformVector(new Vector3(offsetX, offsetY, 0f));
                    }

                    // Raw recorded before the correction, applied recorded after it — the two together
                    // are what tell a fresh write from the handle apart from our own last answer.
                    target.lastRawPosition = raw;
                    target.lastAppliedPosition = target.rect.position;
                }

                // Reported whether or not anything moved this frame, so the highlight stays lit through
                // the frames where the pointer happened to sit still. Grid lines have no index and are
                // not highlighted — there is nothing there for a heavier line to pick out.
                if (target.sourceX >= 0) SnappedGuides.Add(target.sourceX);
                if (target.sourceY >= 0) SnappedGuides.Add(target.sourceY);

                Targets[i] = target;
            }
        }

        /// <summary>
        /// One axis' correction. Whatever is already held wins until it is dragged past the release
        /// distance; only once nothing is held does every guide and every edge compete again, at the
        /// tighter capture distance.
        /// </summary>
        private static float ResolveAxis(SceneViewSettings settings, SceneViewGuideGeometry geometry, Bounds local,
            bool horizontal, bool guideSnaps, bool gridSnaps, ref int source, ref int edge, ref float hold, float capture, float release)
        {
            float scale = Mathf.Abs(geometry.ScreenScale(horizontal));

            if (source != HoldNone)
            {
                float holdDelta = hold - EdgeValue(local, edge, horizontal);
                if (Mathf.Abs(holdDelta) * scale <= release) return holdDelta;
            }

            source = HoldNone;
            edge = 0;

            float best = capture;
            float offset = 0f;

            for (int i = 0; guideSnaps && i < settings.guides.Count; i++)
            {
                SceneViewGuide guide = settings.guides[i];
                if (guide.isHorizontal != horizontal) continue;

                float guideLocal = geometry.NormalizedToLocal(guide.isHorizontal, guide.normalizedPosition);

                for (int candidate = 0; candidate < EdgeCount; candidate++)
                {
                    float delta = guideLocal - EdgeValue(local, candidate, horizontal);
                    float distance = Mathf.Abs(delta) * scale;

                    if (distance >= best) continue;

                    best = distance;
                    offset = delta;
                    source = i;
                    edge = candidate;
                    hold = guideLocal;
                }
            }

            // Guides win outright rather than competing on distance: they were put somewhere on
            // purpose, and being pulled off one by a grid line that happened to be a pixel nearer is
            // the sort of thing that makes a tool feel like it is arguing.
            if (source != HoldNone || !gridSnaps) return offset;

            float spacing = GridSpacing(settings);

            for (int candidate = 0; candidate < EdgeCount; candidate++)
            {
                float value = EdgeValue(local, candidate, horizontal);
                float line = geometry.NearestGridLine(horizontal, value, spacing);
                float delta = line - value;
                float distance = Mathf.Abs(delta) * scale;

                if (distance >= best) continue;

                best = distance;
                offset = delta;
                source = HoldGrid;
                edge = candidate;
                hold = line;
            }

            return offset;
        }

        /// <summary>The finest division on show, so what a rect lands on is what can be seen to line up.</summary>
        public static float GridSpacing(SceneViewSettings settings)
        {
            float cell = Mathf.Max(1f, settings.gridCellSize);
            int subdivisions = Mathf.Clamp(settings.gridSubdivisions, 0, 16);

            return cell / (subdivisions + 1);
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
    }
}
