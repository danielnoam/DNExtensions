using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.ScriptableObjects
{
    public abstract class SODrawer : PropertyDrawer
    {
        private const float ValueWidthRatio = 0.3f; 
        private const float Gap = 5f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (property.objectReferenceValue != null)
            {
                float totalWidth = position.width;
                float objectWidth = totalWidth * ValueWidthRatio;
                float valueWidth = totalWidth - objectWidth - Gap;

                Rect valueRect = new Rect(position.x, position.y, objectWidth, position.height);
                Rect objectRect = new Rect(position.x + objectWidth + Gap, position.y, valueWidth, position.height);
                
                // Draw the Object Field
                EditorGUI.ObjectField(objectRect, property, GUIContent.none);
                
                if (!property.objectReferenceValue) 
                {
                    EditorGUI.EndProperty();
                    return;
                }

                using var so = new SerializedObject(property.objectReferenceValue);
                SerializedProperty valueProp = so.FindProperty("value");
                if (valueProp != null)
                {
                    so.Update();
                    EditorGUI.BeginChangeCheck();
                        
                    EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();
                    }
                }
            }
            else
            {
                EditorGUI.ObjectField(position, property, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(SOFloat))]
    public class SOFloatDrawer : SODrawer { }

    [CustomPropertyDrawer(typeof(SOInt))]
    public class SOIntDrawer : SODrawer { }
}