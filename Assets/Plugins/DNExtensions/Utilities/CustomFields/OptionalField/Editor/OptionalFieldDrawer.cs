using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    [CustomPropertyDrawer(typeof(OptionalField<>))]
    internal class OptionalFieldDrawer : PropertyDrawer
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
                isSetProp.boolValue = newIsSet;

            bool shouldHide = hideValueProp.boolValue && !isSetProp.boolValue;

            if (!shouldHide)
            {
                var wasEnabled = GUI.enabled;
                GUI.enabled = isSetProp.boolValue;
                DrawValue(valueRect, valueProp, property);
                GUI.enabled = wasEnabled;
            }

            EditorGUI.EndProperty();
        }

        private void DrawValue(Rect rect, SerializedProperty valueProp, SerializedProperty property)
        {
            var resolvedField = ConditionalAttributeEvaluator.GetFieldInfo(property);
            var range = resolvedField?.GetCustomAttribute<RangeAttribute>();

            if (range != null)
            {
                var valueField = ConditionalAttributeEvaluator.GetFieldInfo(valueProp);
                var type = valueField?.FieldType ?? resolvedField?.FieldType?.GenericTypeArguments?[0];

                if (type == typeof(float))
                {
                    valueProp.floatValue = EditorGUI.Slider(rect, valueProp.floatValue, range.min, range.max);
                    return;
                }
                if (type == typeof(int))
                {
                    valueProp.intValue = EditorGUI.IntSlider(rect, valueProp.intValue, (int)range.min, (int)range.max);
                    return;
                }
            }

            EditorGUI.PropertyField(rect, valueProp, GUIContent.none);
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