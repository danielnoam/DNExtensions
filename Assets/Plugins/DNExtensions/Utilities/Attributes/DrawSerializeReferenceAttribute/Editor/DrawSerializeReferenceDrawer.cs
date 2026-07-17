using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(DrawSerializeReferenceAttribute))]
    internal class DrawSerializeReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
            {
                Type elementType = fieldInfo.FieldType.IsArray
                    ? fieldInfo.FieldType.GetElementType()
                    : fieldInfo.FieldType;

                property.managedReferenceValue = Activator.CreateInstance(elementType);
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            string typeName = property.managedReferenceValue?.GetType().Name ?? "Null";
            string index = property.propertyPath.EndsWith("]")
                ? property.propertyPath[property.propertyPath.LastIndexOf('[')..]
                : "";

            EditorGUI.PropertyField(position, property, new GUIContent($"{typeName} {index}"), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }
}
