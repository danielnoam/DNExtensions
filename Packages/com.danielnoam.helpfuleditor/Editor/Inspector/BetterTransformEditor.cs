using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal class BetterTransformEditor : Editor
    {
        private static readonly Type NativeEditorType = typeof(Editor).Assembly.GetType("UnityEditor.TransformInspector");

        private static bool _scaleLocked;
        private static bool _scaleLockInitialized;

        private Editor _nativeEditor;
        private Vector3 _lastScale;

        private void OnEnable()
        {
            if (target is Transform transform) _lastScale = transform.localScale;

            if (!_scaleLockInitialized)
            {
                _scaleLocked = HelpfulEditorSettings.Inspector.scaleLockDefaultOn;
                _scaleLockInitialized = true;
            }
        }

        private void OnDisable()
        {
            if (_nativeEditor) DestroyImmediate(_nativeEditor);
        }

        public override void OnInspectorGUI()
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;

            if (!settings.moduleEnabled || !settings.betterTransformEnabled)
            {
                DrawNativeInspector();
                return;
            }

            serializedObject.Update();

            DrawPosition(settings);
            DrawRotation(settings);
            DrawScale(settings);

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    if (obj is Transform transform) EditorUtility.SetDirty(transform);
                }
            }
        }

        private void DrawNativeInspector()
        {
            if (!_nativeEditor && NativeEditorType != null) _nativeEditor = CreateEditor(targets, NativeEditorType);

            if (_nativeEditor) _nativeEditor.OnInspectorGUI();
            else DrawDefaultInspector();
        }

        private void DrawPosition(InspectorSettings settings)
        {
            if (serializedObject.FindProperty("m_LocalPosition") == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localPosition, out bool mixed);
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Position", displayValue, Vector3.zero, false, ref unusedLock,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localPosition = v, Vector3.zero)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newValue - displayValue;
            Undo.RecordObjects(targets, "Position Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localPosition = mixed ? transform.localPosition + delta : newValue;
            }

            serializedObject.Update();
        }

        private void DrawRotation(InspectorSettings settings)
        {
            if (!(target is Transform main)) return;

            Vector3 displayEuler = GetCommonValue(t => t.localEulerAngles, out bool mixed);
            Quaternion quaternion = main.localRotation;
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, false, ref unusedLock,
                extraContextItems: menu => menu.AddItem(new GUIContent("Copy Quaternion"), false, () =>
                    EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}"),
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localEulerAngles = v, Vector3.zero)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newEuler - displayEuler;
            Undo.RecordObjects(targets, "Rotation Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localEulerAngles = mixed ? transform.localEulerAngles + delta : newEuler;
            }
        }

        private void DrawScale(InspectorSettings settings)
        {
            if (serializedObject.FindProperty("m_LocalScale") == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, true, ref _scaleLocked,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localScale = v, Vector3.one)
                    : null);

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            if (_scaleLocked) newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

            Vector3 delta = newValue - displayValue;
            Undo.RecordObjects(targets, "Scale Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localScale = mixed ? transform.localScale + delta : newValue;
            }

            _lastScale = newValue;
            serializedObject.Update();
        }

        private void BuildResetMenu(GenericMenu menu, Action<Transform, Vector3> setter, Vector3 resetValue)
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

        private Vector3 GetCommonValue(Func<Transform, Vector3> selector, out bool mixed)
        {
            Vector3 first = target is Transform firstTransform ? selector(firstTransform) : Vector3.zero;
            mixed = false;

            foreach (Object obj in targets)
            {
                if (!(obj is Transform transform)) continue;

                if (selector(transform) != first)
                {
                    mixed = true;
                    break;
                }
            }

            return first;
        }
    }
}
