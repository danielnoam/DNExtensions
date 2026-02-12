using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities {
    
    /// <summary>
    /// Settings for SaveInPlayMode functionality.
    /// Controls whether the feature is enabled and manages component type blacklist.
    /// </summary>
    public class SaveInPlayModeSettings : ScriptableObject {
        private const string SettingsPath = "ProjectSettings/DNExtensions_SaveInPlayModeSettings.asset";

        [SerializeField] private bool enabled = true;
        [SerializeField] private List<string> blacklistedTypeNames = new List<string>();

        /// <summary>
        /// Whether SaveInPlayMode is enabled.
        /// </summary>
        public bool Enabled => enabled;

        private static SaveInPlayModeSettings _instance;

        internal static SaveInPlayModeSettings Instance {
            get {
                if (!_instance) {
                    _instance = LoadOrCreate();
                }
                return _instance;
            }
        }

        private static SaveInPlayModeSettings LoadOrCreate() {
            if (File.Exists(SettingsPath)) {
                var loaded = InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
                if (loaded is { Length: > 0 }) {
                    return loaded[0] as SaveInPlayModeSettings;
                }
            }
    
            var settings = CreateInstance<SaveInPlayModeSettings>();
            settings.InitializeDefaults();
            settings.Save();
            return settings;
        }

        private void InitializeDefaults() {
            blacklistedTypeNames = new List<string> {
                "UnityEngine.Canvas",
                "UnityEngine.CanvasRenderer",
                "UnityEngine.Animator",
                "UnityEngine.UI.CanvasScaler",
                "DNExtensions.VFXManager.VFXManager",
            };
        }

        internal void Save() {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, SettingsPath, true);
        }

        /// <summary>
        /// Checks if a type is blacklisted.
        /// </summary>
        public bool IsBlacklisted(Type type) {
            return blacklistedTypeNames.Contains(type.FullName);
        }

        /// <summary>
        /// Adds a type to the blacklist.
        /// </summary>
        public void AddToBlacklist(Type type) {
            string fullName = type.FullName;
            if (!blacklistedTypeNames.Contains(fullName)) {
                blacklistedTypeNames.Add(fullName);
                Save();
            }
        }

        /// <summary>
        /// Removes a type from the blacklist.
        /// </summary>
        public void RemoveFromBlacklist(Type type) {
            if (blacklistedTypeNames.Remove(type.FullName)) {
                Save();
            }
        }

        public void ResetToDefaults() {
            enabled = true;
            InitializeDefaults();
            Save();
        }
    }

    static class SaveInPlayModeSettingsProvider {
        private const string MenuPath = "Tools/DNExtensions/Save In Play Mode Settings";
        private const int MenuPriority = 1001;

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void OpenSettings() {
            SettingsService.OpenProjectSettings("Project/DNExtensions/SaveInPlayMode");
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider() {
            var provider = new SettingsProvider("Project/DNExtensions/SaveInPlayMode", SettingsScope.Project) {
                label = "Save In Play Mode",
                guiHandler = (searchContext) => {
                    var settings = new SerializedObject(SaveInPlayModeSettings.Instance);
                    
                    EditorGUILayout.Space(10);
                    EditorGUILayout.HelpBox(
                        "SaveInPlayMode adds a save button to component headers during play mode. " +
                        "Click the button to mark components for restoration when exiting play mode.",
                        MessageType.Info
                    );
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUI.BeginChangeCheck();
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("enabled"), new GUIContent("Enabled", "Enable or disable SaveInPlayMode feature"));
                    var blacklistProp = settings.FindProperty("blacklistedTypeNames");
                    EditorGUILayout.PropertyField(blacklistProp, new GUIContent("Blacklist", "Component types that won't show save buttons"), true);
                    
                    if (EditorGUI.EndChangeCheck()) {
                        settings.ApplyModifiedProperties();
                        SaveInPlayModeSettings.Instance.Save();
                    }
                    
                    EditorGUILayout.Space(10);
                    
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30))) {
                        if (EditorUtility.DisplayDialog(
                            "Reset SaveInPlayMode Settings",
                            "Reset all settings to their default values?",
                            "Reset",
                            "Cancel")) {
                            SaveInPlayModeSettings.Instance.ResetToDefaults();
                        }
                    }
                },
                
                keywords = new[] { "Save", "Play Mode", "DNExtensions", "Components" }
            };

            return provider;
        }
    }
}