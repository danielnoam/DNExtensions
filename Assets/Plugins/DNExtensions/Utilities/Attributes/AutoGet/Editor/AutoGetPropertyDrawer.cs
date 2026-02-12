#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
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
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            var fieldRect = position;
            fieldRect.width -= ButtonWidth + 2;
            
            var buttonRect = new Rect(
                position.xMax - ButtonWidth,
                position.y,
                ButtonWidth,
                EditorGUIUtility.singleLineHeight
            );
            
            EditorGUI.PropertyField(fieldRect, property, label, true);
            
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
            
            Undo.RecordObject(targetObject, "Populate AutoGet Field");
            
            AutoGetSystem.PopulateField(targetObject, property.name);
            
            property.serializedObject.Update();
            EditorUtility.SetDirty(targetObject);
        }
    }
}
#endif