#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.SaveInPlayMode
{
    
    [InitializeOnLoad]
    public static class SaveInPlayMode 
    {
        
        private const string SaveIcon = "💾";
        private const string SavedIcon = "📌";
        
        private static readonly HashSet<string> MarkedForSave = new HashSet<string>();
        private static readonly Dictionary<string, string> SavedData = new Dictionary<string, string>();
        
        static SaveInPlayMode() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += ClearAllData;
            EditorApplication.update += InjectSaveButtons;
        }
        
        private static void InjectSaveButtons() {
            if (!EditorApplication.isPlaying) return;
            if (Selection.activeGameObject == null) return;
            
            Component[] components = Selection.activeGameObject.GetComponents<Component>();
            
            foreach (Component comp in components) {
                if (comp == null) continue;
                
                string key = GetComponentKey(comp);
                bool isSaved = MarkedForSave.Contains(key);
                string icon = isSaved ? SavedIcon : SaveIcon;
                string tooltip = isSaved ? "Click to disable save on play mode exit" : "Click to save on play mode exit";
                
                ComponentHeaderButtonInjector.RegisterButton(comp, icon, tooltip, -1000, () => ToggleSave(comp));
            }
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                SaveMarkedComponents();
            }
            else if (state == PlayModeStateChange.EnteredEditMode) {
                RestoreSavedComponents();
            }
        }
        
        private static void SaveMarkedComponents() {
            foreach (string key in MarkedForSave) {
                Component comp = GetComponentFromKey(key);
                if (comp != null) {
                    string json = JsonUtility.ToJson(comp);
                    SavedData[key] = json;
                }
            }
        }
        
        private static void RestoreSavedComponents() {
            foreach (var kvp in SavedData) {
                string key = kvp.Key;
                string json = kvp.Value;
                
                Component comp = GetComponentFromKey(key);
                if (comp != null) {
                    JsonUtility.FromJsonOverwrite(json, comp);
                    EditorUtility.SetDirty(comp);
                }
            }
            
            MarkedForSave.Clear();
            SavedData.Clear();
        }
        
        private static void ToggleSave(Component comp) {
            string key = GetComponentKey(comp);
            
            if (MarkedForSave.Contains(key)) {
                MarkedForSave.Remove(key);
                SavedData.Remove(key);
            }
            else {
                MarkedForSave.Add(key);
                string json = JsonUtility.ToJson(comp);
                SavedData[key] = json;
            }
            
            ComponentHeaderButtonInjector.ClearButtonsForComponent(comp.GetInstanceID());
        }
        
        private static string GetComponentKey(Component comp) {
            string sceneName = comp.gameObject.scene.name;
            string path = GetGameObjectPath(comp.gameObject);
            string typeName = comp.GetType().FullName;
            return $"{sceneName}_{path}_{typeName}";
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
        
        private static Component GetComponentFromKey(string key) {
            int lastUnderscoreIndex = key.LastIndexOf('_');
            if (lastUnderscoreIndex == -1) return null;
            
            string typeName = key.Substring(lastUnderscoreIndex + 1);
            string remainder = key.Substring(0, lastUnderscoreIndex);
            
            int secondLastUnderscoreIndex = remainder.LastIndexOf('_');
            if (secondLastUnderscoreIndex == -1) return null;
            
            string sceneName = remainder.Substring(0, secondLastUnderscoreIndex);
            string path = remainder.Substring(secondLastUnderscoreIndex + 1);
            
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