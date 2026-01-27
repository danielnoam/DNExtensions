using System;

namespace DNExtensions.Utilities.SerializableSelector
{
    /// <summary>
    /// Customize the display name and optional category path for a type in SerializableSelector dropdown
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SerializableSelectorNameAttribute : Attribute
    {
        /// <summary>
        /// The display name shown in the dropdown
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        /// Optional category path (e.g., "Combat/Damage/Direct")
        /// If null, uses the type's namespace
        /// </summary>
        public string CategoryPath { get; }
        
        /// <summary>
        /// Set custom display name only
        /// </summary>
        public SerializableSelectorNameAttribute(string displayName)
        {
            DisplayName = displayName;
            CategoryPath = null;
        }
        
        /// <summary>
        /// Set custom display name and category path
        /// </summary>
        public SerializableSelectorNameAttribute(string displayName, string categoryPath)
        {
            DisplayName = displayName;
            CategoryPath = categoryPath;
        }
    }
}