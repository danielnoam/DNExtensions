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
            var hideValueProp = property.FindPropertyRelative("hideValueIfSet");

            if (isSetProp == null || valueProp == null)
            {
                EditorGUI.LabelField(position, label.text, "OptionalField property not found");
                return;
            }
            
            HandleContextMenu(position, property, hideValueProp);

            EditorGUI.BeginProperty(position, label, property);

            var labelRect = EditorGUI.PrefixLabel(position, label);
            var toggleRect = new Rect(labelRect.x, labelRect.y, ToggleWidth, labelRect.height);
            var valueRect = new Rect(labelRect.x + ToggleWidth + Spacing, labelRect.y, labelRect.width - ToggleWidth - Spacing, labelRect.height);
            
            EditorGUI.BeginChangeCheck();
            bool newIsSet = EditorGUI.Toggle(toggleRect, isSetProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isSetProp.boolValue = newIsSet;
            }
            
            var shouldHide = hideValueProp.boolValue && !isSetProp.boolValue;
            
            if (!shouldHide)
            {
                var wasEnabled = GUI.enabled;
                GUI.enabled = isSetProp.boolValue;
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                GUI.enabled = wasEnabled;
            }

            EditorGUI.EndProperty();
        }
        
        private void HandleContextMenu(Rect position, SerializedProperty property, SerializedProperty hideValueProp)
        {
            Event current = Event.current;
            if (current.type == EventType.ContextClick && position.Contains(current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Hide Value If Disabled"), hideValueProp.boolValue, () =>
                {
                    hideValueProp.boolValue = !hideValueProp.boolValue;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.ShowAsContext();
                current.Use();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            if (valueProp == null) return EditorGUIUtility.singleLineHeight;
            return EditorGUI.GetPropertyHeight(valueProp);
        }
    }
}