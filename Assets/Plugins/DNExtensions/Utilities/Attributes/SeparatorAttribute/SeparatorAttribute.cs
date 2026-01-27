using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Attribute that creates a visual separator line in the Unity Inspector with an optional title.
    /// Use this to visually separate sections of fields in your scripts.
    /// </summary>
    public class SeparatorAttribute : PropertyAttribute
    {
        /// <summary>
        /// The title text to display with the separator. Can be null or empty for just a line.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Whether to add extra spacing above the separator.
        /// Default is true for better visual separation.
        /// </summary>
        public bool AddSpacing { get; set; } = true;

        /// <summary>
        /// The style of the title text. Default is bold.
        /// </summary>
        public FontStyle TitleStyle { get; set; } = FontStyle.Bold;

        /// <summary>
        /// Creates a separator with no title (just a line).
        /// </summary>
        public SeparatorAttribute()
        {
            Title = null;
        }

        /// <summary>
        /// Creates a separator with a title.
        /// </summary>
        /// <param name="title">The title text to display above the separator line</param>
        public SeparatorAttribute(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Creates a separator with a title and spacing option.
        /// </summary>
        /// <param name="title">The title text to display above the separator line</param>
        /// <param name="addSpacing">Whether to add extra spacing above the separator</param>
        public SeparatorAttribute(string title, bool addSpacing)
        {
            Title = title;
            AddSpacing = addSpacing;
        }
    }
}