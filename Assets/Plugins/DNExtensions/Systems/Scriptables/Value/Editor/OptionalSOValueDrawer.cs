using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    [CustomPropertyDrawer(typeof(OptionalSOValue<>), true)]
    internal class OptionalSOValueDrawer : PropertyDrawer
    {
        private const float ValueWidthRatio = 0.6f;
        private const float Gap = 5f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            SerializedProperty soValueProp = property.FindPropertyRelative("soValue");
            SerializedProperty fallbackProp = property.FindPropertyRelative("fallbackValue");

            float totalWidth = position.width;
            float objectWidth = totalWidth * (1f - ValueWidthRatio);
            float valueWidth = totalWidth - objectWidth - Gap;

            Rect valueRect = new Rect(position.x, position.y, valueWidth, position.height);
            Rect objectRect = new Rect(position.x + valueWidth + Gap, position.y, objectWidth, EditorGUIUtility.singleLineHeight);

            if (soValueProp.objectReferenceValue)
            {
                using var so = new SerializedObject(soValueProp.objectReferenceValue);
                SerializedProperty valueProp = so.FindProperty("value");
                SerializedProperty allowEditingProp = so.FindProperty("allowEditingInReference");

                bool showValue = allowEditingProp == null || allowEditingProp.boolValue;

                if (showValue && valueProp != null)
                {
                    so.Update();
                    EditorGUI.BeginChangeCheck();

                    bool prevWideMode = EditorGUIUtility.wideMode;
                    float prevLabelWidth = EditorGUIUtility.labelWidth;
                    int prevIndent = EditorGUI.indentLevel;

                    EditorGUIUtility.wideMode = true;
                    EditorGUI.indentLevel = 0;
                    EditorGUIUtility.labelWidth = 14f;

                    DrawColorAwareField(valueRect, so, valueProp);

                    EditorGUI.indentLevel = prevIndent;
                    EditorGUIUtility.labelWidth = prevLabelWidth;
                    EditorGUIUtility.wideMode = prevWideMode;

                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();
                    }
                }
                else
                {
                    objectRect = position;
                }
            }
            else
            {
                bool prevWideMode = EditorGUIUtility.wideMode;
                float prevLabelWidth = EditorGUIUtility.labelWidth;
                int prevIndent = EditorGUI.indentLevel;

                EditorGUIUtility.wideMode = true;
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 14f;

                EditorGUI.PropertyField(valueRect, fallbackProp, GUIContent.none, true);

                EditorGUI.indentLevel = prevIndent;
                EditorGUIUtility.labelWidth = prevLabelWidth;
                EditorGUIUtility.wideMode = prevWideMode;
            }

            EditorGUI.ObjectField(objectRect, soValueProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty soValueProp = property.FindPropertyRelative("soValue");

            if (soValueProp.objectReferenceValue)
            {
                using var so = new SerializedObject(soValueProp.objectReferenceValue);
                SerializedProperty valueProp = so.FindProperty("value");
                SerializedProperty allowEditingProp = so.FindProperty("allowEditingInReference");

                bool showValue = allowEditingProp == null || allowEditingProp.boolValue;

                if (showValue && valueProp != null)
                {
                    return Mathf.Max(EditorGUI.GetPropertyHeight(valueProp, true), EditorGUIUtility.singleLineHeight);
                }
            }
            else
            {
                SerializedProperty fallbackProp = property.FindPropertyRelative("fallbackValue");
                return Mathf.Max(EditorGUI.GetPropertyHeight(fallbackProp, true), EditorGUIUtility.singleLineHeight);
            }

            return EditorGUIUtility.singleLineHeight;
        }

        private static void DrawColorAwareField(Rect rect, SerializedObject so, SerializedProperty valueProp)
        {
            if (valueProp.propertyType != SerializedPropertyType.Color)
            {
                EditorGUI.PropertyField(rect, valueProp, GUIContent.none, true);
                return;
            }

            SerializedProperty isHDRProp = so.FindProperty("isHDR");
            SerializedProperty showAlphaProp = so.FindProperty("showAlpha");

            if (isHDRProp == null && showAlphaProp == null)
            {
                EditorGUI.PropertyField(rect, valueProp, GUIContent.none, true);
                return;
            }

            valueProp.colorValue = EditorGUI.ColorField(
                rect,
                GUIContent.none,
                valueProp.colorValue,
                showEyedropper: true,
                showAlpha: showAlphaProp?.boolValue ?? true,
                hdr: isHDRProp?.boolValue ?? false
            );
        }
    }
}