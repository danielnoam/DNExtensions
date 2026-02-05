#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet.Editor
{
    using AutoGet;
    
    /// <summary>
    /// Property drawer for AutoGet attributes.
    /// Adds a populate button next to fields with AutoGet attributes.
    /// Works with all Inspector systems (default, Odin, etc.).
    /// </summary>
    [CustomPropertyDrawer(typeof(AutoGetAttribute), useForChildren: true)]
    internal class AutoGetPropertyDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 24f;
        private const string ButtonIcon = "🔄";
        private const string ButtonTooltip = "Populate field";
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var settings = AutoGetSettings.Instance;
            
            if (!settings.ShowPopulateButton)
            {
                // Just draw the default field
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            // Calculate rects
            var fieldRect = position;
            fieldRect.width -= ButtonWidth + 2;
            
            var buttonRect = new Rect(
                position.xMax - ButtonWidth,
                position.y,
                ButtonWidth,
                EditorGUIUtility.singleLineHeight
            );
            
            // Draw the field
            EditorGUI.PropertyField(fieldRect, property, label, true);
            
            // Draw the button
            var buttonContent = new GUIContent(ButtonIcon, ButtonTooltip);
            if (GUI.Button(buttonRect, buttonContent))
            {
                PopulateField(property);
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        private void PopulateField(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject as MonoBehaviour;
            if (targetObject == null) return;
            
            // Record undo
            Undo.RecordObject(targetObject, "Populate AutoGet Field");
            
            // Populate the field
            AutoGetSystem.PopulateField(targetObject, property.name);
            
            // Apply changes
            property.serializedObject.Update();
            EditorUtility.SetDirty(targetObject);
        }
    }
}
#endif