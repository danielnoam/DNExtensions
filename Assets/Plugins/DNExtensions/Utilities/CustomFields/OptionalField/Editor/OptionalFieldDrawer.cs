using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields.Editor
{
    [CustomPropertyDrawer(typeof(OptionalField<>))]
    public class OptionalFieldDrawer : PropertyDrawer
    {
        private const float ToggleWidth = 16f;
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

            EditorGUI.BeginProperty(position, label, property);

            var labelRect = EditorGUI.PrefixLabel(position, label);
            var toggleRect = new Rect(labelRect.x, labelRect.y, ToggleWidth, labelRect.height);
            var valueRect = new Rect(labelRect.x + ToggleWidth + Spacing, labelRect.y, 
                labelRect.width - ToggleWidth - Spacing, labelRect.height);

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