
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Custom editor for SOAudioLibrarySettings that provides a unified interface for managing
    /// audio categories and their mappings with foldout sections and creation tools.
    /// </summary>
    [CustomEditor(typeof(SOAudioLibrarySettings))]
    public class SOAudioLibrarySettingsEditor : Editor
    {
        private const string SettingsPath = "Assets/Resources/AudioLibrarySettings.asset";

        private readonly Dictionary<string, bool> _foldouts = new();
        private readonly Dictionary<string, SerializedObject> _serializedCategories = new();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var audioLibrary = (SOAudioLibrarySettings)target;

            if (audioLibrary.AudioCategories != null && audioLibrary.AudioCategories.Length != 0)
            {
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
                    _foldouts[category.name] = EditorGUILayout.Foldout(_foldouts[category.name], category.label, true, EditorStyles.foldoutHeader);
                    GUILayout.FlexibleSpace();
                    SerializedProperty resMixer = serializedCategory.FindProperty("audioMixerGroup");
                    if (resMixer != null)
                    {
                        EditorGUILayout.PropertyField(resMixer, GUIContent.none);
                        GUILayout.Space(5);
                    }



                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(3);

                    // Content
                    if (_foldouts[category.name])
                    {
                        Rect lineRect = EditorGUILayout.GetControlRect(false, 3);
                        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

                        EditorGUILayout.Space(5);
                        DrawMappingList(serializedCategory);

                        if (GUILayout.Button("+ New Mapping"))
                        {
                            AddNewMappingToCategory(category);
                        }
                        EditorGUILayout.Space(2);
                        serializedCategory.ApplyModifiedProperties();
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(5);
                }
            }
            
            if (GUILayout.Button("+ New Category", GUILayout.Height(30)))
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

        private void AddNewMappingToCategory(SOAudioCategory category)
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
            newCategory.label = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(audioLibrary);
            SerializedProperty categoriesProp = so.FindProperty("audioCategories");
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
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            Debug.Log("Created Audio Library Settings at " + SettingsPath);
        }
    }
}