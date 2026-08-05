using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Where each Project tree row sits in the tree: its depth, whether the path down to it passes
    /// through a last child at every level, and whether it draws a foldout arrow. That is what turns
    /// the window's guides from full-height depth bars into proper terminating elbows.
    ///
    /// Unity's per-row callback hands over a guid and a rect and nothing about the tree, so the
    /// answer comes from the tree's own row list. Reading it per row would be quadratic, so it is
    /// walked once and cached until the set of visible rows changes.
    /// </summary>
    internal static class ProjectTreeTopology
    {
        private sealed class RowInfo
        {
            public bool[] lastOnPath;
            public bool hasChildren;
        }

        private static readonly Dictionary<string, RowInfo> Map = new Dictionary<string, RowInfo>();
        private static readonly List<object> ChainBuffer = new List<object>();

        private static int _rowCount = -1;
        private static bool _rebuiltThisPass;

        /// <summary>
        /// Rows arrive top to bottom in a single sweep. The row count is checked here rather than
        /// per row because reaching the tree means finding the window, which is far too expensive to
        /// repeat forty times a repaint — and expanding or collapsing anything changes that count.
        /// A rebuild is allowed once more per sweep, triggered by a row the map has never heard of.
        /// </summary>
        public static void BeginPass()
        {
            _rebuiltThisPass = false;

            IList rows = HelpfulEditorTreeReflection.GetProjectRows();

            // Unreadable rows are treated as unknown for this sweep rather than as a permanent
            // verdict: a window mid-open or mid-close reports nothing, and latching that off would
            // leave the guides in their fallback shape for the rest of the session.
            if (rows == null)
            {
                Map.Clear();
                _rowCount = -1;
                return;
            }

            if (rows.Count != _rowCount) Rebuild(rows);
        }

        public static bool TryGet(string assetPath, out IReadOnlyList<bool> lastOnPath, out bool hasChildren)
        {
            lastOnPath = null;
            hasChildren = false;

            if (string.IsNullOrEmpty(assetPath)) return false;

            if (!Map.TryGetValue(assetPath, out RowInfo info))
            {
                if (_rebuiltThisPass) return false;

                Rebuild(HelpfulEditorTreeReflection.GetProjectRows());
                if (!Map.TryGetValue(assetPath, out info)) return false;
            }

            lastOnPath = info.lastOnPath;
            hasChildren = info.hasChildren;
            return true;
        }

        private static void Rebuild(IList rows)
        {
            _rebuiltThisPass = true;

            Map.Clear();
            _rowCount = -1;

            if (rows == null) return;

            _rowCount = rows.Count;

            foreach (object item in rows)
            {
                if (item == null) continue;

                Object resolved = HelpfulEditorObjectId.Resolve(HelpfulEditorTreeReflection.GetItemId(item));
                if (!resolved) continue;

                string path = AssetDatabase.GetAssetPath(resolved);
                if (string.IsNullOrEmpty(path)) continue;

                Map[path] = Build(item, path);
            }
        }

        /// <summary>
        /// Depth comes from the path rather than from the item, so the tree root does not have to be
        /// located: "Assets/Foo" sits at the top guide level in both layouts, exactly as the
        /// Hierarchy's scene roots do. The parent chain is only consulted for sibling position.
        /// </summary>
        private static RowInfo Build(object item, string path)
        {
            int depth = Mathf.Max(0, SlashCount(path) - 1);

            RowInfo info = new RowInfo
            {
                lastOnPath = depth > 0 ? new bool[depth] : Array.Empty<bool>(),
                hasChildren = HelpfulEditorTreeReflection.GetItemChildren(item)?.Count > 0
            };

            if (depth == 0) return info;

            // Index k holds the node k steps above the row, so the guide at level L is owned by the
            // node at depth - 1 - L.
            ChainBuffer.Clear();
            ChainBuffer.Add(item);

            object cursor = item;
            for (int step = 1; step < depth; step++)
            {
                cursor = HelpfulEditorTreeReflection.GetItemParent(cursor);
                if (cursor == null) break;

                ChainBuffer.Add(cursor);
            }

            for (int level = 0; level < depth; level++)
            {
                int step = depth - 1 - level;

                // A chain that ran short leaves the guide running full height, which is the same
                // thing the old depth-only drawing did.
                info.lastOnPath[level] = step < ChainBuffer.Count && IsLastChild(ChainBuffer[step]);
            }

            return info;
        }

        private static bool IsLastChild(object item)
        {
            object parent = HelpfulEditorTreeReflection.GetItemParent(item);
            if (parent == null) return false;

            IList children = HelpfulEditorTreeReflection.GetItemChildren(parent);
            if (children == null || children.Count == 0) return false;

            return ReferenceEquals(children[children.Count - 1], item);
        }

        private static int SlashCount(string path)
        {
            int count = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/') count++;
            }

            return count;
        }
    }
}
