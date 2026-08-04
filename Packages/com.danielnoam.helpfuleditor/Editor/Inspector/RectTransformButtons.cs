using System;
using System.Collections.Generic;
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

        /// <summary>The size Unity's own UI factory gives a new element.</summary>
        private const float DefaultSize = 100f;

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
        /// Sized by whichever numbers the inspector is actually showing. A stretched axis has no
        /// width or height — it has two offsets from its anchors — so this row means something
        /// different per axis, and copying sizeDelta on a stretched rect copies a number nobody is
        /// looking at.
        /// </summary>
        private static ComponentHeaderButtons.ButtonData GetSizeButton(Component component)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = SizeIcon,
                Tooltip = "Size — copy and paste, right-click to reset",
                Priority = -855,
                SupportsMultiSelect = true,
                Callback = ShowSizeValueMenu,
                ContextCallback = ShowSizeResetMenu
            };
        }

        private static bool IsStretched(RectTransform rectTransform, int axis)
        {
            return axis == 0
                ? !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x)
                : !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
        }

        /// <summary>Names the row the way the inspector labels it, so the menu says what it will act on.</summary>
        private static string DescribeSize(RectTransform rectTransform)
        {
            bool x = IsStretched(rectTransform, 0);
            bool y = IsStretched(rectTransform, 1);

            if (x && y) return "offsets";
            if (x) return "left/right and height";
            if (y) return "width and top/bottom";

            return "width and height";
        }

        private static void ShowSizeValueMenu(Component component)
        {
            if (!(component is RectTransform rectTransform)) return;

            string name = DescribeSize(rectTransform);
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent($"Copy {name}"), false, () => CopySize(rectTransform));

            if (CanPasteSize())
            {
                menu.AddItem(new GUIContent($"Paste {name}"), false, () => PasteSize(rectTransform));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent($"Paste {name}"));
            }

            menu.ShowAsContext();
        }

        private static void ShowSizeResetMenu(Component component)
        {
            if (!(component is RectTransform rectTransform)) return;

            GenericMenu menu = new GenericMenu();

            // No keep-children option: a size change moves anchored children by design, and putting
            // them back would fight the layout rather than preserve it.
            menu.AddItem(new GUIContent("Reset"), false, () => Apply(rectTransform, "Reset size", ResetSize));
            menu.AddItem(new GUIContent("Reset Only Children"), false, () => ApplyToChildren(rectTransform, "Reset size", ResetSize));

            menu.ShowAsContext();
        }

        /// <summary>
        /// The offsets, which describe the rect against its anchors whatever the stretch mode is.
        /// Four numbers rather than two, so a rect copied while stretched still pastes correctly.
        /// </summary>
        private static void CopySize(RectTransform rectTransform)
        {
            Vector2 min = rectTransform.offsetMin;
            Vector2 max = rectTransform.offsetMax;

            EditorGUIUtility.systemCopyBuffer = $"{min.x},{min.y},{max.x},{max.y}";
        }

        private static bool CanPasteSize()
        {
            return TryReadOffsets(out _) || LinkedVector3Field.CanPaste();
        }

        private static void PasteSize(RectTransform rectTransform)
        {
            if (TryReadOffsets(out Vector4 offsets))
            {
                Apply(rectTransform, "Paste size", target =>
                {
                    target.offsetMin = new Vector2(offsets.x, offsets.y);
                    target.offsetMax = new Vector2(offsets.z, offsets.w);
                });

                return;
            }

            // Three numbers means it came from one of the other rows, which carry a plain size.
            if (!LinkedVector3Field.TryParseClipboard(out Vector3 value)) return;

            Apply(rectTransform, "Paste size", target => target.sizeDelta = new Vector2(value.x, value.y));
        }

        private static bool TryReadOffsets(out Vector4 offsets)
        {
            offsets = Vector4.zero;

            string[] parts = (EditorGUIUtility.systemCopyBuffer ?? string.Empty).Split(',');
            if (parts.Length != 4) return false;

            if (!float.TryParse(parts[0], out float minX) || !float.TryParse(parts[1], out float minY) ||
                !float.TryParse(parts[2], out float maxX) || !float.TryParse(parts[3], out float maxY)) return false;

            offsets = new Vector4(minX, minY, maxX, maxY);
            return true;
        }

        /// <summary>
        /// A stretched axis resets to sitting on its anchors, an unstretched one to the default size.
        /// Resetting a stretched axis to 100 would be meaningless — that axis has no width to set.
        /// </summary>
        private static void ResetSize(RectTransform rectTransform)
        {
            bool stretchedX = IsStretched(rectTransform, 0);
            bool stretchedY = IsStretched(rectTransform, 1);

            if (stretchedX || stretchedY)
            {
                Vector2 min = rectTransform.offsetMin;
                Vector2 max = rectTransform.offsetMax;

                if (stretchedX) { min.x = 0f; max.x = 0f; }
                if (stretchedY) { min.y = 0f; max.y = 0f; }

                rectTransform.offsetMin = min;
                rectTransform.offsetMax = max;
            }

            if (stretchedX && stretchedY) return;

            Vector2 size = rectTransform.sizeDelta;
            if (!stretchedX) size.x = DefaultSize;
            if (!stretchedY) size.y = DefaultSize;

            rectTransform.sizeDelta = size;
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
            Vector3 resetValue, Func<RectTransform, Vector3> getter, Action<RectTransform, Vector3> setter)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = icon,
                Tooltip = $"{char.ToUpperInvariant(name[0])}{name.Substring(1)} — copy and paste, right-click to reset",
                Priority = priority,
                SupportsMultiSelect = true,
                Callback = target => ShowValueMenu(target, name, getter, setter),
                ContextCallback = target => ShowResetMenu(target, name, resetValue, setter)
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
            Action<RectTransform, Vector3> setter)
        {
            if (!(component is RectTransform rectTransform)) return;

            GenericMenu menu = new GenericMenu();
            string undoName = $"Reset {name}";

            menu.AddItem(new GUIContent("Reset"), false, () => Apply(rectTransform, undoName, target => setter(target, resetValue)));

            menu.AddItem(new GUIContent("Reset Without Children"), false,
                () => ResetKeepingChildren(rectTransform, undoName, target => setter(target, resetValue)));

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
            foreach (RectTransform target in Targets(rectTransform))
            {
                ResetOneKeepingChildren(target, undoName, action);
            }
        }

        private static void ResetOneKeepingChildren(RectTransform rectTransform, string undoName, Action<RectTransform> action)
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

            // Deliberately not Apply: that spans the whole selection, and this already runs once per
            // selected object. Going through it here would reset every target N times over.
            Undo.RecordObject(rectTransform, undoName);
            action(rectTransform);

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

        /// <summary>
        /// Every selected RectTransform, or just this one when it is not part of the selection.
        /// Each target is read and written on its own, so rects with different stretch modes each
        /// get the treatment their own anchoring calls for.
        /// </summary>
        private static List<RectTransform> Targets(RectTransform rectTransform)
        {
            List<RectTransform> targets = new List<RectTransform>();

            foreach (GameObject target in ComponentHeaderButtons.TargetObjects(rectTransform))
            {
                if (target && target.transform is RectTransform rect) targets.Add(rect);
            }

            if (targets.Count == 0 && rectTransform) targets.Add(rectTransform);

            return targets;
        }

        private static void Apply(RectTransform rectTransform, string undoName, Action<RectTransform> action)
        {
            List<RectTransform> targets = Targets(rectTransform);
            if (targets.Count == 0) return;

            Undo.RecordObjects(targets.ToArray(), undoName);

            foreach (RectTransform target in targets)
            {
                action(target);
            }
        }

        private static void ApplyToChildren(RectTransform rectTransform, string undoName, Action<RectTransform> action)
        {
            foreach (RectTransform target in Targets(rectTransform))
            {
                for (int i = 0; i < target.childCount; i++)
                {
                    if (!(target.GetChild(i) is RectTransform child)) continue;

                    Undo.RecordObject(child, undoName);
                    action(child);
                }
            }
        }
    }
}
