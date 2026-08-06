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
        private const string EulerHintProperty = "m_LocalEulerAnglesHint";

        private static readonly Type NativeEditorType = typeof(Editor).Assembly.GetType("UnityEditor.TransformInspector");

        private static bool _scaleLocked;
        private static bool _scaleLockInitialized;
        private static bool _scaleLockDefault;

        private Editor _nativeEditor;
        private Vector3 _lastScale;

        private readonly TransformWorldFields _worldFields = new TransformWorldFields();

        private void OnEnable()
        {
            if (target is Transform transform) _lastScale = transform.localScale;

            _worldFields.Forget();

            // The lock is deliberately shared across inspectors and kept across selections, so it is
            // only seeded from the setting once. Comparing against the last seeded value is what
            // makes changing that setting take effect without waiting for a recompile.
            bool setting = HelpfulEditorSettings.Inspector.scaleLockDefaultOn;

            if (_scaleLockInitialized && setting == _scaleLockDefault) return;

            _scaleLocked = setting;
            _scaleLockDefault = setting;
            _scaleLockInitialized = true;
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

            bool showWorld = ShowWorldFields(settings);

            DrawPosition(settings);
            DrawRotation(settings);
            DrawScale(settings);

            if (showWorld) _worldFields.Draw(serializedObject, target as Transform, targets);

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
            SerializedProperty property = serializedObject.FindProperty("m_LocalPosition");
            if (property == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localPosition, out bool mixed);
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Position", displayValue, Vector3.zero, false, ref unusedLock,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localPosition = v, Vector3.zero)
                    : null,
                property: property);

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

        /// <summary>
        /// Rotation is shown from Unity's own euler hint rather than from localEulerAngles.
        ///
        /// localEulerAngles is derived from the quaternion, so it is one of several equivalent
        /// answers — type 370 and it reads back 10, and values jump about while editing near gimbal
        /// configurations. Unity's Transform inspector keeps m_LocalEulerAnglesHint for exactly this
        /// reason, and replacing that inspector without carrying the hint forward loses the property
        /// most people would notice first.
        /// </summary>
        private void DrawRotation(InspectorSettings settings)
        {
            if (!(target is Transform main)) return;

            SerializedProperty hint = serializedObject.FindProperty(EulerHintProperty);

            Vector3 displayEuler;
            bool mixed;

            if (hint != null)
            {
                displayEuler = hint.vector3Value;
                mixed = hint.hasMultipleDifferentValues;
            }
            else
            {
                displayEuler = GetCommonValue(t => t.localEulerAngles, out mixed);
            }

            Quaternion quaternion = main.localRotation;
            bool unusedLock = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", displayEuler, Vector3.zero, false, ref unusedLock,
                extraContextItems: menu => menu.AddItem(new GUIContent("Copy Quaternion"), false, () =>
                    EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}"),
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localEulerAngles = v, Vector3.zero)
                    : null,
                property: serializedObject.FindProperty("m_LocalRotation"));

            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;

            Vector3 delta = newEuler - displayEuler;
            Undo.RecordObjects(targets, "Rotation Changed");

            foreach (Object obj in targets)
            {
                if (obj is Transform transform) transform.localEulerAngles = mixed ? transform.localEulerAngles + delta : newEuler;
            }

            serializedObject.Update();

            // Written back explicitly rather than trusting the setter to have maintained it, so the
            // number just typed is the number that reads back on the next repaint.
            if (hint != null && !mixed)
            {
                hint.vector3Value = newEuler;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawScale(InspectorSettings settings)
        {
            SerializedProperty property = serializedObject.FindProperty("m_LocalScale");
            if (property == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixed;

            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, true, ref _scaleLocked,
                extraResetItems: settings.resetMenuItemsEnabled
                    ? menu => BuildResetMenu(menu, (t, v) => t.localScale = v, Vector3.one)
                    : null,
                property: property);

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

        /// <summary>
        /// The world block is only drawn under a parent: a root object's local values already are its
        /// world values, so the second set would be an exact copy of the first sitting beneath it.
        /// </summary>
        private bool ShowWorldFields(InspectorSettings settings) =>
            settings.worldFieldsEnabled && TransformWorldFields.AnyParented(targets);

        private void BuildResetMenu(GenericMenu menu, Action<Transform, Vector3> setter, Vector3 resetValue)
        {
            TransformResetMenu.Build(menu, targets, setter, resetValue);
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
