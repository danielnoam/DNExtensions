#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(LinkedVector3Attribute))]
    public class LinkedVector3Drawer : PropertyDrawer
    {
        private bool _locked;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector3)
                return EditorGUIUtility.singleLineHeight;

            if (EditorGUIUtility.wideMode)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector3)
            {
                EditorGUI.HelpBox(position, "[LinkedVector3] only works on Vector3 fields", MessageType.Error);
                return;
            }

            var attr = (LinkedVector3Attribute)attribute;
            Vector3 resetValue = new Vector3(attr.ResetX, attr.ResetY, attr.ResetZ);

            EditorGUI.BeginChangeCheck();
            Vector3 newValue = LinkedVector3Field.Draw(position, label.text, property.vector3Value, resetValue, attr.ShowLock, ref _locked, out _);
            if (EditorGUI.EndChangeCheck())
                property.vector3Value = newValue;
        }
    }
}
#endif