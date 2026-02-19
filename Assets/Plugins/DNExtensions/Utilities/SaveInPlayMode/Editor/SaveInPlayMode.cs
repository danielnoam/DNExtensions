using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR

namespace DNExtensions.Utilities {
    
    /// <summary>
    /// Handles SaveInPlayMode functionality by adding save buttons and managing state preservation.
    /// During play mode, all components (except blacklisted ones) get a save button.
    /// </summary>
    [InitializeOnLoad]
    internal static class SaveInPlayModeHandler {
        
        
        private const string SaveIcon = "💾";
        private static readonly Color StyleBackgroundColor = new Color(0.3f, 0.7f, 0.3f, 0.3f);
        private static readonly HashSet<string> MarkedForSave = new HashSet<string>();
        private static readonly Dictionary<string, string> SavedData = new Dictionary<string, string>();


        static SaveInPlayModeHandler() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += ClearAllData;
            ComponentHeaderButtonInjector.RegisterDynamicButtonProvider(GetButtonForComponent);
        }
        
        /// <summary>
        /// Button provider function called by ComponentHeaderButtonInjector.
        /// Returns button data for components that should have save buttons, or null otherwise.
        /// </summary>
        private static ComponentHeaderButtonInjector.ButtonData GetButtonForComponent(Component component) {
            if (!ShouldShowSaveButton(component.GetType())) {
                return null;
            }
            
            bool isSaved = IsSaved(component);
            
            return new ComponentHeaderButtonInjector.ButtonData {
                Icon = SaveIcon,
                Tooltip = isSaved ? "Disable save on exit" : "Save on play mode exit",
                Priority = -1000,
                Callback = ToggleSave,
                StyleCallback = button => {
                    if (isSaved)
                    {
                        button.style.backgroundColor = StyleBackgroundColor; // Green tint
                    }
                }
            };
        }
        
        /// <summary>
        /// Checks if a component is marked for save.
        /// </summary>
        private static bool IsSaved(Component comp) {
            if (comp == null) return false;
            string key = GetComponentKey(comp);
            return MarkedForSave.Contains(key);
        }
        
        /// <summary>
        /// Checks if a component type should have a save button.
        /// Shows for all components during play mode except blacklisted types.
        /// </summary>
        private static bool ShouldShowSaveButton(Type type) {
            if (!SaveInPlayModeSettings.Instance.Enabled) return false;
            if (!EditorApplication.isPlaying) return false;
            return !SaveInPlayModeSettings.Instance.IsBlacklisted(type);
        }
        
        /// <summary>
        /// Called by the save button to toggle save state.
        /// </summary>
        private static void ToggleSave(Component comp) {
            if (comp == null) return;
            
            string key = GetComponentKey(comp);
            
            if (!MarkedForSave.Add(key)) {
                MarkedForSave.Remove(key);
                SavedData.Remove(key);
            }
            else {
                string json = EditorJsonUtility.ToJson(comp, false);
                SavedData[key] = json;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            switch (state) {
                case PlayModeStateChange.ExitingPlayMode:
                    SaveMarkedComponents();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    MarkedForSave.Clear();
                    SavedData.Clear();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall += RestoreSavedComponents;
                    break;
            }
        }

        private static void SaveMarkedComponents() {
            foreach (string key in MarkedForSave) {
                Component comp = GetComponentFromKey(key);
                if (comp != null) {
                    string json = EditorJsonUtility.ToJson(comp, false);
                    SavedData[key] = json;
                }
            }
        }

        private static void RestoreSavedComponents() {
            foreach (var kvp in SavedData) {
                Component comp = GetComponentFromKey(kvp.Key);
                if (comp != null) {
                    Undo.RecordObject(comp, "Restore Play Mode Changes");
                    EditorJsonUtility.FromJsonOverwrite(kvp.Value, comp);
                    EditorUtility.SetDirty(comp);
                }
            }
            
            MarkedForSave.Clear();
            SavedData.Clear();
        }

        private static string GetGameObjectPath(GameObject go) {
            string path = go.name;
            Transform parent = go.transform.parent;
            
            while (parent != null) {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        private static string GetComponentKey(Component comp) {
            string sceneName = comp.gameObject.scene.name;
            string path = GetGameObjectPath(comp.gameObject);
            string typeName = comp.GetType().FullName;
            return $"{sceneName}::{path}::{typeName}";
        }

        private static Component GetComponentFromKey(string key) {
            string[] parts = key.Split(new[] { "::" }, 3, StringSplitOptions.None);
            if (parts.Length != 3) return null;

            string sceneName = parts[0];
            string path = parts[1];
            string typeName = parts[2];

            GameObject go = GameObject.Find(path);
            if (go == null || go.scene.name != sceneName) return null;

            Type type = Type.GetType(typeName);
            if (type == null) {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    type = assembly.GetType(typeName);
                    if (type != null) break;
                }
            }

            if (type == null) return null;

            return go.GetComponent(type);
        }

        private static void ClearAllData() {
            MarkedForSave.Clear();
            SavedData.Clear();
        }
    }
}

#endif