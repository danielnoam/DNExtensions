using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// Custom editor for <see cref="SOColorHDR"/> that draws the value field with HDR support.
    /// </summary>
    [CustomEditor(typeof(SOColorHDR))]
    public class SOColorHDREditor : SOBaseEditor
    {
        protected override void DrawValueProperty(SerializedProperty valueProperty)
        {
            EditorGUI.BeginChangeCheck();

            Color color = EditorGUILayout.ColorField(
                new GUIContent("Value"),
                valueProperty.colorValue,
                showEyedropper: true,
                showAlpha: true,
                hdr: true
            );

            if (EditorGUI.EndChangeCheck())
            {
                valueProperty.colorValue = color;
            }
        }
    }
}