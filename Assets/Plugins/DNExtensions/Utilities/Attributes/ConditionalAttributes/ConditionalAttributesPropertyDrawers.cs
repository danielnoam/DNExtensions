#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace DNExtensions
{
    /// <summary>
    /// Property drawer for ShowIf attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            bool shouldShow = showIf.Evaluate(property);
            
            // Return negative spacing to eliminate a gap when hidden
            return shouldShow ? EditorGUI.GetPropertyHeight(property, label, true) : -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            bool shouldShow = showIf.Evaluate(property);
            
            if (!shouldShow) return;
            
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    /// <summary>
    /// Property drawer for HideIf attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HideIfAttribute hideIf = (HideIfAttribute)attribute;
            bool shouldHide = hideIf.Evaluate(property);
            
            // Return negative spacing to eliminate a gap when hidden
            return shouldHide ? -EditorGUIUtility.standardVerticalSpacing : EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HideIfAttribute hideIf = (HideIfAttribute)attribute;
            bool shouldHide = hideIf.Evaluate(property);
            
            if (shouldHide) return;
            
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    /// <summary>
    /// Property drawer for EnableIf attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
            bool shouldEnable = enableIf.Evaluate(property);
            
            bool wasEnabled = GUI.enabled;
            GUI.enabled = shouldEnable;
            
            EditorGUI.PropertyField(position, property, label, true);
            
            GUI.enabled = wasEnabled;
        }
    }

    /// <summary>
    /// Property drawer for DisableIf attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DisableIfAttribute disableIf = (DisableIfAttribute)attribute;
            bool shouldDisable = disableIf.Evaluate(property);
            
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !shouldDisable;
            
            EditorGUI.PropertyField(position, property, label, true);
            
            GUI.enabled = wasEnabled;
        }
    }
}
#endif