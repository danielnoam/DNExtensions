using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// The Reset / Reset Without Children / Reset Only Children menu behind a row's reset button. Shared
    /// so the Transform and RectTransform inspectors offer the same three on the same right-click.
    /// </summary>
    internal static class TransformResetMenu
    {
        public static void Build(GenericMenu menu, Object[] targets, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            menu.AddItem(new GUIContent("Reset"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (!(obj is Transform transform)) continue;

                    Undo.RecordObject(transform, "Reset");
                    setter(transform, resetValue);
                }
            });

            menu.AddItem(new GUIContent("Reset Without Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform transform) ResetWithoutChildren(transform, setter, resetValue);
                }
            });

            menu.AddItem(new GUIContent("Reset Only Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform transform) ResetOnlyChildren(transform, setter, resetValue);
                }
            });
        }

        private static void ResetWithoutChildren(Transform transform, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            int childCount = transform.childCount;
            Vector3[] worldPositions = new Vector3[childCount];
            Quaternion[] worldRotations = new Quaternion[childCount];
            Vector3[] worldScales = new Vector3[childCount];

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                worldPositions[i] = child.position;
                worldRotations[i] = child.rotation;
                worldScales[i] = child.lossyScale;
            }

            Undo.RecordObject(transform, "Reset Without Children");
            for (int i = 0; i < childCount; i++)
            {
                Undo.RecordObject(transform.GetChild(i), "Reset Without Children");
            }

            setter(transform, resetValue);

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.position = worldPositions[i];
                child.rotation = worldRotations[i];
                SetLossyScale(child, worldScales[i]);
            }
        }

        private static void ResetOnlyChildren(Transform transform, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Undo.RecordObject(child, "Reset Only Children");
                setter(child, resetValue);
            }
        }

        private static void SetLossyScale(Transform transform, Vector3 targetLossyScale)
        {
            Transform parent = transform.parent;
            Vector3 parentScale = parent ? parent.lossyScale : Vector3.one;

            transform.localScale = new Vector3(
                parentScale.x != 0f ? targetLossyScale.x / parentScale.x : 1f,
                parentScale.y != 0f ? targetLossyScale.y / parentScale.y : 1f,
                parentScale.z != 0f ? targetLossyScale.z / parentScale.z : 1f
            );
        }
    }
}
