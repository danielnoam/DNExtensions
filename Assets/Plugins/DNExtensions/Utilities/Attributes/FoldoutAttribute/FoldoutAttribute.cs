using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Groups fields under a foldout in the inspector.
    /// Multiple fields with the same group name will be grouped together.
    /// </summary>
    public class FoldoutAttribute : PropertyAttribute
    {
        public readonly string GroupName;
        public readonly bool DefaultExpanded;

        /// <summary>
        /// Creates a foldout group for inspector fields.
        /// </summary>
        /// <param name="groupName">Name of the foldout group</param>
        /// <param name="defaultExpanded">Whether the foldout starts expanded</param>
        public FoldoutAttribute(string groupName, bool defaultExpanded = false)
        {
            GroupName = groupName;
            DefaultExpanded = defaultExpanded;
        }
    }
}