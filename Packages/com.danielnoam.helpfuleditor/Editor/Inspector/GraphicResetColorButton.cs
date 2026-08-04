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
        private static readonly string ResetIcon = HelpfulEditorPlatform.Glyph("🎨", "Color");
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
                SupportsMultiSelect = true,
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
        /// <summary>
        /// Resolved per object rather than once: a selection can mix Images with TextMeshPro, and the
        /// two keep their colour in different fields.
        /// </summary>
        private static void ResetColor(Component component)
        {
            foreach (GameObject target in ComponentHeaderButtons.TargetObjects(component))
            {
                if (!target) continue;

                foreach (Component candidate in target.GetComponents<Component>())
                {
                    string colorProperty = candidate ? GetColorProperty(candidate) : null;
                    if (colorProperty == null) continue;

                    SerializedObject serializedObject = new SerializedObject(candidate);

                    SerializedProperty property = serializedObject.FindProperty(colorProperty);
                    if (property == null) continue;

                    property.colorValue = Color.white;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
