using System.Collections.Generic;
using UnityEditor;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Plays multi-row folds one at a time so collapsing a tree, or isolating a branch, slides shut
    /// rather than snapping. A tree can only animate one row at a time — starting a second fold
    /// replaces the one already running — so folds are queued and released as it goes idle.
    ///
    /// This owns only the mechanics. Which rows to fold, and in what order they belong, is decided
    /// per window: the Project window reasons about asset paths, the Hierarchy about transforms.
    /// </summary>
    internal sealed class HelpfulEditorFoldQueue
    {
        internal readonly struct Fold
        {
            public readonly object id;
            public readonly bool expand;

            public Fold(object id, bool expand)
            {
                this.id = id;
                this.expand = expand;
            }
        }

        /// <summary>
        /// How long the queue waits for the window to draw before giving up and applying the rest
        /// instantly. Nothing should ever reach this — it exists so a window that stops drawing, or
        /// a tree that will not come unstuck, leaves the folds applied and the queue empty rather
        /// than pending forever behind a repaint request the window is not answering.
        /// </summary>
        private const double StallSeconds = 0.5;

        private readonly TreeKind _kind;
        private readonly List<Fold> _animated = new List<Fold>();
        private readonly List<object> _deferred = new List<object>();

        private double _lastProgressTime;

        public HelpfulEditorFoldQueue(TreeKind kind)
        {
            _kind = kind;

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public bool Pending => _animated.Count > 0 || _deferred.Count > 0;

        public bool CanAnimate => HelpfulEditorTreeReflection.CanAnimateFolds(_kind);

        public void Cancel()
        {
            _animated.Clear();
            _deferred.Clear();
        }

        /// <summary>
        /// Queues a set of folds, plus rows to collapse without animation once the visible ones have
        /// played — animating something already hidden behind a closed ancestor costs a frame each
        /// and shows nothing.
        /// </summary>
        public void Begin(List<Fold> animated, List<object> deferredCollapse = null)
        {
            Cancel();

            if (animated != null)
            {
                // Top to bottom, so a sweep reads as one motion down the window rather than jumping
                // about in whatever order the expanded set happened to be stored in.
                animated.Sort((left, right) =>
                    HelpfulEditorTreeReflection.GetRowIndex(_kind, left.id)
                        .CompareTo(HelpfulEditorTreeReflection.GetRowIndex(_kind, right.id)));

                _animated.AddRange(animated);
            }

            if (deferredCollapse != null) _deferred.AddRange(deferredCollapse);

            _lastProgressTime = EditorApplication.timeSinceStartup;

            HelpfulEditorTreeReflection.RepaintTree(_kind);
        }

        public void ApplyInstantly(IEnumerable<Fold> folds, IEnumerable<object> deferredCollapse = null)
        {
            if (folds != null)
            {
                foreach (Fold fold in folds) HelpfulEditorTreeReflection.SetExpandedImmediate(_kind, fold.id, fold.expand);
            }

            if (deferredCollapse != null)
            {
                foreach (object id in deferredCollapse) HelpfulEditorTreeReflection.SetExpandedImmediate(_kind, id, false);
            }

            HelpfulEditorTreeReflection.RepaintTree(_kind);
        }

        /// <summary>
        /// Releases the next fold. Must be called while the window is drawing: starting a fold
        /// reaches into the tree's GUI state, and the editor only has that during a GUI pass —
        /// called outside one, the tree throws rather than animating.
        /// </summary>
        public void Pump()
        {
            if (!Pending) return;

            // Waiting on a fold that is still playing counts as progress, so the stall guard does
            // not fire part-way through a long sweep.
            if (HelpfulEditorTreeReflection.IsTreeAnimating(_kind))
            {
                _lastProgressTime = EditorApplication.timeSinceStartup;
                return;
            }

            if (_animated.Count > 0)
            {
                Fold next = _animated[0];

                // The row lookup can disagree with the row list while a long tree is collapsing.
                // Folding on that answer closes the wrong row, so the tree is nudged into rebuilding
                // and the entry stays queued. The progress clock is deliberately not touched: if it
                // never comes unstuck, the stall guard applies the rest instantly.
                if (HelpfulEditorTreeReflection.IsRowStale(_kind, next.id))
                {
                    HelpfulEditorTreeReflection.NudgeTree(_kind);
                    return;
                }

                _animated.RemoveAt(0);

                HelpfulEditorTreeReflection.SetExpandedAnimated(_kind, next.id, next.expand);
            }
            else
            {
                foreach (object id in _deferred) HelpfulEditorTreeReflection.SetExpandedImmediate(_kind, id, false);

                _deferred.Clear();
            }

            _lastProgressTime = EditorApplication.timeSinceStartup;

            HelpfulEditorTreeReflection.RepaintTree(_kind);
        }

        /// <summary>
        /// Drives the frames the folds play in. The window has no reason of its own to redraw
        /// between them, and the pump only runs while it is drawing, so without this the queue stops
        /// after its first fold.
        /// </summary>
        private void Tick()
        {
            if (!Pending) return;

            if (EditorApplication.timeSinceStartup - _lastProgressTime > StallSeconds)
            {
                List<Fold> remaining = new List<Fold>(_animated);
                List<object> remainingDeferred = new List<object>(_deferred);

                Cancel();
                ApplyInstantly(remaining, remainingDeferred);
                return;
            }

            HelpfulEditorTreeReflection.RepaintTree(_kind);
        }
    }
}
