using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    [CustomEditor(typeof(SOColor))]
    internal class SOColorEditor : SOBaseEditor
    {
        protected override void DrawValueProperty(SerializedProperty valueProperty)
        {
            SerializedProperty isHDRProp = serializedObject.FindProperty("isHDR");
            SerializedProperty showAlphaProp = serializedObject.FindProperty("showAlpha");

            EditorGUI.BeginChangeCheck();

            Color color = EditorGUILayout.ColorField(
                new GUIContent("Value"),
                valueProperty.colorValue,
                showEyedropper: true,
                showAlpha: showAlphaProp?.boolValue ?? true,
                hdr: isHDRProp?.boolValue ?? false
            );

            if (EditorGUI.EndChangeCheck())
            {
                valueProperty.colorValue = color;
            }
        }
    }
}