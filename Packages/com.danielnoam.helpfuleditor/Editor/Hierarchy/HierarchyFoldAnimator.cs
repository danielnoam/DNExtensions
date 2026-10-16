#if UNITY_6000_6_OR_NEWER
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Fold animation for the UI Toolkit Hierarchy, which folds instantly on its own. Nothing here
    /// knows what caused a fold — an arrow, a key, Alt+Click, Collapse Everything, Isolate — it only
    /// sees the expanded flag change, so every route animates alike.
    ///
    /// The view announces the change while its rows still show the old layout, so their positions
    /// are taken then. On the next tick the rows sit in their new places, and each one is pushed
    /// back to where it was and eased home: rows below a fold slide to make or close the gap, and
    /// rows that were not there before unfold out from under the row above them as they fade in.
    ///
    /// Rows are placed by the view through translate, so the easing goes on top instead, leaving
    /// the view's own positioning untouched.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyFoldAnimator
    {
        private const string RowClass = "unity-collection-view__item";
        private const string StickyRowClass = "unity-collection-view__item--sticky";

        /// <summary>How quickly rows settle, as a rate for exponential easing. Around a fifth of a second to rest.</summary>
        private const float SlideRate = 16f;

        private const float SettleDistance = 0.25f;
        private const long TickMilliseconds = 16;

        private static readonly HashSet<HierarchyView> HookedViews = new HashSet<HierarchyView>();
        private static readonly Dictionary<HierarchyView, Dictionary<HierarchyNode, float>> PendingSnapshots = new Dictionary<HierarchyView, Dictionary<HierarchyNode, float>>();
        private static readonly Dictionary<VisualElement, RowMotion> Motions = new Dictionary<VisualElement, RowMotion>();
        private static readonly List<VisualElement> SettledBuffer = new List<VisualElement>();
        private static readonly List<RowPosition> RowBuffer = new List<RowPosition>();

        private static IVisualElementScheduledItem _ticker;
        private static double _lastTickTime;

        private struct RowPosition
        {
            public VisualElement row;
            public HierarchyNode node;
            public float y;
        }

        private class RowMotion
        {
            public float offset;
            public float opacity = 1f;
        }

        static HierarchyFoldAnimator()
        {
            HierarchyWindow.BindView -= OnBindView;
            HierarchyWindow.BindView += OnBindView;

            HierarchyWindow.UnbindView -= OnUnbindView;
            HierarchyWindow.UnbindView += OnUnbindView;

            // Windows already open when scripts reload bound their views before this subscribed.
            EditorApplication.delayCall += HookOpenWindows;
        }

        private static void HookOpenWindows()
        {
            foreach (EditorWindow candidate in HelpfulEditorWindows.AllOfType(typeof(HierarchyWindow)))
            {
                if (candidate is HierarchyWindow window && window.View != null) Hook(window.View);
            }
        }

        private static void OnBindView(HierarchyWindow window, HierarchyView view) => Hook(view);

        private static void OnUnbindView(HierarchyWindow window, HierarchyView view)
        {
            if (view == null) return;

            view.FlagsChanged -= OnFlagsChanged;
            HookedViews.Remove(view);
            PendingSnapshots.Remove(view);
        }

        private static void Hook(HierarchyView view)
        {
            if (!HookedViews.Add(view)) return;

            view.FlagsChanged -= OnFlagsChanged;
            view.FlagsChanged += OnFlagsChanged;
        }

        private static bool Enabled
        {
            get
            {
                HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
                return settings.moduleEnabled && settings.animatedFoldsEnabled;
            }
        }

        /// <summary>
        /// Only the first change of a burst is snapshotted: Collapse Everything flips many flags in a
        /// row, and the layout worth animating from is the one before the first of them.
        /// </summary>
        private static void OnFlagsChanged(HierarchyView view, HierarchyNodeFlags flags)
        {
            if ((flags & HierarchyNodeFlags.Expanded) == 0) return;
            if (!Enabled || view.Filtering || PendingSnapshots.ContainsKey(view)) return;

            Dictionary<HierarchyNode, float> snapshot = new Dictionary<HierarchyNode, float>();

            CollectRows(view);
            foreach (RowPosition position in RowBuffer) snapshot[position.node] = position.y;

            if (snapshot.Count == 0) return;

            PendingSnapshots[view] = snapshot;
            view.schedule.Execute(() => Animate(view));
        }

        /// <summary>
        /// The view's visible rows, top to bottom, where they are drawn: the translate the view set
        /// plus any easing still running on top, so a fold made mid-slide starts from what is on
        /// screen. The sticky scene header is left out: it stays pinned whatever folds beneath it.
        /// </summary>
        private static void CollectRows(HierarchyView view)
        {
            RowBuffer.Clear();

            view.Query<HierarchyViewItem>().ForEach(item =>
            {
                VisualElement row = item.parent;
                while (row != null && !row.ClassListContains(RowClass)) row = row.parent;

                if (row == null || row.resolvedStyle.display == DisplayStyle.None || row.ClassListContains(StickyRowClass)) return;

                RowBuffer.Add(new RowPosition { row = row, node = item.Node, y = row.resolvedStyle.translate.y + CurrentOffset(row) });
            });

            RowBuffer.Sort((left, right) => left.y.CompareTo(right.y));
        }

        /// <summary>
        /// Rows that were visible before ease from their old place. A row that was not is either
        /// revealed by an unfold, and comes out from under the nearest row above that was there, or
        /// scrolled in from below by a fold above it, and moves with the row above it.
        /// </summary>
        private static void Animate(HierarchyView view)
        {
            if (!PendingSnapshots.TryGetValue(view, out Dictionary<HierarchyNode, float> snapshot)) return;

            PendingSnapshots.Remove(view);

            // Measured at rest: an element's running offset belonged to whatever it showed before the
            // view rebound it, so it says nothing about where this node now sits.
            ClearMotions();
            CollectRows(view);

            float lastOffset = 0f;
            float anchorOldY = float.NaN;
            bool anyMoved = false;

            foreach (RowPosition position in RowBuffer)
            {
                float offset;
                float opacity = 1f;

                if (snapshot.TryGetValue(position.node, out float oldY))
                {
                    offset = oldY - position.y;
                    anchorOldY = oldY;
                }
                else if (!float.IsNaN(anchorOldY) && anchorOldY < position.y)
                {
                    offset = anchorOldY - position.y;
                    opacity = 0f;
                }
                else
                {
                    offset = lastOffset;
                }

                lastOffset = offset;

                if (Mathf.Abs(offset) < SettleDistance && opacity >= 1f) continue;

                Motions[position.row] = new RowMotion { offset = offset, opacity = opacity };
                Apply(position.row, Motions[position.row]);
                anyMoved = true;
            }

            if (anyMoved) StartTicking(view);
        }

        private static float CurrentOffset(VisualElement row)
        {
            return Motions.TryGetValue(row, out RowMotion motion) ? motion.offset : 0f;
        }

        private static void ClearMotions()
        {
            foreach (VisualElement row in Motions.Keys) Release(row);

            Motions.Clear();
        }

        /// <summary>
        /// Put back as the view leaves its rows, so a recycled row carries nothing of this into what
        /// it shows next: top at the zero the view sets inline itself, opacity to the stylesheet.
        /// </summary>
        private static void Release(VisualElement row)
        {
            row.style.top = 0f;
            row.style.opacity = StyleKeyword.Null;
        }

        private static void Apply(VisualElement row, RowMotion motion)
        {
            row.style.top = motion.offset;
            row.style.opacity = motion.opacity;
        }

        private static void StartTicking(VisualElement host)
        {
            _lastTickTime = EditorApplication.timeSinceStartup;

            if (_ticker != null && _ticker.isActive) return;

            _ticker = host.schedule.Execute(Tick).Every(TickMilliseconds);
        }

        /// <summary>Eased by elapsed time rather than per tick, so the speed holds however often the scheduler gets to run.</summary>
        private static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Clamp((float)(now - _lastTickTime), 0f, 0.1f);
            _lastTickTime = now;

            float blend = 1f - Mathf.Exp(-SlideRate * deltaTime);

            SettledBuffer.Clear();

            foreach (KeyValuePair<VisualElement, RowMotion> entry in Motions)
            {
                RowMotion motion = entry.Value;

                motion.offset = Mathf.Lerp(motion.offset, 0f, blend);
                motion.opacity = Mathf.Lerp(motion.opacity, 1f, blend);

                if (Mathf.Abs(motion.offset) < SettleDistance && motion.opacity > 0.99f)
                {
                    SettledBuffer.Add(entry.Key);
                    continue;
                }

                Apply(entry.Key, motion);
            }

            foreach (VisualElement row in SettledBuffer)
            {
                Release(row);
                Motions.Remove(row);
            }

            if (Motions.Count > 0) return;

            _ticker?.Pause();
            _ticker = null;
        }
    }
}
#endif
