
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Base class for conditional attributes that show/hide or enable/disable fields based on other field values
    /// </summary>
    public abstract class IfAttribute : PropertyAttribute
    {
        private readonly string _variableName;
        private readonly object _variableValue;

#if UNITY_EDITOR
        /// <summary>
        /// Evaluates the condition against the target object
        /// </summary>
        /// <param name="property">The serialized property to evaluate against</param>
        /// <returns>True if the condition is met</returns>
        public bool Evaluate(SerializedProperty property)
        {
            var siblingProperty = FindSiblingProperty(property, _variableName);
    
            if (siblingProperty != null)
            {
                object currentValue = GetSerializedPropertyValue(siblingProperty);
                return Equals(currentValue, _variableValue);
            }
    
            return false;
        }

        private SerializedProperty FindSiblingProperty(SerializedProperty property, string siblingName)
        {
            string path = property.propertyPath;
    
            // Progressively strip path segments until we find the sibling
            while (path.Length > 0)
            {
                int lastDot = path.LastIndexOf('.');
                if (lastDot < 0) break;
        
                string parent = path.Substring(0, lastDot);
                string candidate = parent + "." + siblingName;
        
                var found = property.serializedObject.FindProperty(candidate);
                if (found != null) return found;
        
                path = parent;
            }
    
            // Try root level
            return property.serializedObject.FindProperty(siblingName);
        }
        
        /// <summary>
        /// Gets the value from a SerializedProperty based on its type
        /// </summary>
        /// <param name="property">The serialized property to get the value from</param>
        /// <returns>The value of the property</returns>
        private object GetSerializedPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex;
                default:
                    return null;
            }
        }
#endif

        /// <summary>
        /// Creates a condition that evaluates to true when the boolean field is true
        /// </summary>
        /// <param name="boolName">The name of the boolean field to check</param>
        protected IfAttribute(string boolName) 
        { 
            _variableName = boolName; 
            _variableValue = true; 
        }
        
        /// <summary>
        /// Creates a condition that evaluates to true when the field equals the specified value
        /// </summary>
        /// <param name="variableName">The name of the field to check</param>
        /// <param name="variableValue">The value to compare against</param>
        protected IfAttribute(string variableName, object variableValue) 
        { 
            _variableName = variableName; 
            _variableValue = variableValue; 
        }
    }

    /// <summary>
    /// Hides the field when the condition is met
    /// </summary>
    public class HideIfAttribute : IfAttribute
    {
        /// <summary>
        /// Hides the field when the boolean is true
        /// </summary>
        /// <param name="boolName">The name of the boolean field to check</param>
        public HideIfAttribute(string boolName) : base(boolName) { }
        
        /// <summary>
        /// Hides the field when the variable equals the specified value
        /// </summary>
        /// <param name="variableName">The name of the field to check</param>
        /// <param name="variableValue">The value to compare against</param>
        public HideIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    /// <summary>
    /// Shows the field only when the condition is met
    /// </summary>
    public class ShowIfAttribute : IfAttribute
    {
        /// <summary>
        /// Shows the field when the boolean is true
        /// </summary>
        /// <param name="boolName">The name of the boolean field to check</param>
        public ShowIfAttribute(string boolName) : base(boolName) { }
        
        /// <summary>
        /// Shows the field when the variable equals the specified value
        /// </summary>
        /// <param name="variableName">The name of the field to check</param>
        /// <param name="variableValue">The value to compare against</param>
        public ShowIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    /// <summary>
    /// Enables the field only when the condition is met
    /// </summary>
    public class EnableIfAttribute : IfAttribute
    {
        /// <summary>
        /// Enables the field when the boolean is true
        /// </summary>
        /// <param name="boolName">The name of the boolean field to check</param>
        public EnableIfAttribute(string boolName) : base(boolName) { }
        
        /// <summary>
        /// Enables the field when the variable equals the specified value
        /// </summary>
        /// <param name="variableName">The name of the field to check</param>
        /// <param name="variableValue">The value to compare against</param>
        public EnableIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    /// <summary>
    /// Disables the field when the condition is met
    /// </summary>
    public class DisableIfAttribute : IfAttribute
    {
        /// <summary>
        /// Disables the field when the boolean is true
        /// </summary>
        /// <param name="boolName">The name of the boolean field to check</param>
        public DisableIfAttribute(string boolName) : base(boolName) { }
        
        /// <summary>
        /// Disables the field when the variable equals the specified value
        /// </summary>
        /// <param name="variableName">The name of the field to check</param>
        /// <param name="variableValue">The value to compare against</param>
        public DisableIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }
}