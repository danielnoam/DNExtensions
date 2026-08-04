using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Single subscription point for Unity's per-row Hierarchy and Project callbacks.
    /// The spec targets the instanceID-based APIs for Unity 2021 LTS compatibility, but Unity 6.4
    /// replaced them with EntityId variants, so the id type is resolved here and modules only ever
    /// receive the already-resolved Object. Order of the overlay passes is owned by each module's
    /// single handler rather than by subscription order.
    /// </summary>
    internal static class HelpfulEditorHooks
    {
        /// <summary>
        /// Raw row id, the object it resolves to, and the row rect. The object is null for rows that
        /// are not objects at all — scene headers being the one that matters — so anything acting on
        /// a row generally needs the id rather than the resolved object.
        /// </summary>
        public static event Action<object, Object, Rect> HierarchyItem;

        public static event Action<string, Rect> ProjectItem;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyItem;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyItem;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItem;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItem;
#endif

            EditorApplication.projectWindowItemOnGUI -= OnProjectItem;
            EditorApplication.projectWindowItemOnGUI += OnProjectItem;
        }

#if UNITY_6000_4_OR_NEWER
        private static void OnHierarchyItem(EntityId entityId, Rect rowRect)
        {
            HierarchyItem?.Invoke(entityId, EditorUtility.EntityIdToObject(entityId), rowRect);
        }
#else
        private static void OnHierarchyItem(int instanceId, Rect rowRect)
        {
            HierarchyItem?.Invoke(instanceId, EditorUtility.InstanceIDToObject(instanceId), rowRect);
        }
#endif

        private static void OnProjectItem(string guid, Rect rowRect)
        {
            ProjectItem?.Invoke(guid, rowRect);
        }
    }
}
