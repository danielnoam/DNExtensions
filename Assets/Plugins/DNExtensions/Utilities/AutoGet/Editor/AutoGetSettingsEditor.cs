#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet.Editor
{
    
    /// <summary>
    /// Custom editor for AutoGetSettings.
    /// </summary>
    [CustomEditor(typeof(AutoGetSettings))]
    internal class AutoGetSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _autoPopulateMode;
        private SerializedProperty _autoPopulateInPrefabs;
        private SerializedProperty _validateOnSelection;
        private SerializedProperty _validateOnSceneSave;
        private SerializedProperty _showPopulateButton;
        private SerializedProperty _cacheReflectionData;
        
        private void OnEnable()
        {
            _autoPopulateMode = serializedObject.FindProperty("autoPopulateMode");
            _autoPopulateInPrefabs = serializedObject.FindProperty("autoPopulateInPrefabs");
            _validateOnSelection = serializedObject.FindProperty("validateOnSelection");
            _validateOnSceneSave = serializedObject.FindProperty("validateOnSceneSave");
            _showPopulateButton = serializedObject.FindProperty("showPopulateButton");
            _cacheReflectionData = serializedObject.FindProperty("cacheReflectionData");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawAutoPopulationSection();
            DrawValidationSection();
            DrawInspectorUISection();
            DrawPerformanceSection();
            
            EditorGUILayout.Space();
            DrawToolsSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawAutoPopulationSection()
        {
            EditorGUILayout.PropertyField(_autoPopulateMode);
            
            if (_autoPopulateMode.enumValueIndex != (int)AutoPopulateMode.Never)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_autoPopulateInPrefabs);
                var description = GetAutoPopulateModeDescription();
                EditorGUILayout.HelpBox(description, MessageType.Info);
                EditorGUI.indentLevel--;
                

            }
        }
        
        private void DrawValidationSection()
        {
            EditorGUILayout.PropertyField(_validateOnSelection);
            EditorGUILayout.PropertyField(_validateOnSceneSave);
            
            if (!_validateOnSelection.boolValue && !_validateOnSceneSave.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Automatic validation is disabled. Fields will only populate via manual triggers.",
                    MessageType.Warning
                );
            }
        }
        
        private void DrawInspectorUISection()
        {
            EditorGUILayout.PropertyField(_showPopulateButton);
        }
        
        private void DrawPerformanceSection()
        {
            EditorGUILayout.PropertyField(_cacheReflectionData);
            
            if (_cacheReflectionData.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "⚠ Warning: Caching works best without Hot Reload plugins. " +
                    "If using Hot Reload, manually clear cache after adding new AutoGet fields.",
                    MessageType.Warning
                );
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear Cache", GUILayout.Width(120)))
                {
                    AutoGetCache.Clear();
                    Debug.Log("AutoGet reflection cache cleared.");
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawToolsSection()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate Current Scene"))
            {
                AutoGetMenu.PopulateCurrentScene();
            }
            if (GUILayout.Button("Populate Selected"))
            {
                AutoGetMenu.PopulateSelected();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private string GetAutoPopulateModeDescription()
        {
            return _autoPopulateMode.enumValueIndex switch
            {
                (int)AutoPopulateMode.Never => 
                    "Fields will only be populated manually via button or context menu.",
                (int)AutoPopulateMode.WhenEmpty => 
                    "Fields will be populated automatically when null or empty. Existing values are preserved.",
                (int)AutoPopulateMode.Always => 
                    "⚠ Fields will always be repopulated, replacing any manually assigned values.",
                _ => ""
            };
        }
    }
}
#endif