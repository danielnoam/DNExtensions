using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Decides which Project rows to fold for Collapse Everything and Isolate, and hands the sequence
    /// to the shared queue to play. Ancestry here is asset paths, which is the only part of this that
    /// differs from the Hierarchy's equivalent.
    /// </summary>
    internal static class ProjectExpandQueue
    {
        private static readonly HelpfulEditorFoldQueue Queue = new HelpfulEditorFoldQueue(TreeKind.Project);

        private static bool AnimationEnabled => HelpfulEditorSettings.Project.animatedFoldsEnabled && Queue.CanAnimate;

        public static void Pump() => Queue.Pump();

        /// <summary>
        /// Collapses every asset folder, leaving the tree roots and any structural rows open. Nested
        /// folders lose their expanded state too, so re-opening a root does not unfold the whole tree
        /// again — this is "collapse everything", not "hide everything".
        /// </summary>
        public static void CollapseAll()
        {
            List<HelpfulEditorFoldQueue.Fold> folds = new List<HelpfulEditorFoldQueue.Fold>();
            List<object> deferred = new List<object>();

            foreach (object id in HelpfulEditorTreeReflection.GetExpandedIds(TreeKind.Project))
            {
                string path = CollapsablePathOf(id);
                if (path == null) continue;

                // A folder directly under a root closes everything beneath it in one fold, so it is
                // the only one worth animating on that branch.
                if (IsTopLevel(path)) folds.Add(new HelpfulEditorFoldQueue.Fold(id, false));
                else deferred.Add(id);
            }

            if (!AnimationEnabled)
            {
                Queue.ApplyInstantly(folds, deferred);
                return;
            }

            Queue.Begin(folds, deferred);

            ProjectNavigationAnimator.ScrollTo(0f);
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

            List<HelpfulEditorFoldQueue.Fold> folds = new List<HelpfulEditorFoldQueue.Fold>();
            List<string> closing = new List<string>();
            List<string> foldPaths = new List<string>();

            foreach (object id in HelpfulEditorTreeReflection.GetExpandedIds(TreeKind.Project))
            {
                string path = CollapsablePathOf(id);
                if (path == null) continue;
                if (IsAncestorOrSelf(path, assetPath)) continue;

                closing.Add(path);
                folds.Add(new HelpfulEditorFoldQueue.Fold(id, false));
                foldPaths.Add(path);
            }

            // Closing an ancestor already hides what is inside it, so only the outermost fold of each
            // branch is worth playing.
            for (int i = folds.Count - 1; i >= 0; i--)
            {
                if (!HasClosingAncestor(foldPaths[i], closing)) continue;

                folds.RemoveAt(i);
                foldPaths.RemoveAt(i);
            }

            folds.Add(new HelpfulEditorFoldQueue.Fold(targetId, true));

            if (!AnimationEnabled)
            {
                Queue.ApplyInstantly(folds);
                return;
            }

            Queue.Begin(folds);
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
