
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Property attribute that makes fields read-only in the Unity Inspector while keeping them serialized.
    /// Useful for displaying calculated values, debug information, or fields that should not be manually edited.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute 
    {
    }
    
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        
            GUI.enabled = true;
        }
    }

#endif
    
}

