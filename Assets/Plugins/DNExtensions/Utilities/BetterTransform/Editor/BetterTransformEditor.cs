#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class BetterTransformEditor : Editor
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

            bool dummy = false;
            EditorGUI.BeginChangeCheck();
            Vector3 newValue = LinkedVector3Field.Draw("Position", prop.vector3Value, Vector3.zero, showLock: false, ref dummy);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Position Changed");
                foreach (Object obj in targets)
                    if (obj is Transform t) t.localPosition = newValue;
                serializedObject.Update();
            }
        }

        private void DrawRotation()
        {
            if (!(target is Transform main)) return;

            Vector3 euler = main.localEulerAngles;
            Quaternion quaternion = main.localRotation;
            bool dummy = false;

            EditorGUI.BeginChangeCheck();
            Vector3 newEuler = LinkedVector3Field.Draw("Rotation", euler, Vector3.zero, showLock: false, ref dummy,
                extraContextItems: menu =>
                {
                    menu.AddItem(new UnityEngine.GUIContent("Copy Quaternion"), false, () =>
                        EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}");
                });

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, "Rotation Changed");
                foreach (Object obj in targets)
                    if (obj is Transform t) t.localEulerAngles = newEuler;
            }
        }

        private void DrawScale()
        {
            SerializedProperty prop = serializedObject.FindProperty("m_LocalScale");
            if (prop == null) return;

            Vector3 oldScale = prop.vector3Value;

            EditorGUI.BeginChangeCheck();
            Vector3 newValue = LinkedVector3Field.Draw("Scale", oldScale, Vector3.one, showLock: true, ref _scaleLocked);
            if (EditorGUI.EndChangeCheck())
            {
                if (_scaleLocked)
                    newValue = LinkedVector3Field.ApplyLock(oldScale, newValue, _lastScale);

                Undo.RecordObjects(targets, "Scale Changed");
                foreach (Object obj in targets)
                    if (obj is Transform t) t.localScale = newValue;

                _lastScale = newValue;
                serializedObject.Update();
            }
        }
    }
}
#endif