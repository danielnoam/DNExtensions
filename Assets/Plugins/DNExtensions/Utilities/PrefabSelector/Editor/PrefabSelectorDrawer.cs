#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.PrefabSelector.Editor
{
    public struct PrefabInfo
    {
        public GameObject Prefab;
        public string DisplayName;
        public string Path;
    }
    
    [CustomPropertyDrawer(typeof(PrefabSelectorAttribute))]
    public class PrefabSelectorDrawer : PropertyDrawer
    {
        
        private static readonly Dictionary<string, PrefabInfo[]> PrefabCache = new Dictionary<string, PrefabInfo[]>();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "[PrefabSelector] requires Object reference field");
                return;
            }
    
            PrefabSelectorAttribute attr = attribute as PrefabSelectorAttribute;
    
            Rect controlRect = EditorGUI.PrefixLabel(position, label);
    
            float buttonWidth = 20f;
            float spacing = 2f;
            Rect objectFieldRect = new Rect(controlRect.x, controlRect.y, 
                controlRect.width - buttonWidth - spacing, controlRect.height);
            Rect dropdownButtonRect = new Rect(objectFieldRect.xMax + spacing, controlRect.y, 
                buttonWidth, controlRect.height);
            
            // Draw object field (supports drag-drop)
            EditorGUI.BeginChangeCheck();
            Object newValue = EditorGUI.ObjectField(objectFieldRect, property.objectReferenceValue, fieldInfo.FieldType, false);
            if (EditorGUI.EndChangeCheck())
            {
                // Validate that it's a prefab if a value was set
                if (newValue)
                {
                    GameObject prefab = null;
                    
                    if (newValue is GameObject go)
                    {
                        prefab = go;
                    }
                    else if (newValue is Component comp)
                    {
                        prefab = comp.gameObject;
                    }
                    
                    // Check if it's actually a prefab
                    if (prefab && PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab)
                    {
                        // If LockDragDrop is enabled, validate against filter
                        if (attr is { LockDragDrop: true } && !IsValidPrefab(prefab, newValue, attr))
                        {
                            Debug.LogWarning($"Prefab '{prefab.name}' does not match the folder/filter criteria for this field.");
                        }
                        else
                        {
                            property.objectReferenceValue = newValue;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Only prefab assets can be assigned to fields with [PrefabSelector]");
                    }
                }
                else
                {
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            
            // Draw dropdown button
            if (GUI.Button(dropdownButtonRect, "▼", EditorStyles.miniButton))
            {
                // Create combined rect for popup (objectField + button)
                Rect combinedRect = new Rect(
                    objectFieldRect.x,
                    objectFieldRect.y,
                    objectFieldRect.width + buttonWidth + 2,
                    objectFieldRect.height
                );
                ShowPrefabMenu(property, attr, combinedRect);
            }
        }
        
        private void ShowPrefabMenu(SerializedProperty property, PrefabSelectorAttribute attr, Rect fieldRect)
        {
            // Get or cache prefabs
            PrefabInfo[] prefabs = GetPrefabs(property, attr);
            
            // Determine if we should show search
            bool showSearch = prefabs.Length >= attr.SearchThreshold;
            
            // Show popup under the full field (object field + button)
            PrefabSelectorPopup.Show(
                fieldRect,
                prefabs,
                attr.AllowNull,
                showSearch,
                selectedPrefab => SetPrefab(property, selectedPrefab)
            );
        }
        
        private bool IsValidPrefab(GameObject prefab, Object value, PrefabSelectorAttribute attr)
        {
            // Get the path of the prefab
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            
            // Check folder filter
            if (!string.IsNullOrEmpty(attr.FolderPath))
            {
                if (!prefabPath.StartsWith(attr.FolderPath))
                    return false;
            }
            
            // Check search filter (basic name matching)
            if (!string.IsNullOrEmpty(attr.SearchFilter))
            {
                if (!prefab.name.ToLower().Contains(attr.SearchFilter.ToLower()))
                    return false;
            }
            
            // Component type is already validated by Unity's ObjectField type constraint
            // So we don't need to check that here
            
            return true;
        }
        
        private PrefabInfo[] GetPrefabs(SerializedProperty property, PrefabSelectorAttribute attr)
        {
            // Create cache key
            Type fieldType = fieldInfo.FieldType;
            string cacheKey = $"{fieldType.FullName}|{attr.FolderPath ?? ""}|{attr.SearchFilter ?? ""}";
            
            if (PrefabCache.TryGetValue(cacheKey, out var prefabs))
                return prefabs;
            
            // Find all prefabs
            List<PrefabInfo> prefabList = new List<PrefabInfo>();
            
            // Build search filter
            string searchQuery = "t:Prefab";
            if (!string.IsNullOrEmpty(attr.SearchFilter))
                searchQuery += " " + attr.SearchFilter;
            
            // Search for prefabs
            var guids = !string.IsNullOrEmpty(attr.FolderPath) 
                ? AssetDatabase.FindAssets(searchQuery, new[] { attr.FolderPath }) 
                : AssetDatabase.FindAssets(searchQuery);
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (!prefab)
                    continue;
                
                // If field is a Component type, only include prefabs that have that component
                if (typeof(Component).IsAssignableFrom(fieldType))
                {
                    Component comp = prefab.GetComponent(fieldType);
                    if (!comp)
                        continue;
                }
                // If field is GameObject, no filtering needed
                
                prefabList.Add(new PrefabInfo
                {
                    Prefab = prefab,
                    DisplayName = prefab.name,
                    Path = path
                });
            }
            
            // Sort by name
            var sortedPrefabs = prefabList.OrderBy(p => p.DisplayName).ToArray();
            PrefabCache[cacheKey] = sortedPrefabs;
            
            return sortedPrefabs;
        }
        
        private void SetPrefab(SerializedProperty property, GameObject prefab)
        {
            // If field type is Component, get the component from the prefab
            Type fieldType = fieldInfo.FieldType;
            
            if (!prefab)
            {
                property.objectReferenceValue = null;
            }
            else if (typeof(Component).IsAssignableFrom(fieldType))
            {
                Component comp = prefab.GetComponent(fieldType);
                property.objectReferenceValue = comp;
            }
            else
            {
                property.objectReferenceValue = prefab;
            }
            
            property.serializedObject.ApplyModifiedProperties();
        }
        

        public static void ClearCache()
        {
            PrefabCache.Clear();
        }
    }
}
#endif