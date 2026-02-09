#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Utilities.AutoGet
{
    static class AutoGetSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/DNExtensions/AutoGet", SettingsScope.Project)
            {
                label = "AutoGet",
                guiHandler = (searchContext) =>
                {
                    var settings = new SerializedObject(AutoGetSettings.Instance);
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUI.BeginChangeCheck();
                    
                    EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                    DrawAutoPopulationSection(settings);
                    DrawInspectorUISection(settings);
                    DrawPerformanceSection(settings);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ApplyModifiedProperties();
                        AutoGetSettings.Instance.Save();
                    }
                    
                    EditorGUILayout.Space(10);
                    DrawToolsSection();
                    
                    EditorGUILayout.Space(10);
                    
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Reset AutoGet Settings",
                            "Reset all AutoGet settings to their default values?",
                            "Reset",
                            "Cancel"))
                        {
                            AutoGetSettings.Instance.ResetToDefaults();
                        }
                    }
                },
                
                keywords = new[] { "AutoGet", "DNExtensions", "Auto", "Get", "Component", "Child" }
            };

            return provider;
        }

        private static void DrawAutoPopulationSection(SerializedObject settings)
        {
            var autoPopulateMode = settings.FindProperty("autoPopulateMode");
            
            if (autoPopulateMode.enumValueIndex == (int)AutoPopulateMode.Default)
            {
                autoPopulateMode.enumValueIndex = (int)AutoPopulateMode.WhenEmpty;
                settings.ApplyModifiedProperties();
            }
            
            EditorGUILayout.PropertyField(autoPopulateMode, new GUIContent("Auto-Populate", "When should fields be automatically populated?"));
            
            if (autoPopulateMode.enumValueIndex != (int)AutoPopulateMode.Never)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Triggers", EditorStyles.miniBoldLabel);
                
                var validateOnSelection = settings.FindProperty("validateOnSelection");
                var validateOnSceneSave = settings.FindProperty("validateOnSceneSave");
                
                EditorGUILayout.PropertyField(validateOnSelection, new GUIContent("On Selection", "Auto-populate when selecting objects"));
                EditorGUILayout.PropertyField(validateOnSceneSave, new GUIContent("On Scene Save", "Auto-populate when saving scenes"));
                
                if (!validateOnSelection.boolValue && !validateOnSceneSave.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "No triggers enabled. Auto-population is effectively disabled.",
                        MessageType.Warning
                    );
                }
                
                EditorGUILayout.Space(3);
                
                var description = GetAutoPopulateModeDescription(autoPopulateMode.enumValueIndex);
                EditorGUILayout.HelpBox(description, MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(5);
            
            var autoPopulateInPrefabs = settings.FindProperty("autoPopulateInPrefabs");
            EditorGUILayout.PropertyField(autoPopulateInPrefabs, new GUIContent("Allow in Prefabs", "Enable AutoGet when editing prefabs (affects both auto and manual)"));
        }

        private static void DrawInspectorUISection(SerializedObject settings)
        {
            var showPopulateButton = settings.FindProperty("showPopulateButton");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(showPopulateButton, new GUIContent("Show Populate Button", "Show 🔄 button next to AutoGet fields"));
            if (EditorGUI.EndChangeCheck())
            {
                settings.ApplyModifiedProperties();
                AutoGetSettings.Instance.Save();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private static void DrawPerformanceSection(SerializedObject settings)
        {
            var cacheReflectionData = settings.FindProperty("cacheReflectionData");
            EditorGUILayout.PropertyField(cacheReflectionData, new GUIContent("Cache Reflection Data", "Cache for better performance (disable if using Hot Reload)"));
            
            if (cacheReflectionData.boolValue)
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

        private static void DrawToolsSection()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate Current Scene"))
            {
                PopulateCurrentScene();
            }
            if (GUILayout.Button("Populate Selected"))
            {
                PopulateSelected();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void PopulateSelected()
        {
            var count = 0;
            foreach (var gameObject in Selection.gameObjects)
            {
                var behaviours = gameObject.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour))
                    {
                        Undo.RecordObject(behaviour, "Populate AutoGet Fields");
                        AutoGetSystem.Process(behaviour);
                        count++;
                    }
                }
            }
            
            Debug.Log($"Populated AutoGet fields on {count} component(s).");
        }

        private static void PopulateCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            var count = 0;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var behaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour))
                    {
                        Undo.RecordObject(behaviour, "Populate AutoGet Fields");
                        AutoGetSystem.Process(behaviour);
                        count++;
                    }
                }
            }
            
            Debug.Log($"Populated AutoGet fields on {count} component(s) in scene '{scene.name}'.");
        }

        private static string GetAutoPopulateModeDescription(int mode)
        {
            return mode switch
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