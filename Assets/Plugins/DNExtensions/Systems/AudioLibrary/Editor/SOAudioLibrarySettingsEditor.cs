
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomEditor(typeof(SOAudioLibrarySettings))]
    public class SOAudioLibrarySettingsEditor : Editor
    {
        private const string SettingsPath = "Assets/Settings/Resources/AudioLibrarySettings.asset";
        
        private readonly Dictionary<string, bool> _foldouts = new();
        private readonly Dictionary<string, SerializedObject> _serializedCategories = new();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var audioLibrary = (SOAudioLibrarySettings)target;

            if (audioLibrary.AudioCategories == null || audioLibrary.AudioCategories.Length == 0) return;

            EditorGUILayout.Space(10);

            foreach (var category in audioLibrary.AudioCategories)
            {
                if (!category) continue;
    
                _foldouts.TryAdd(category.name, true);
    
                if (!_serializedCategories.TryGetValue(category.name, out var serializedCategory))
                {
                    serializedCategory = new SerializedObject(category);
                    _serializedCategories[category.name] = serializedCategory;
                }
    
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                serializedCategory.Update();
                GUILayout.Space(5);

                
                // Header
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
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
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(3);
                
                // Content
                if (_foldouts[category.name])
                {
                    Rect lineRect = EditorGUILayout.GetControlRect(false, 3);
                    EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    
                    EditorGUILayout.Space(2);
                    DrawMappingList(serializedCategory);
                    EditorGUILayout.Space(2);
                    serializedCategory.ApplyModifiedProperties();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            if (GUILayout.Button("+ New Category"))
            {
                AddNewCategory(audioLibrary);
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
        
        
        private void AddNewCategory(SOAudioLibrarySettings audioLibrary)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Audio Category",
                "NewAudioCategory",
                "asset",
                "Choose where to save the new category",
                "Assets/"
            );

            if (string.IsNullOrEmpty(path)) return;

            var newCategory = CreateInstance<SOAudioCategory>();
            AssetDatabase.CreateAsset(newCategory, path);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(audioLibrary);
            SerializedProperty categoriesProp = so.FindProperty("audioCategories"); // match your field name
            categoriesProp.arraySize++;
            categoriesProp.GetArrayElementAtIndex(categoriesProp.arraySize - 1).objectReferenceValue = newCategory;
            so.ApplyModifiedProperties();

            AssetDatabase.Refresh();
        }
        
        
        [MenuItem("Tools/DNExtensions/Audio Library Settings")]
        private static void OpenSettings()
        {
            var settings = SOAudioLibrarySettings.Instance;
            
            if (!settings)
            {
                string[] guids = AssetDatabase.FindAssets("t:AudioLibrarySettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<SOAudioLibrarySettings>(path);
                }
            }
            
            if (settings)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                // Offer to create it
                if (EditorUtility.DisplayDialog(
                    "Create Audio Library Settings?",
                    "No Audio Library Settings found. Create one now?\n\n" +
                    "It will be created at: " + SettingsPath + "\n\n" +
                    "Note: Must be in a Resources folder for runtime access.",
                    "Create", "Cancel"))
                {
                    CreateSettingsAsset();
                }
            }
        }
        
        private static void CreateSettingsAsset()
        {
            var settings = CreateInstance<SOAudioLibrarySettings>();

            string directory = Path.GetDirectoryName(SettingsPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                if (directory != null)
                {
                    string[] folders = directory.Split('/');
                    string currentPath = folders[0];
                
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string parentPath = currentPath;
                        currentPath = $"{currentPath}/{folders[i]}";
                    
                        if (!AssetDatabase.IsValidFolder(currentPath))
                        {
                            AssetDatabase.CreateFolder(parentPath, folders[i]);
                        }
                    }
                }
            }
            
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            
            Debug.Log($"Created AudioLibrarySettings at: {SettingsPath}");
        }
    }
}
