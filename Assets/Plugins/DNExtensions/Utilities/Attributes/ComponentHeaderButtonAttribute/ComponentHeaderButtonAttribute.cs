using System;

namespace DNExtensions.Utilities 
{
    
    /// <summary>
    /// Adds a button to the component header in the inspector.
    /// Note: Only works with default Unity inspectors. Components with CustomEditor won't show injected buttons.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ComponentHeaderButtonAttribute : Attribute {
        
        public string Icon { get; }
        public string Tooltip { get; }
        public int Priority { get; }
        
        /// <param name="icon">Unicode icon character</param>
        /// <param name="tooltip">Tooltip text shown on hover</param>
        /// <param name="priority">Lower numbers appear first (left to right)</param>
        public ComponentHeaderButtonAttribute(string icon, string tooltip = "", int priority = 0) {
            Icon = icon;
            Tooltip = tooltip;
            Priority = priority;
        }
    }
}