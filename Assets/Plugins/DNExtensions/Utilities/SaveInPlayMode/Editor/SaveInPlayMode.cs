using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
        private static readonly Dictionary<string, ComponentSnapshot> SavedData = new Dictionary<string, ComponentSnapshot>();

        /// <summary>
        /// Snapshot of a component's state. Object reference fields are captured separately via
        /// GlobalObjectId rather than relying on EditorJsonUtility, because JSON embeds the raw
        /// instance ID of referenced objects. Instance IDs are only valid for the play mode session
        /// they were captured in - once play mode exits those objects are gone and the IDs get
        /// recycled, so restoring them directly either nulls the reference out or points it at
        /// whatever unrelated object now owns that recycled ID. GlobalObjectId instead encodes a
        /// stable identifier (scene + local file ID for scene objects, asset GUID for assets) that
        /// still resolves correctly after returning to edit mode.
        /// </summary>
        private class ComponentSnapshot {
            public string Json;
            public Dictionary<string, string> ObjectReferences;
        }


        static SaveInPlayModeHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += ClearAllData;
            ComponentHeaderButtonInjector.RegisterDynamicButtonProvider(GetButtonForComponent);
        }
        
        /// <summary>
        /// Button provider function called by ComponentHeaderButtonInjector.
        /// Returns button data for components that should have save buttons, or null otherwise.
        /// </summary>
        private static ComponentHeaderButtonInjector.ButtonData GetButtonForComponent(Component component) { if (!ShouldShowSaveButton(component.GetType())) { return null; }
            
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
                SavedData[key] = CaptureSnapshot(comp);
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
                    SavedData[key] = CaptureSnapshot(comp);
                }
            }
        }

        /// <summary>
        /// Captures a component's state as JSON plus a separate table of object reference fields
        /// keyed by property path, using GlobalObjectId to identify each referenced object.
        /// </summary>
        private static ComponentSnapshot CaptureSnapshot(Component comp) {
            var snapshot = new ComponentSnapshot {
                Json = EditorJsonUtility.ToJson(comp, false),
                ObjectReferences = new Dictionary<string, string>()
            };

            var serializedObject = new SerializedObject(comp);
            var property = serializedObject.GetIterator();
            while (property.NextVisible(true)) {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;

                var value = property.objectReferenceValue;
                snapshot.ObjectReferences[property.propertyPath] = value != null
                    ? GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()
                    : string.Empty;
            }

            return snapshot;
        }

        private static void RestoreSavedComponents() {
            foreach (var kvp in SavedData) {
                Component comp = GetComponentFromKey(kvp.Key);
                if (comp != null) {
                    ApplySnapshot(comp, kvp.Value);
                }
            }

            MarkedForSave.Clear();
            SavedData.Clear();
        }

        /// <summary>
        /// Restores a captured snapshot onto a component. Non-reference fields are restored via
        /// JSON overwrite; object reference fields are then resolved from their GlobalObjectId and
        /// applied explicitly, overriding whatever (potentially stale) reference JSON produced.
        /// </summary>
        private static void ApplySnapshot(Component comp, ComponentSnapshot snapshot) {
            Undo.RecordObject(comp, "Restore Play Mode Changes");
            EditorJsonUtility.FromJsonOverwrite(snapshot.Json, comp);

            if (snapshot.ObjectReferences.Count > 0) {
                var serializedObject = new SerializedObject(comp);
                bool changed = false;

                foreach (var kvp in snapshot.ObjectReferences) {
                    var property = serializedObject.FindProperty(kvp.Key);
                    if (property == null) continue;

                    Object resolved = null;
                    if (!string.IsNullOrEmpty(kvp.Value) && GlobalObjectId.TryParse(kvp.Value, out var id)) {
                        resolved = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                    }

                    if (property.objectReferenceValue != resolved) {
                        property.objectReferenceValue = resolved;
                        changed = true;
                    }
                }

                if (changed) {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(comp);
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

