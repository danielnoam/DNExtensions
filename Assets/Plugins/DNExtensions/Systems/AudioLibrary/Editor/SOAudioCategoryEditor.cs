using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomEditor(typeof(SOAudioCategory))]
    public class SOAudioCategoryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("label"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioMixerGroup"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mappings", EditorStyles.boldLabel);
            SerializedProperty list = serializedObject.FindProperty("audioMappings");

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.PropertyField(element, GUIContent.none);

                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    list.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            
            if (GUILayout.Button("+ Add New Mapping"))
            {
                list.InsertArrayElementAtIndex(list.arraySize);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}