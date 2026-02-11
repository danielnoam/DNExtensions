#if UNITY_EDITOR
using UnityEditor;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(Note))]
    [CanEditMultipleObjects]
    public class NoteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty textProp = serializedObject.FindProperty("text");
            SerializedProperty showInSceneViewProp = serializedObject.FindProperty("showInSceneView");
            SerializedProperty gizmoColorProp = serializedObject.FindProperty("gizmoColor");

            EditorGUILayout.PropertyField(textProp);
            EditorGUILayout.PropertyField(showInSceneViewProp);

            if (showInSceneViewProp.boolValue)
            {
                EditorGUILayout.PropertyField(gizmoColorProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif