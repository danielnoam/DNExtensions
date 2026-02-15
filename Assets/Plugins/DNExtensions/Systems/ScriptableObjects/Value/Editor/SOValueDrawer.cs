using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.ScriptableObjects
{
    [CustomPropertyDrawer(typeof(SOBase), true)]
    public class SOValueDrawer : PropertyDrawer
    {
        private const float ValueWidthRatio = 0.6f; 
        private const float Gap = 5f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (property.objectReferenceValue != null)
            {
                using var so = new SerializedObject(property.objectReferenceValue);
                var valueProp = so.FindProperty("value");
                var allowEditingProp = so.FindProperty("allowEditingInReference");
                
                bool showValue = allowEditingProp == null || allowEditingProp.boolValue;

                if (showValue && valueProp != null)
                {
                    float totalWidth = position.width;
                    float objectWidth = totalWidth * (1f - ValueWidthRatio);
                    float valueWidth = totalWidth - objectWidth - Gap;

                    Rect valueRect = new Rect(position.x, position.y, valueWidth, position.height);
                    Rect objectRect = new Rect(position.x + valueWidth + Gap, position.y, objectWidth, EditorGUIUtility.singleLineHeight);
                    
                    EditorGUI.ObjectField(objectRect, property, GUIContent.none);

                    so.Update();
                    EditorGUI.BeginChangeCheck();
                    
                    bool prevWideMode = EditorGUIUtility.wideMode;
                    float prevLabelWidth = EditorGUIUtility.labelWidth;
                    int prevIndent = EditorGUI.indentLevel;

                    EditorGUIUtility.wideMode = true;
                    EditorGUI.indentLevel = 0;
                    EditorGUIUtility.labelWidth = 14f;
                    
                    EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);

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
                    EditorGUI.ObjectField(position, property, GUIContent.none);
                }
            }
            else
            {
                EditorGUI.ObjectField(position, property, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue)
            {
                using var so = new SerializedObject(property.objectReferenceValue);
                var valueProp = so.FindProperty("value");
                var allowEditingProp = so.FindProperty("allowEditingInReference");

                bool showValue = allowEditingProp == null || allowEditingProp.boolValue;

                if (showValue && valueProp != null)
                {
                    return Mathf.Max(EditorGUI.GetPropertyHeight(valueProp, true), EditorGUIUtility.singleLineHeight);
                }
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }
}