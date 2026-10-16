using System.Collections.Generic;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Expand/collapse for whichever Hierarchy window is open. The legacy window is driven through
    /// reflection into its TreeViewController; the UI Toolkit window that 6.6 made the default has a
    /// public API for the same thing, so the rest of the module only ever talks to this.
    ///
    /// Ids are the same opaque row ids either way, so isolation and the fold queue keep working on
    /// one representation regardless of which window is drawing.
    /// </summary>
    internal static class HierarchyTree
    {
#if UNITY_6000_6_OR_NEWER
        /// <summary>True while a UI Toolkit Hierarchy window is open, which is the one the API below drives.</summary>
        public static bool UsesNewWindow => HierarchyViewTree.Active;
#else
        public static bool UsesNewWindow => false;
#endif

        /// <param name="rawId">A row id, or a boxed Scene for a scene header in the new window.</param>
        public static void ToggleExpanded(object rawId, bool includeChildren)
        {
#if UNITY_6000_6_OR_NEWER
            if (UsesNewWindow)
            {
                HierarchyViewTree.ToggleExpanded(rawId, includeChildren);
                return;
            }
#endif
            HelpfulEditorTreeReflection.ToggleHierarchyExpanded(rawId, includeChildren);
        }

        public static object[] GetExpandedIds()
        {
#if UNITY_6000_6_OR_NEWER
            if (UsesNewWindow) return HierarchyViewTree.GetExpandedIds();
#endif
            return HelpfulEditorTreeReflection.GetHierarchyExpandedIds();
        }

        /// <summary>Replaces the expanded set of GameObject rows. Scene headers are left as they are.</summary>
        public static void SetExpandedIds(IEnumerable<object> ids)
        {
#if UNITY_6000_6_OR_NEWER
            if (UsesNewWindow)
            {
                HierarchyViewTree.SetExpandedIds(ids);
                return;
            }
#endif
            HelpfulEditorTreeReflection.SetHierarchyExpandedIds(ids);
        }

        /// <summary>Collapses every GameObject but leaves the scene headers open.</summary>
        public static void CollapseAll()
        {
#if UNITY_6000_6_OR_NEWER
            if (UsesNewWindow)
            {
                HierarchyViewTree.CollapseAll();
                return;
            }
#endif
            HelpfulEditorTreeReflection.CollapseAllHierarchy();
        }
    }
}
