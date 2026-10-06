using System;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Starts a collapsible inspector group holding this field and every field after it,
    /// up to an [EndFoldout] or the next [Foldout]. Use "/" in the name to nest groups, e.g. "Movement/Jump".
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldoutAttribute : Attribute
    {
        public readonly string Name;

        public FoldoutAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Closes every open foldout. Place it on the first field that should be drawn outside the group.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EndFoldoutAttribute : Attribute { }
}
