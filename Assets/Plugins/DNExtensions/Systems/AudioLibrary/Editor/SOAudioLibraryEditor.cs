#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomEditor(typeof(SOAudioLibrarySettings))]
    public class SOAudioLibraryEditor : Editor
    {
        private readonly Dictionary<string, bool> _foldouts = new();
        private readonly Dictionary<string, SerializedObject> _serializedCategories = new();

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(10);
            base.OnInspectorGUI();
            var audioLibrary = (SOAudioLibrarySettings)target;

            if (audioLibrary.AudioCategories == null || audioLibrary.AudioCategories.Length == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Library Management", EditorStyles.boldLabel);

            foreach (var category in audioLibrary.AudioCategories)
            {
                if (!category) continue;
    
                _foldouts.TryAdd(category.name, true);
    
                if (!_serializedCategories.TryGetValue(category.name, out var serializedCategory))
                {
                    serializedCategory = new SerializedObject(category);
                    _serializedCategories[category.name] = serializedCategory;
                }
    
                serializedCategory.Update();
                
                // --- CATEGORY HEADER ---
                EditorGUILayout.BeginHorizontal();
                _foldouts[category.name] = EditorGUILayout.Foldout(_foldouts[category.name], category.Label, true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                 SerializedProperty resMixer = serializedCategory.FindProperty("audioMixerGroup");
                if (resMixer != null)
                {
                    EditorGUILayout.PropertyField(resMixer, GUIContent.none);
                    GUILayout.Space(5);
                }

                if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    AddNewResourceToCategory(category);
                    _foldouts[category.name] = true; 
                }
                EditorGUILayout.Space(1);
                EditorGUILayout.EndHorizontal();

                
                // --- CATEGORY CONTENT ---
                if (_foldouts[category.name])
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.Space(2);
                    
                    DrawMappingList(serializedCategory);
                    
                    EditorGUILayout.Space(2);
                    serializedCategory.ApplyModifiedProperties();
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.Space(5);
            }
        }

        private void DrawMappingList(SerializedObject serializedCategory)
        {
            SerializedProperty resourcesProp = serializedCategory.FindProperty("audioMappings");

            if (resourcesProp == null || resourcesProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("Category is empty.", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < resourcesProp.arraySize; i++)
            {
                SerializedProperty mapping = resourcesProp.GetArrayElementAtIndex(i);
        
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(mapping, GUIContent.none); 
        
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    resourcesProp.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
        }

        private void AddNewResourceToCategory(SOAudioCategory category)
        {
            if (!_serializedCategories.TryGetValue(category.name, out var so)) return;
    
            SerializedProperty prop = so.FindProperty("audioMappings");
            prop.InsertArrayElementAtIndex(prop.arraySize);
    
            SerializedProperty newElem = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newElem.FindPropertyRelative("id").stringValue = "New_ID";
            newElem.FindPropertyRelative("audioObject").objectReferenceValue = null;

            so.ApplyModifiedProperties();
        }
    }
}
#endif