using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// Custom editor for SOBase-derived ScriptableObjects.
    /// </summary>
    [CustomEditor(typeof(SOBase), true)]
    public class SOBaseEditor : Editor
    {
        private SerializedProperty _allowEditingProp;

        private void OnEnable()
        {
            _allowEditingProp = serializedObject.FindProperty("allowEditingInReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);

            DrawPropertiesExcluding(serializedObject, "m_Script", _allowEditingProp.name);
            
            EditorGUILayout.PropertyField(_allowEditingProp, new GUIContent("Show In References"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}