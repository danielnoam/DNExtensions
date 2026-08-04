using System;

namespace DNExtensions.Utilities.SerializableSelector
{
    /// <summary>
    /// Add tooltip to types in SerializableSelector dropdown
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SerializableSelectorTooltipAttribute : Attribute
    {
        public string Tooltip { get; }
        
        public SerializableSelectorTooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}