using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Adds per-row reset buttons to RectTransform headers. Unity's own inspector only resets the
    /// whole component at once, and the suite no longer replaces that inspector. Right-clicking a
    /// button offers the same choices the inline reset buttons carry elsewhere in the suite.
    /// </summary>
    [InitializeOnLoad]
    internal static class RectTransformResetButtons
    {
        private const string PositionIcon = "P";
        private const string SizeIcon = "WH";
        private const string RotationIcon = "R";
        private const string ScaleIcon = "S";

        private static readonly Vector2 DefaultSize = new Vector2(100f, 100f);

        static RectTransformResetButtons()
        {
            // One registration per button: a provider contributes a single ButtonData per component.
            ComponentHeaderButtons.RegisterProvider(GetPositionButton);
            ComponentHeaderButtons.RegisterProvider(GetSizeButton);
            ComponentHeaderButtons.RegisterProvider(GetRotationButton);
            ComponentHeaderButtons.RegisterProvider(GetScaleButton);
        }

        /// <summary>Zeroes x, y and z together — anchoredPosition alone would leave Pos Z behind.</summary>
        private static ComponentHeaderButtons.ButtonData GetPositionButton(Component component)
        {
            return Build(component, PositionIcon, "position", -860,
                rectTransform => rectTransform.anchoredPosition3D = Vector3.zero);
        }

        /// <summary>
        /// The size Unity's own UI factory gives a new element. Zero would be the literal property
        /// default, but that collapses an unstretched rect to nothing.
        /// </summary>
        private static ComponentHeaderButtons.ButtonData GetSizeButton(Component component)
        {
            return Build(component, SizeIcon, "width and height", -855,
                rectTransform => rectTransform.sizeDelta = DefaultSize);
        }

        private static ComponentHeaderButtons.ButtonData GetRotationButton(Component component)
        {
            return Build(component, RotationIcon, "rotation", -850,
                rectTransform => rectTransform.localRotation = Quaternion.identity);
        }

        private static ComponentHeaderButtons.ButtonData GetScaleButton(Component component)
        {
            return Build(component, ScaleIcon, "scale", -840,
                rectTransform => rectTransform.localScale = Vector3.one);
        }

        private static ComponentHeaderButtons.ButtonData Build(Component component, string icon, string name, int priority, Action<RectTransform> setter)
        {
            if (!ShouldShow(component)) return null;

            string undoName = $"Reset {name}";

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = icon,
                Tooltip = $"Reset {name} — right-click for more",
                Priority = priority,
                Callback = target => Apply(target, undoName, setter),
                ContextCallback = target => ShowMenu(target, undoName, setter)
            };
        }

        private static bool ShouldShow(Component component)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.rectTransformResetButtonsEnabled) return false;

            return component is RectTransform;
        }

        private static void ShowMenu(Component component, string undoName, Action<RectTransform> setter)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Reset"), false, () => Apply(component, undoName, setter));
            menu.AddItem(new GUIContent("Reset Only Children"), false, () => ApplyToChildren(component, undoName, setter));

            menu.ShowAsContext();
        }

        private static void Apply(Component component, string undoName, Action<RectTransform> setter)
        {
            if (!(component is RectTransform rectTransform)) return;

            Undo.RecordObject(rectTransform, undoName);
            setter(rectTransform);
        }

        private static void ApplyToChildren(Component component, string undoName, Action<RectTransform> setter)
        {
            if (!(component is RectTransform rectTransform)) return;

            for (int i = 0; i < rectTransform.childCount; i++)
            {
                if (!(rectTransform.GetChild(i) is RectTransform child)) continue;

                Undo.RecordObject(child, undoName);
                setter(child);
            }
        }
    }
}
