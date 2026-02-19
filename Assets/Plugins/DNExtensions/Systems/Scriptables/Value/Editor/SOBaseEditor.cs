using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    [CustomEditor(typeof(SOBase), true)]
    public class SOBaseEditor : Editor
    {
        private SerializedProperty allowEditingProp;

        private void OnEnable()
        {
            allowEditingProp = serializedObject.FindProperty("allowEditingInReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);

            DrawPropertiesExcluding(serializedObject, "m_Script", allowEditingProp.name);
            
            EditorGUILayout.PropertyField(allowEditingProp, new GUIContent("Show In References"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}