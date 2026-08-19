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
        /// <summary>
        /// One of the three scopes, as something a caller can hold. The RectTransform block names its
        /// own items — it resets a position and a size together, which no single Reset describes — so
        /// it picks a scope and applies it rather than taking the trio below wholesale.
        /// </summary>
        public delegate void Scope(Object[] targets, Action<Transform> apply, string undoName);

        public static void Build(GenericMenu menu, Object[] targets, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            void Apply(Transform transform) => setter(transform, resetValue);

            menu.AddItem(new GUIContent("Reset"), false,
                () => ApplyToTargets(targets, Apply, "Reset"));

            menu.AddItem(new GUIContent("Reset Without Children"), false,
                () => ApplyWithoutChildren(targets, Apply, "Reset Without Children"));

            menu.AddItem(new GUIContent("Reset Only Children"), false,
                () => ApplyToChildren(targets, Apply, "Reset Only Children"));
        }

        /// <summary>The operation on each selected transform, and nothing else.</summary>
        public static void ApplyToTargets(Object[] targets, Action<Transform> apply, string undoName)
        {
            foreach (Object obj in targets)
            {
                if (!(obj is Transform transform)) continue;

                Undo.RecordObject(transform, undoName);
                apply(transform);
            }
        }

        /// <summary>
        /// The operation on each selected transform, with every child put back where it stood in the
        /// world afterwards. Position, rotation and scale only: a stretched RectTransform child takes
        /// its size from the parent it is anchored to, and there is no world size to restore that to.
        /// </summary>
        public static void ApplyWithoutChildren(Object[] targets, Action<Transform> apply, string undoName)
        {
            foreach (Object obj in targets)
            {
                if (obj is Transform transform) ApplyWithoutChildren(transform, apply, undoName);
            }
        }

        /// <summary>The operation on the children instead of on the transform itself.</summary>
        public static void ApplyToChildren(Object[] targets, Action<Transform> apply, string undoName)
        {
            foreach (Object obj in targets)
            {
                if (!(obj is Transform transform)) continue;

                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);

                    Undo.RecordObject(child, undoName);
                    apply(child);
                }
            }
        }

        private static void ApplyWithoutChildren(Transform transform, Action<Transform> apply, string undoName)
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

            Undo.RecordObject(transform, undoName);
            for (int i = 0; i < childCount; i++)
            {
                Undo.RecordObject(transform.GetChild(i), undoName);
            }

            apply(transform);

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.position = worldPositions[i];
                child.rotation = worldRotations[i];
                TransformWorldFields.SetLossyScale(child, worldScales[i]);
            }
        }
    }
}
