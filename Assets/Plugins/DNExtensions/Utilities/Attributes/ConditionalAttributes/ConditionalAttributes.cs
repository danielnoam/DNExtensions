using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace DNExtensions.Utilities
{
    public abstract class IfAttribute : PropertyAttribute
    {
        private readonly string _variableName;
        private readonly object _variableValue;

#if UNITY_EDITOR
        public bool Evaluate(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetType = targetObject.GetType();
    
            var propertyInfo = targetType.GetProperty(_variableName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
            if (propertyInfo != null)
            {
                object currentValue = propertyInfo.GetValue(targetObject);
                return Equals(currentValue, _variableValue);
            }
    
            var methodInfo = targetType.GetMethod(_variableName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, System.Type.EmptyTypes, null);
    
            if (methodInfo != null && methodInfo.ReturnType == typeof(bool))
            {
                object result = methodInfo.Invoke(targetObject, null);
                return (bool)result;
            }
    
            var siblingProperty = FindSiblingProperty(property, _variableName);
            if (siblingProperty != null)
            {
                object currentValue = GetSerializedPropertyValue(siblingProperty);
                
                if (siblingProperty.propertyType == SerializedPropertyType.Enum && _variableValue is System.Enum)
                {
                    int enumIndex = System.Convert.ToInt32(_variableValue);
                    return Equals(currentValue, enumIndex);
                }
        
                return Equals(currentValue, _variableValue);
            }
    
            return false;
        }

        private SerializedProperty FindSiblingProperty(SerializedProperty property, string siblingName)
        {
            string path = property.propertyPath;
            
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
            
            return property.serializedObject.FindProperty(siblingName);
        }
        
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

        protected IfAttribute(string boolName) 
        { 
            _variableName = boolName; 
            _variableValue = true; 
        }
        
        protected IfAttribute(string variableName, object variableValue) 
        { 
            _variableName = variableName; 
            _variableValue = variableValue; 
        }
    }

    public class HideIfAttribute : IfAttribute
    {
        public HideIfAttribute(string boolName) : base(boolName) { }
        public HideIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    public class ShowIfAttribute : IfAttribute
    {
        public ShowIfAttribute(string boolName) : base(boolName) { }
        public ShowIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    public class EnableIfAttribute : IfAttribute
    {
        public EnableIfAttribute(string boolName) : base(boolName) { }
        public EnableIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    public class DisableIfAttribute : IfAttribute
    {
        public DisableIfAttribute(string boolName) : base(boolName) { }
        public DisableIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }
}