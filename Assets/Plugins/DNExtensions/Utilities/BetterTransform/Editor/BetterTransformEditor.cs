using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal class BetterTransformEditor : Editor
    {
        private static bool _scaleLocked;
        private Vector3 _lastScale;

        private void OnEnable()
        {
            if (target is Transform t) _lastScale = t.localScale;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPosition();
            DrawRotation();
            DrawScale();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        EditorUtility.SetDirty(t);
                    }
                }
            }
        }

        private void DrawPosition()
        {
            SerializedProperty prop = serializedObject.FindProperty("m_LocalPosition");
            if (prop == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localPosition, out bool mixed);

            bool dummy = false;
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = mixed;
            Vector3 newValue = LinkedVector3Field.Draw("Position", displayValue, Vector3.zero, showLock: false, ref dummy,
                extraResetItems: menu => BuildResetMenu(menu, t => t.localPosition, (t, v) => t.localPosition = v, Vector3.zero));
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newValue - displayValue;
                Undo.RecordObjects(targets, "Position Changed");
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        t.localPosition = mixed ? t.localPosition + delta : newValue;
                    }
                }
                serializedObject.Update();
            }
        }

        private void DrawRotation()
        {
            if (!(target is Transform main)) return;

            Vector3 displayEuler = GetCommonValue(t => t.localEulerAngles, out bool mixed);
            Quaternion quaternion = main.localRotation;
            bool dummy = false;

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = mixed;
            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, showLock: false, ref dummy,
                extraContextItems: menu =>
                {
                    menu.AddItem(new GUIContent("Copy Quaternion"), false, () =>
                        EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}");
                },
                extraResetItems: menu => BuildResetMenu(menu, t => t.localEulerAngles, (t, v) => t.localEulerAngles = v, Vector3.zero));
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newEuler - displayEuler;
                Undo.RecordObjects(targets, "Rotation Changed");
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        t.localEulerAngles = mixed ? t.localEulerAngles + delta : newEuler;
                    }
                }
            }
        }

        private void DrawScale()
        {
            SerializedProperty prop = serializedObject.FindProperty("m_LocalScale");
            if (prop == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = mixed;
            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, showLock: true, ref _scaleLocked,
                extraResetItems: menu => BuildResetMenu(menu, t => t.localScale, (t, v) => t.localScale = v, Vector3.one));
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                if (_scaleLocked)
                    newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

                Vector3 delta = newValue - displayValue;
                Undo.RecordObjects(targets, "Scale Changed");
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        t.localScale = mixed ? t.localScale + delta : newValue;
                    }
                }

                _lastScale = newValue;
                serializedObject.Update();
            }
        }

        private void BuildResetMenu(GenericMenu menu, System.Func<Transform, Vector3> getter, System.Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            menu.AddItem(new GUIContent("Reset"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        Undo.RecordObject(t, "Reset");
                        setter(t, resetValue);
                    }
                }
            });

            menu.AddItem(new GUIContent("Reset Without Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        ResetWithoutChildren(t, setter, resetValue);
                    }
                }
            });

            menu.AddItem(new GUIContent("Reset Only Children"), false, () =>
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform t)
                    {
                        ResetOnlyChildren(t, setter, resetValue);
                    }
                }
            });
        }

        private static void ResetWithoutChildren(Transform t, System.Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            int childCount = t.childCount;
            var worldPositions = new Vector3[childCount];
            var worldRotations = new Quaternion[childCount];
            var worldScales = new Vector3[childCount];

            for (int i = 0; i < childCount; i++)
            {
                Transform child = t.GetChild(i);
                worldPositions[i] = child.position;
                worldRotations[i] = child.rotation;
                worldScales[i] = child.lossyScale;
            }

            Undo.RecordObject(t, "Reset Without Children");
            for (int i = 0; i < childCount; i++)
            {
                Undo.RecordObject(t.GetChild(i), "Reset Without Children");
            }

            setter(t, resetValue);

            for (int i = 0; i < childCount; i++)
            {
                Transform child = t.GetChild(i);
                child.position = worldPositions[i];
                child.rotation = worldRotations[i];
                SetLossyScale(child, worldScales[i]);
            }
        }

        private static void ResetOnlyChildren(Transform t, System.Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                Undo.RecordObject(child, "Reset Only Children");
                setter(child, resetValue);
            }
        }

        private static void SetLossyScale(Transform t, Vector3 targetLossyScale)
        {
            Transform parent = t.parent;
            Vector3 parentScale = parent ? parent.lossyScale : Vector3.one;

            t.localScale = new Vector3(
                parentScale.x != 0f ? targetLossyScale.x / parentScale.x : 1f,
                parentScale.y != 0f ? targetLossyScale.y / parentScale.y : 1f,
                parentScale.z != 0f ? targetLossyScale.z / parentScale.z : 1f
            );
        }

        private Vector3 GetCommonValue(System.Func<Transform, Vector3> selector, out bool mixed)
        {
            Vector3 first = target is Transform t0 ? selector(t0) : Vector3.zero;
            mixed = false;

            foreach (Object obj in targets)
            {
                if (!(obj is Transform t)) continue;
                if (selector(t) != first)
                {
                    mixed = true;
                    break;
                }
            }

            return first;
        }
    }
}