using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields.Editor
{
    [CustomPropertyDrawer(typeof(OptionalField<>))]
    public class OptionalFieldDrawer : PropertyDrawer
    {
        private const float IconWidth = 20f;
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var isSetProp = property.FindPropertyRelative("isSet");
            var valueProp = property.FindPropertyRelative("value");

            if (isSetProp == null || valueProp == null)
            {
                EditorGUI.LabelField(position, label.text, "OptionalField property not found");
                return;
            }

            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - IconWidth, position.height);
            var toggleRect = new Rect(position.x + EditorGUIUtility.labelWidth - IconWidth, position.y, IconWidth, position.height);
            var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth + Spacing, position.y, 
                position.width - EditorGUIUtility.labelWidth - Spacing, position.height);

            EditorGUI.LabelField(labelRect, label);
            isSetProp.boolValue = EditorGUI.Toggle(toggleRect, isSetProp.boolValue);

            var wasEnabled = GUI.enabled;
            GUI.enabled = isSetProp.boolValue;
            EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
            GUI.enabled = wasEnabled;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            if (valueProp == null) return EditorGUIUtility.singleLineHeight;
            return EditorGUI.GetPropertyHeight(valueProp);
        }
    }
}