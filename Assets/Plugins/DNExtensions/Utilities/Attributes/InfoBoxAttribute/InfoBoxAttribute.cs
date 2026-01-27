using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Attribute that displays an informational text box in the Unity Inspector.
    /// Use this to provide helpful information, warnings, or instructions to users.
    /// </summary>
    public class InfoBoxAttribute : PropertyAttribute
    {
        /// <summary>
        /// The message text to display in the info box.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The type of info box, which determines the visual style and icon.
        /// </summary>
        public InfoBoxType Type { get; set; } = InfoBoxType.Info;

        /// <summary>
        /// Whether to add extra spacing above the info box.
        /// Default is true for better visual separation.
        /// </summary>
        public bool AddSpacing { get; set; } = true;

        /// <summary>
        /// Creates an info box with the specified message.
        /// </summary>
        /// <param name="message">The text to display in the info box</param>
        public InfoBoxAttribute(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Creates an info box with the specified message and type.
        /// </summary>
        /// <param name="message">The text to display in the info box</param>
        /// <param name="type">The type of info box (Info, Warning, Error)</param>
        public InfoBoxAttribute(string message, InfoBoxType type)
        {
            Message = message;
            Type = type;
        }

        /// <summary>
        /// Creates an info box with the specified message, type, and spacing option.
        /// </summary>
        /// <param name="message">The text to display in the info box</param>
        /// <param name="type">The type of info box (Info, Warning, Error)</param>
        /// <param name="addSpacing">Whether to add extra spacing above the info box</param>
        public InfoBoxAttribute(string message, InfoBoxType type, bool addSpacing)
        {
            Message = message;
            Type = type;
            AddSpacing = addSpacing;
        }
    }

    /// <summary>
    /// Defines the visual style of the info box.
    /// </summary>
    public enum InfoBoxType
    {
        /// <summary>
        /// Neutral informational message with blue/gray styling.
        /// </summary>
        Info,

        /// <summary>
        /// Warning message with yellow/orange styling.
        /// </summary>
        Warning,

        /// <summary>
        /// Error or critical message with red styling.
        /// </summary>
        Error,

        /// <summary>
        /// Success or positive message with green styling.
        /// </summary>
        Success
    }
}