using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Plays multi-row folds one at a time so collapsing everything, or isolating a folder, slides
    /// shut rather than snapping. The tree can only animate one row at a time — starting a second
    /// fold replaces the one already running — so the folds are queued and released as the tree goes
    /// idle.
    ///
    /// Rows that are only hidden as a side effect of an ancestor closing are collapsed afterwards
    /// without animation, since animating something already out of sight costs a frame each and
    /// shows nothing.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectExpandQueue
    {
        private struct Entry
        {
            public object id;
            public string path;
            public bool expand;
        }

        /// <summary>
        /// How long the queue waits for the window to draw before giving up and applying the rest
        /// instantly. Nothing should ever reach this — it exists so a window that stops drawing
        /// leaves folds applied and the queue empty, rather than pending forever behind a repaint
        /// request that is itself asking the window to draw.
        /// </summary>
        private const double StallSeconds = 0.5;

        private static readonly List<Entry> Animated = new List<Entry>();
        private static readonly List<object> Deferred = new List<object>();

        private static double _lastProgressTime;

        static ProjectExpandQueue()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static bool Pending => Animated.Count > 0 || Deferred.Count > 0;

        private static bool AnimationEnabled =>
            HelpfulEditorSettings.Project.animatedFoldsEnabled && HelpfulEditorTreeReflection.CanAnimateProjectFolds();

        public static void Cancel()
        {
            Animated.Clear();
            Deferred.Clear();
        }

        /// <summary>
        /// Collapses every asset folder, leaving the tree roots and any structural rows open. Nested
        /// folders lose their expanded state too, so re-opening a root does not unfold the whole
        /// tree again — this is "collapse everything", not "hide everything".
        /// </summary>
        public static void CollapseAll()
        {
            if (!AnimationEnabled)
            {
                HelpfulEditorTreeReflection.CollapseAllProjectFolders();
                return;
            }

            Cancel();

            foreach (object id in HelpfulEditorTreeReflection.GetProjectExpandedIds())
            {
                string path = CollapsablePathOf(id);
                if (path == null) continue;

                // A folder directly under a root closes everything beneath it in one fold, so it is
                // the only one worth animating on that branch.
                if (IsTopLevel(path)) Animated.Add(new Entry { id = id, path = path, expand = false });
                else Deferred.Add(id);
            }

            SortByRow(Animated);

            ProjectNavigationAnimator.ScrollTo(0f);
            Begin();
        }

        /// <summary>
        /// Closes everything that is not on the path to the target and opens the target itself.
        /// Folders nested inside what closes keep their expanded state, so reopening one of those
        /// branches later finds it as it was left.
        /// </summary>
        public static void Isolate(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            object targetId = RawIdOf(assetPath);
            if (targetId == null) return;

            Cancel();

            List<Entry> folds = new List<Entry>();
            List<string> closing = new List<string>();

            foreach (object id in HelpfulEditorTreeReflection.GetProjectExpandedIds())
            {
                string path = CollapsablePathOf(id);
                if (path == null) continue;
                if (IsAncestorOrSelf(path, assetPath)) continue;

                closing.Add(path);
                folds.Add(new Entry { id = id, path = path, expand = false });
            }

            // Closing an ancestor already hides what is inside it, so only the outermost fold of
            // each branch is worth playing.
            folds.RemoveAll(fold => HasClosingAncestor(fold.path, closing));

            folds.Add(new Entry { id = targetId, path = assetPath, expand = true });

            if (!AnimationEnabled)
            {
                foreach (Entry fold in folds)
                {
                    HelpfulEditorTreeReflection.SetProjectExpandedImmediate(fold.id, fold.expand);
                }

                EditorApplication.RepaintProjectWindow();
                return;
            }

            SortByRow(folds);
            Animated.AddRange(folds);

            Begin();
        }

        /// <summary>Starts the clock the stall guard measures against, and asks for the first frame.</summary>
        private static void Begin()
        {
            _lastProgressTime = EditorApplication.timeSinceStartup;

            EditorApplication.RepaintProjectWindow();
        }

        /// <summary>
        /// Releases the next fold. Driven from the Project window's own row callback rather than
        /// from the update loop, because starting a fold reaches into the tree's GUI state and the
        /// editor only has that while the window is drawing — called outside it, the tree throws
        /// rather than animating.
        /// </summary>
        public static void Pump()
        {
            if (!Pending) return;

            _lastProgressTime = EditorApplication.timeSinceStartup;

            // Releasing the next fold while one is still playing replaces it mid-slide, which is
            // exactly the snap the queue exists to avoid.
            if (HelpfulEditorTreeReflection.IsProjectTreeAnimating()) return;

            if (Animated.Count > 0)
            {
                Entry next = Animated[0];
                Animated.RemoveAt(0);

                HelpfulEditorTreeReflection.SetProjectExpandedAnimated(next.id, next.expand);
            }
            else
            {
                foreach (object id in Deferred)
                {
                    HelpfulEditorTreeReflection.SetProjectExpandedImmediate(id, false);
                }

                Deferred.Clear();
            }

            EditorApplication.RepaintProjectWindow();
        }

        /// <summary>
        /// Drives the frames the folds play in. The window has no reason of its own to redraw
        /// between them, and the pump only runs while it is drawing, so without this the queue
        /// stops after its first fold.
        /// </summary>
        private static void OnUpdate()
        {
            if (!Pending) return;

            if (EditorApplication.timeSinceStartup - _lastProgressTime > StallSeconds)
            {
                DrainInstantly();
                return;
            }

            EditorApplication.RepaintProjectWindow();
        }

        private static void DrainInstantly()
        {
            foreach (Entry fold in Animated)
            {
                HelpfulEditorTreeReflection.SetProjectExpandedImmediate(fold.id, fold.expand);
            }

            foreach (object id in Deferred)
            {
                HelpfulEditorTreeReflection.SetProjectExpandedImmediate(id, false);
            }

            Cancel();

            EditorApplication.RepaintProjectWindow();
        }

        private static void SortByRow(List<Entry> entries)
        {
            // Top to bottom, so a sweep of folds reads as one motion down the window rather than
            // jumping about in whatever order the expanded set happened to be stored in.
            entries.Sort((left, right) =>
                HelpfulEditorTreeReflection.GetProjectRowIndex(left.id)
                    .CompareTo(HelpfulEditorTreeReflection.GetProjectRowIndex(right.id)));
        }

        /// <summary>
        /// The asset path of an expanded row, or null for rows that must stay open: structural rows
        /// with no asset behind them, and the tree roots themselves.
        /// </summary>
        private static string CollapsablePathOf(object id)
        {
            Object resolved = HelpfulEditorObjectId.Resolve(id);
            if (!resolved) return null;

            string path = AssetDatabase.GetAssetPath(resolved);
            if (string.IsNullOrEmpty(path) || path.IndexOf('/') < 0) return null;

            return path;
        }

        private static object RawIdOf(string assetPath)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            return asset ? HelpfulEditorObjectId.Raw(asset) : null;
        }

        /// <summary>A folder sitting directly under a tree root, such as "Assets/Scripts".</summary>
        private static bool IsTopLevel(string path)
        {
            return path.IndexOf('/') == path.LastIndexOf('/');
        }

        private static bool IsAncestorOrSelf(string candidate, string path)
        {
            return path == candidate || path.StartsWith(candidate + "/", StringComparison.Ordinal);
        }

        private static bool HasClosingAncestor(string path, List<string> closing)
        {
            foreach (string other in closing)
            {
                if (other != path && IsAncestorOrSelf(other, path)) return true;
            }

            return false;
        }
    }
}
