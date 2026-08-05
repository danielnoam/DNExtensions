using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Decides which Hierarchy rows to fold for Collapse Everything and Isolate, and hands the
    /// sequence to the shared queue to play. Ancestry here is the transform chain, which is the only
    /// part of this that differs from the Project window's equivalent.
    /// </summary>
    internal static class HierarchyExpandQueue
    {
        private static readonly HelpfulEditorFoldQueue Queue = new HelpfulEditorFoldQueue(TreeKind.Hierarchy);

        private static bool AnimationEnabled => HelpfulEditorSettings.Hierarchy.animatedFoldsEnabled && Queue.CanAnimate;

        public static void Pump() => Queue.Pump();

        /// <summary>
        /// Collapses every GameObject, leaving the scene headers open. Nested objects lose their
        /// expanded state too, so reopening a root does not unfold its whole branch again.
        /// </summary>
        public static void CollapseAll()
        {
            if (!AnimationEnabled)
            {
                HelpfulEditorTreeReflection.CollapseAllHierarchy();
                return;
            }

            List<HelpfulEditorFoldQueue.Fold> folds = new List<HelpfulEditorFoldQueue.Fold>();
            List<object> deferred = new List<object>();

            foreach (object id in HelpfulEditorTreeReflection.GetExpandedIds(TreeKind.Hierarchy))
            {
                // Scene headers are rows without an object behind them, and folding those would hide
                // the scene rather than tidy it.
                if (HelpfulEditorObjectId.Resolve(id) is not GameObject gameObject) continue;

                // A root object closes everything beneath it in one fold, so it is the only one
                // worth animating on that branch.
                if (gameObject.transform.parent) deferred.Add(id);
                else folds.Add(new HelpfulEditorFoldQueue.Fold(id, false));
            }

            Queue.Begin(folds, deferred);
        }

        /// <summary>
        /// Plays the move from the current expansion to the given one. Isolation states its result as
        /// a target set rather than a list of folds, because it has to be able to put back whatever
        /// was expanded before.
        /// </summary>
        public static void AnimateTo(IReadOnlyList<object> targetExpandedIds)
        {
            if (!AnimationEnabled)
            {
                HelpfulEditorTreeReflection.SetHierarchyExpandedIds(targetExpandedIds);
                return;
            }

            HashSet<object> target = new HashSet<object>(targetExpandedIds);
            HashSet<object> current = new HashSet<object>(HelpfulEditorTreeReflection.GetExpandedIds(TreeKind.Hierarchy));

            List<HelpfulEditorFoldQueue.Fold> folds = new List<HelpfulEditorFoldQueue.Fold>();
            List<object> deferred = new List<object>();

            foreach (object id in current)
            {
                if (target.Contains(id)) continue;

                // Rows whose parent is closing are hidden by that fold anyway, so they are collapsed
                // silently once the visible ones have played.
                if (HasClosingAncestor(id, current, target)) deferred.Add(id);
                else folds.Add(new HelpfulEditorFoldQueue.Fold(id, false));
            }

            foreach (object id in target)
            {
                if (current.Contains(id)) continue;

                // A row cannot slide open while its parent is still shut, so those are opened up
                // front and only the outermost visible one is played.
                if (AreAllParentsExpanded(id, target)) folds.Add(new HelpfulEditorFoldQueue.Fold(id, true));
                else HelpfulEditorTreeReflection.SetExpandedImmediate(TreeKind.Hierarchy, id, true);
            }

            Queue.Begin(folds, deferred);
        }

        private static bool HasClosingAncestor(object id, HashSet<object> current, HashSet<object> target)
        {
            if (HelpfulEditorObjectId.Resolve(id) is not GameObject gameObject) return false;

            for (Transform parent = gameObject.transform.parent; parent; parent = parent.parent)
            {
                object parentId = HelpfulEditorObjectId.Raw(parent.gameObject);
                if (parentId == null) continue;

                if (current.Contains(parentId) && !target.Contains(parentId)) return true;
            }

            return false;
        }

        private static bool AreAllParentsExpanded(object id, HashSet<object> target)
        {
            if (HelpfulEditorObjectId.Resolve(id) is not GameObject gameObject) return true;

            for (Transform parent = gameObject.transform.parent; parent; parent = parent.parent)
            {
                object parentId = HelpfulEditorObjectId.Raw(parent.gameObject);
                if (parentId != null && !target.Contains(parentId)) return false;
            }

            return true;
        }
    }
}
