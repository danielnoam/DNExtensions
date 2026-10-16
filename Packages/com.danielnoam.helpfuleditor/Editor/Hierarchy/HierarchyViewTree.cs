#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Expand/collapse for the UI Toolkit Hierarchy through its public HierarchyView API, which
    /// replaces the reflection the legacy window needs.
    ///
    /// The view has no "list what is expanded" call, so the expanded set is read by asking every
    /// GameObject with children. That is a walk of the open scenes, which only happens on a keybind
    /// press, never per row or per frame.
    /// </summary>
    internal static class HierarchyViewTree
    {
        private static readonly List<HierarchyNode> ExpandBuffer = new List<HierarchyNode>();
        private static readonly List<HierarchyNode> CollapseBuffer = new List<HierarchyNode>();
        private static readonly List<GameObject> WalkBuffer = new List<GameObject>();
        private static readonly List<GameObject> RootBuffer = new List<GameObject>();

        private static bool _warned;

        public static bool Active => HelpfulEditorWindows.First(typeof(HierarchyWindow));

        /// <param name="rawId">A GameObject's row id, or a boxed Scene for a scene header.</param>
        public static void ToggleExpanded(object rawId, bool includeChildren)
        {
            HierarchyView view = TargetView();
            if (view == null || rawId == null) return;

            try
            {
                if (!TryGetNode(view, rawId, out HierarchyNode node)) return;

                bool expand = !view.IsExpanded(in node);

                if (!includeChildren)
                {
                    if (expand) view.Expand(in node);
                    else view.Collapse(in node);

                    return;
                }

                // Walked by hand rather than through ExpandRecursive so the set is exactly what the
                // legacy window folds: the row itself and every GameObject below it.
                List<HierarchyNode> nodes = expand ? ExpandBuffer : CollapseBuffer;
                nodes.Clear();
                nodes.Add(node);

                CollectBranch(rawId, WalkBuffer);
                AddNodes(view, WalkBuffer, nodes);

                Apply(view, nodes, expand);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        public static object[] GetExpandedIds()
        {
            HierarchyView view = TargetView();
            if (view == null) return Array.Empty<object>();

            List<object> ids = new List<object>();

            try
            {
                HierarchyGameObjectHandler handler = FindHandler<HierarchyGameObjectHandler>(view);
                if (handler == null) return Array.Empty<object>();

                CollectAllParents(WalkBuffer);

                foreach (GameObject gameObject in WalkBuffer)
                {
                    HierarchyNode node = handler.GetOrCreateNode(gameObject);
                    if (view.IsExpanded(in node)) ids.Add(HelpfulEditorObjectId.Raw(gameObject));
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return ids.ToArray();
        }

        /// <summary>Every GameObject with children ends up expanded exactly when it is in the set.</summary>
        public static void SetExpandedIds(IEnumerable<object> ids)
        {
            HierarchyView view = TargetView();
            if (view == null) return;

            try
            {
                HierarchyGameObjectHandler handler = FindHandler<HierarchyGameObjectHandler>(view);
                if (handler == null) return;

                HashSet<GameObject> wanted = new HashSet<GameObject>();
                foreach (object id in ids)
                {
                    if (HelpfulEditorObjectId.Resolve(id) is GameObject gameObject) wanted.Add(gameObject);
                }

                ExpandBuffer.Clear();
                CollapseBuffer.Clear();
                CollectAllParents(WalkBuffer);

                foreach (GameObject gameObject in WalkBuffer)
                {
                    HierarchyNode node = handler.GetOrCreateNode(gameObject);
                    bool isExpanded = view.IsExpanded(in node);
                    bool shouldExpand = wanted.Contains(gameObject);

                    if (shouldExpand && !isExpanded) ExpandBuffer.Add(node);
                    else if (!shouldExpand && isExpanded) CollapseBuffer.Add(node);
                }

                Apply(view, CollapseBuffer, false);
                Apply(view, ExpandBuffer, true);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// Not HierarchyView.CollapseAll: that folds the scene headers too, which hides the scene
        /// rather than tidying it.
        /// </summary>
        public static void CollapseAll() => SetExpandedIds(Array.Empty<object>());

        /// <summary>
        /// The window under the cursor, since expansion is per window and the keybinds act on the row
        /// being hovered. Any open one otherwise, for actions started from somewhere else.
        /// </summary>
        private static HierarchyView TargetView()
        {
            if (EditorWindow.mouseOverWindow is HierarchyWindow hovered && hovered.View != null) return hovered.View;

            return HelpfulEditorWindows.First(typeof(HierarchyWindow)) is HierarchyWindow window ? window.View : null;
        }

        /// <summary>
        /// Handlers are only handed out on a row, so one is borrowed from whichever row of the right
        /// kind is currently built. Every visible GameObject row shares the same handler.
        /// </summary>
        public static T FindHandler<T>(HierarchyView view) where T : HierarchyNodeTypeHandler
        {
            foreach (HierarchyViewItem item in view.Query<HierarchyViewItem>().ToList())
            {
                if (item.Handler is T handler) return handler;
            }

            return null;
        }

        private static bool TryGetNode(HierarchyView view, object rawId, out HierarchyNode node)
        {
            node = HierarchyNode.Null;

            if (rawId is Scene scene)
            {
                HierarchySceneHandler sceneHandler = FindHandler<HierarchySceneHandler>(view);
                if (sceneHandler == null) return false;

                node = sceneHandler.GetOrCreateNode(scene);
                return !node.Equals(HierarchyNode.Null);
            }

            if (HelpfulEditorObjectId.Resolve(rawId) is not GameObject gameObject) return false;

            HierarchyGameObjectHandler handler = FindHandler<HierarchyGameObjectHandler>(view);
            if (handler == null) return false;

            node = handler.GetOrCreateNode(gameObject);
            return !node.Equals(HierarchyNode.Null);
        }

        private static void AddNodes(HierarchyView view, List<GameObject> gameObjects, List<HierarchyNode> nodes)
        {
            HierarchyGameObjectHandler handler = FindHandler<HierarchyGameObjectHandler>(view);
            if (handler == null) return;

            foreach (GameObject gameObject in gameObjects)
            {
                nodes.Add(handler.GetOrCreateNode(gameObject));
            }
        }

        private static void Apply(HierarchyView view, List<HierarchyNode> nodes, bool expand)
        {
            if (nodes.Count == 0) return;

            ReadOnlySpan<HierarchyNode> span = nodes.ToArray();

            if (expand) view.Expand(span);
            else view.Collapse(span);
        }

        /// <summary>Every expandable GameObject below the row, which for a scene header is the whole scene.</summary>
        private static void CollectBranch(object rawId, List<GameObject> result)
        {
            result.Clear();

            if (rawId is Scene scene)
            {
                if (!scene.isLoaded) return;

                scene.GetRootGameObjects(RootBuffer);
                foreach (GameObject root in RootBuffer)
                {
                    CollectParents(root.transform, result);
                }

                return;
            }

            if (HelpfulEditorObjectId.Resolve(rawId) is not GameObject gameObject) return;

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                CollectParents(gameObject.transform.GetChild(i), result);
            }
        }

        /// <summary>Only objects with children can be expanded, so those are the only ones worth asking.</summary>
        private static void CollectAllParents(List<GameObject> result)
        {
            result.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                scene.GetRootGameObjects(RootBuffer);
                foreach (GameObject root in RootBuffer)
                {
                    CollectParents(root.transform, result);
                }
            }
        }

        private static void CollectParents(Transform transform, List<GameObject> result)
        {
            if (transform.childCount == 0) return;

            result.Add(transform.gameObject);

            for (int i = 0; i < transform.childCount; i++)
            {
                CollectParents(transform.GetChild(i), result);
            }
        }

        /// <summary>
        /// The HierarchyView API is new in 6.6 and changed between previews, so a call that throws is
        /// reported once and the keybind does nothing, rather than an exception on every press.
        /// </summary>
        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"HelpfulEditor: a Hierarchy fold failed and was skipped. {e.GetType().Name}: {e.Message}");
        }
    }
}
#endif
