// #if UNITY_EDITOR
//
// using System;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// namespace DNExtensions.Utilities.SaveInPlayMode
// {
//     
//     /// <summary>
//     /// Automatically injects save buttons (💾/📌) into component headers during play mode.
//     /// Toggling a component's save button marks it for preservation — its serialized state
//     /// is captured and restored when exiting play mode, surviving the normal play mode reset.
//     /// </summary>
//     [InitializeOnLoad]
//     public static class SaveInPlayMode {
//         
//         private const string SaveIcon = "💾";
//         private const string SavedIcon = "📌";
//         private const int SaveButtonPriority = -1000;
//         
//         private static readonly HashSet<string> MarkedForSave = new HashSet<string>();
//         private static readonly Dictionary<string, string> SavedData = new Dictionary<string, string>();
//         
//         /// <summary>
//         /// Tracks which component instance IDs have had save buttons injected,
//         /// so we can clean them all up on play mode transitions regardless of selection.
//         /// </summary>
//         private static readonly HashSet<int> InjectedInstanceIds = new HashSet<int>();
//         
//         static SaveInPlayMode() {
//             EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
//             EditorApplication.quitting += ClearAllData;
//             EditorApplication.update += Update;
//         }
//         
//         private static void Update() {
//             if (EditorApplication.isPlaying) {
//                 InjectSaveButtons();
//             }
//         }
//         
//         private static void InjectSaveButtons() {
//             if (Selection.activeGameObject == null) return;
//
//             Component[] components = Selection.activeGameObject.GetComponents<Component>();
//
//             foreach (Component comp in components) {
//                 if (comp == null) continue;
//
//                 string key = GetComponentKey(comp);
//                 bool isSaved = MarkedForSave.Contains(key);
//                 string icon = isSaved ? SavedIcon : SaveIcon;
//                 string tooltip = isSaved ? "Disable save on exit" : "Save on play mode exit";
//
//                 ComponentHeaderButtonInjector.RegisterButton(comp, icon, tooltip, SaveButtonPriority, () => ToggleSave(comp), out _);
//                 InjectedInstanceIds.Add(comp.GetInstanceID());
//             }
//         }
//         
//         private static void OnPlayModeStateChanged(PlayModeStateChange state) {
//             switch (state) {
//                 case PlayModeStateChange.ExitingPlayMode:
//                     // Capture the latest state of all marked components right before exit.
//                     SaveMarkedComponents();
//                     // Remove save buttons from the cache. The injector will handle UI cleanup
//                     // on its next cycle (wrappers become stale when inspector rebuilds on mode change).
//                     RemoveSaveButtonsFromCache();
//                     break;
//                 case PlayModeStateChange.ExitingEditMode:
//                     RemoveSaveButtonsFromCache();
//                     MarkedForSave.Clear();
//                     SavedData.Clear();
//                     break;
//                 case PlayModeStateChange.EnteredEditMode:
//                     EditorApplication.delayCall += RestoreSavedComponents;
//                     break;
//             }
//         }
//         
//         /// <summary>
//         /// Removes save buttons from the injector's cache for all tracked components.
//         /// Does not touch the UI directly — the injector detects the count mismatch
//         /// and rebuilds (or removes the wrapper if no buttons remain).
//         /// </summary>
//         private static void RemoveSaveButtonsFromCache() {
//             foreach (int instanceId in InjectedInstanceIds) {
//                 ComponentHeaderButtonInjector.RemoveButton(instanceId, SaveButtonPriority);
//             }
//             
//             InjectedInstanceIds.Clear();
//         }
//         
//         /// <summary>
//         /// Captures the current serialized state of all components marked for save.
//         /// Called right before exiting play mode to ensure the latest values are preserved.
//         /// </summary>
//         private static void SaveMarkedComponents() {
//             foreach (string key in MarkedForSave) {
//                 Component comp = GetComponentFromKey(key);
//                 if (comp != null) {
//                     string json = EditorJsonUtility.ToJson(comp, false);
//                     SavedData[key] = json;
//                 }
//             }
//         }
//         
//         /// <summary>
//         /// Restores saved component data after returning to edit mode.
//         /// Uses Undo.RecordObject so the restore is undoable.
//         /// </summary>
//         private static void RestoreSavedComponents() {
//             foreach (var kvp in SavedData) {
//                 string key = kvp.Key;
//                 string json = kvp.Value;
//                 
//                 Component comp = GetComponentFromKey(key);
//                 if (comp != null) {
//                     Undo.RecordObject(comp, "Restore Play Mode Changes");
//                     EditorJsonUtility.FromJsonOverwrite(json, comp);
//                     EditorUtility.SetDirty(comp);
//                 }
//             }
//             
//             MarkedForSave.Clear();
//             SavedData.Clear();
//         }
//         
//         private static void ToggleSave(Component comp) {
//             if (comp == null) return;
//             
//             string key = GetComponentKey(comp);
//             
//             if (!MarkedForSave.Add(key)) {
//                 MarkedForSave.Remove(key);
//                 SavedData.Remove(key);
//             }
//             else {
//                 string json = EditorJsonUtility.ToJson(comp, false);
//                 SavedData[key] = json;
//             }
//         }
//         
//         /// <summary>
//         /// Generates a unique key for a component based on its scene, hierarchy path, and type.
//         /// Used to re-locate the component after a play mode transition.
//         /// </summary>
//         private static string GetComponentKey(Component comp) {
//             string sceneName = comp.gameObject.scene.name;
//             string path = GetGameObjectPath(comp.gameObject);
//             string typeName = comp.GetType().FullName;
//             return $"{sceneName}_{path}_{typeName}";
//         }
//         
//         private static string GetGameObjectPath(GameObject go) {
//             string path = go.name;
//             Transform parent = go.transform.parent;
//             
//             while (parent != null) {
//                 path = parent.name + "/" + path;
//                 parent = parent.parent;
//             }
//             
//             return path;
//         }
//         
//         private static Component GetComponentFromKey(string key) {
//             int lastUnderscoreIndex = key.LastIndexOf('_');
//             if (lastUnderscoreIndex == -1) return null;
//             
//             string typeName = key.Substring(lastUnderscoreIndex + 1);
//             string remainder = key.Substring(0, lastUnderscoreIndex);
//             
//             int secondLastUnderscoreIndex = remainder.LastIndexOf('_');
//             if (secondLastUnderscoreIndex == -1) return null;
//             
//             string sceneName = remainder.Substring(0, secondLastUnderscoreIndex);
//             string path = remainder.Substring(secondLastUnderscoreIndex + 1);
//             
//             GameObject go = GameObject.Find(path);
//             if (go == null || go.scene.name != sceneName) return null;
//             
//             Type type = Type.GetType(typeName);
//             if (type == null) {
//                 foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
//                     type = assembly.GetType(typeName);
//                     if (type != null) break;
//                 }
//             }
//             
//             if (type == null) return null;
//             
//             return go.GetComponent(type);
//         }
//         
//         private static void ClearAllData() {
//             MarkedForSave.Clear();
//             SavedData.Clear();
//             InjectedInstanceIds.Clear();
//         }
//     }
// }
//
// #endif