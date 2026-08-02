using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Adds a reset-color button to uGUI Graphic headers — Image, RawImage, Text, and anything else
    /// deriving from Graphic, TextMeshProUGUI included.
    /// </summary>
    [InitializeOnLoad]
    internal static class GraphicResetColorButton
    {
        private const string ResetIcon = "🎨";
        private const string ColorProperty = "m_Color";
        private const string TextColorProperty = "m_fontColor";
        private const string GraphicType = "UnityEngine.UI.Graphic";
        private const string TextType = "TMPro.TMP_Text";

        static GraphicResetColorButton()
        {
            ComponentHeaderButtons.RegisterProvider(GetButton);
        }

        private static ComponentHeaderButtons.ButtonData GetButton(Component component)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = ResetIcon,
                Tooltip = "Reset color to white",
                Priority = -880,
                Callback = ResetColor
            };
        }

        private static bool ShouldShow(Component component)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.graphicResetColorEnabled) return false;

            return GetColorProperty(component) != null;
        }

        /// <summary>
        /// The field the component actually renders from. TextMeshProUGUI is a Graphic but overrides
        /// color with its own m_fontColor and never reads the inherited m_Color, so it has to be
        /// recognised as text first — and the non-UGUI TextMeshPro, which is no Graphic at all, is
        /// only reachable this way.
        /// </summary>
        private static string GetColorProperty(Component component)
        {
            Type type = component.GetType();

            if (HelpfulEditorReflection.DerivesFrom(type, TextType)) return TextColorProperty;
            if (HelpfulEditorReflection.DerivesFrom(type, GraphicType)) return ColorProperty;

            return null;
        }

        /// <summary>
        /// White is the field default for both Graphic and TMP_Text. Written through SerializedObject
        /// rather than the color property so the change records undo and marks a prefab override like
        /// any inspector edit would.
        /// </summary>
        private static void ResetColor(Component component)
        {
            string colorProperty = GetColorProperty(component);
            if (colorProperty == null) return;

            SerializedObject serializedObject = new SerializedObject(component);

            SerializedProperty property = serializedObject.FindProperty(colorProperty);
            if (property == null) return;

            property.colorValue = Color.white;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
