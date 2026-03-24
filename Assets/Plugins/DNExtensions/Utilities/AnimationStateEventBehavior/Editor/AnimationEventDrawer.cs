using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(AnimationEvent))]
    public class AnimationEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
        

            SerializedProperty eventName = property.FindPropertyRelative("eventName");
            SerializedProperty animationEvent = property.FindPropertyRelative("onAnimationEvent");

            Rect nameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect eventRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUI.GetPropertyHeight(eventName));

            EditorGUI.PropertyField(nameRect, eventName);
            EditorGUI.PropertyField(eventRect, animationEvent, true);
        
            EditorGUI.EndProperty();
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty animationEvent = property.FindPropertyRelative("onAnimationEvent");
            return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(animationEvent) + 4;
        }
    }
}



