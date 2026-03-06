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
                foreach (Object obj in targets)
                    if (obj is Transform t) EditorUtility.SetDirty(t);
        }

        private void DrawPosition()
        {
            SerializedProperty prop = serializedObject.FindProperty("m_LocalPosition");
            if (prop == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localPosition, out bool mixed);

            bool dummy = false;
            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = mixed;
            Vector3 newValue = LinkedVector3Field.Draw("Position", displayValue, Vector3.zero, showLock: false, ref dummy);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newValue - displayValue;
                Undo.RecordObjects(targets, "Position Changed");
                foreach (Object obj in targets)
                    if (obj is Transform t)
                        t.localPosition = mixed ? t.localPosition + delta : newValue;
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
                });
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newEuler - displayEuler;
                Undo.RecordObjects(targets, "Rotation Changed");
                foreach (Object obj in targets)
                    if (obj is Transform t)
                        t.localEulerAngles = mixed ? t.localEulerAngles + delta : newEuler;
            }
        }

        private void DrawScale()
        {
            SerializedProperty prop = serializedObject.FindProperty("m_LocalScale");
            if (prop == null) return;

            Vector3 displayValue = GetCommonValue(t => t.localScale, out bool mixed);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = mixed;
            Vector3 newValue = LinkedVector3Field.Draw("Scale", displayValue, Vector3.one, showLock: true, ref _scaleLocked);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                if (_scaleLocked)
                    newValue = LinkedVector3Field.ApplyLock(displayValue, newValue, _lastScale);

                Vector3 delta = newValue - displayValue;
                Undo.RecordObjects(targets, "Scale Changed");
                foreach (Object obj in targets)
                    if (obj is Transform t)
                        t.localScale = mixed ? t.localScale + delta : newValue;

                _lastScale = newValue;
                serializedObject.Update();
            }
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