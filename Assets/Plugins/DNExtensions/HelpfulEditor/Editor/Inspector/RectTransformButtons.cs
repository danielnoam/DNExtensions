using System;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Per-row buttons on RectTransform headers, one for each thing Unity's own inspector can only
    /// reset all at once. Left-click offers copy and paste of that row's value, right-click the reset
    /// actions — the destructive one is the one you have to go looking for.
    /// </summary>
    [InitializeOnLoad]
    internal static class RectTransformButtons
    {
        private const string PositionIcon = "P";
        private const string SizeIcon = "WH";
        private const string RotationIcon = "R";
        private const string ScaleIcon = "S";

        private static readonly Vector3 DefaultSize = new Vector3(100f, 100f, 0f);

        static RectTransformButtons()
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
            return Build(component, PositionIcon, "position", -860, Vector3.zero,
                rectTransform => rectTransform.anchoredPosition3D,
                (rectTransform, value) => rectTransform.anchoredPosition3D = value);
        }

        /// <summary>
        /// Carried as a Vector3 with an unused z so it shares one clipboard format with the other
        /// rows: a size copied here pastes onto a scale, and a Transform's position pastes onto a size.
        /// </summary>
        private static ComponentHeaderButtons.ButtonData GetSizeButton(Component component)
        {
            // No keep-children option: a size change moves anchored children by design, and putting
            // them back would fight the layout rather than preserve it.
            return Build(component, SizeIcon, "width and height", -855, DefaultSize,
                rectTransform => new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 0f),
                (rectTransform, value) => rectTransform.sizeDelta = new Vector2(value.x, value.y),
                canKeepChildren: false);
        }

        private static ComponentHeaderButtons.ButtonData GetRotationButton(Component component)
        {
            return Build(component, RotationIcon, "rotation", -850, Vector3.zero,
                rectTransform => rectTransform.localEulerAngles,
                (rectTransform, value) => rectTransform.localEulerAngles = value);
        }

        private static ComponentHeaderButtons.ButtonData GetScaleButton(Component component)
        {
            return Build(component, ScaleIcon, "scale", -840, Vector3.one,
                rectTransform => rectTransform.localScale,
                (rectTransform, value) => rectTransform.localScale = value);
        }

        private static ComponentHeaderButtons.ButtonData Build(Component component, string icon, string name, int priority,
            Vector3 resetValue, Func<RectTransform, Vector3> getter, Action<RectTransform, Vector3> setter, bool canKeepChildren = true)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = icon,
                Tooltip = $"{char.ToUpperInvariant(name[0])}{name.Substring(1)} — copy and paste, right-click to reset",
                Priority = priority,
                Callback = target => ShowValueMenu(target, name, getter, setter),
                ContextCallback = target => ShowResetMenu(target, name, resetValue, setter, canKeepChildren)
            };
        }

        private static bool ShouldShow(Component component)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.rectTransformResetButtonsEnabled) return false;

            return component is RectTransform;
        }

        private static void ShowValueMenu(Component component, string name, Func<RectTransform, Vector3> getter, Action<RectTransform, Vector3> setter)
        {
            if (!(component is RectTransform rectTransform)) return;

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent($"Copy {name}"), false, () => LinkedVector3Field.CopyToClipboard(getter(rectTransform)));

            // Nothing parseable on the clipboard is shown greyed rather than hidden, so the menu has
            // the same shape either way and the button does not look broken when the clipboard is text.
            if (LinkedVector3Field.CanPaste())
            {
                menu.AddItem(new GUIContent($"Paste {name}"), false, () =>
                {
                    if (!LinkedVector3Field.TryParseClipboard(out Vector3 value)) return;

                    Apply(rectTransform, $"Paste {name}", target => setter(target, value));
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent($"Paste {name}"));
            }

            menu.ShowAsContext();
        }

        private static void ShowResetMenu(Component component, string name, Vector3 resetValue,
            Action<RectTransform, Vector3> setter, bool canKeepChildren)
        {
            if (!(component is RectTransform rectTransform)) return;

            GenericMenu menu = new GenericMenu();
            string undoName = $"Reset {name}";

            menu.AddItem(new GUIContent("Reset"), false, () => Apply(rectTransform, undoName, target => setter(target, resetValue)));

            if (canKeepChildren)
            {
                menu.AddItem(new GUIContent("Reset Without Children"), false,
                    () => ResetKeepingChildren(rectTransform, undoName, target => setter(target, resetValue)));
            }

            menu.AddItem(new GUIContent("Reset Only Children"), false, () => ApplyToChildren(rectTransform, undoName, target => setter(target, resetValue)));

            menu.ShowAsContext();
        }

        /// <summary>
        /// Resets this object while leaving the children where they are on screen, by putting their
        /// world transforms back afterwards.
        ///
        /// Scale is restored through lossyScale, which cannot represent a skew — a child under a
        /// rotated, non-uniformly scaled parent comes back approximately rather than exactly. Unity's
        /// own transform tools have the same limit, because a Transform has nowhere to store one.
        /// </summary>
        private static void ResetKeepingChildren(RectTransform rectTransform, string undoName, Action<RectTransform> action)
        {
            if (!rectTransform) return;

            int count = rectTransform.childCount;
            Vector3[] positions = new Vector3[count];
            Quaternion[] rotations = new Quaternion[count];
            Vector3[] scales = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Transform child = rectTransform.GetChild(i);

                positions[i] = child.position;
                rotations[i] = child.rotation;
                scales[i] = child.lossyScale;

                Undo.RecordObject(child, undoName);
            }

            Apply(rectTransform, undoName, action);

            Vector3 parentScale = rectTransform.lossyScale;

            for (int i = 0; i < count; i++)
            {
                Transform child = rectTransform.GetChild(i);

                child.position = positions[i];
                child.rotation = rotations[i];
                child.localScale = RelativeScale(scales[i], parentScale);
            }
        }

        /// <summary>The local scale that reproduces a world scale under this parent. A zeroed parent axis is left alone.</summary>
        private static Vector3 RelativeScale(Vector3 world, Vector3 parent)
        {
            return new Vector3(
                Mathf.Approximately(parent.x, 0f) ? world.x : world.x / parent.x,
                Mathf.Approximately(parent.y, 0f) ? world.y : world.y / parent.y,
                Mathf.Approximately(parent.z, 0f) ? world.z : world.z / parent.z);
        }

        private static void Apply(RectTransform rectTransform, string undoName, Action<RectTransform> action)
        {
            if (!rectTransform) return;

            Undo.RecordObject(rectTransform, undoName);
            action(rectTransform);
        }

        private static void ApplyToChildren(RectTransform rectTransform, string undoName, Action<RectTransform> action)
        {
            if (!rectTransform) return;

            for (int i = 0; i < rectTransform.childCount; i++)
            {
                if (!(rectTransform.GetChild(i) is RectTransform child)) continue;

                Undo.RecordObject(child, undoName);
                action(child);
            }
        }
    }
}
