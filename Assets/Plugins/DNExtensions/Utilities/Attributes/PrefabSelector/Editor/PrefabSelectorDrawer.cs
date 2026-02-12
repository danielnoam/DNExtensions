#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.PrefabSelector
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
    
            Event evt = Event.current;
            if (evt.type == EventType.ContextClick && position.Contains(evt.mousePosition))
            {
                ShowContextMenu(property, attr);
                evt.Use();
                return;
            }
    
            Rect controlRect = EditorGUI.PrefixLabel(position, label);
    
            float buttonWidth = 20f;
            float spacing = 2f;
            Rect objectFieldRect = new Rect(controlRect.x, controlRect.y, 
                controlRect.width - buttonWidth - spacing, controlRect.height);
            Rect dropdownButtonRect = new Rect(objectFieldRect.xMax + spacing, controlRect.y, 
                buttonWidth, controlRect.height);
            
            EditorGUI.BeginChangeCheck();
            Object newValue = EditorGUI.ObjectField(objectFieldRect, property.objectReferenceValue, fieldInfo.FieldType, false);
            if (EditorGUI.EndChangeCheck())
            {
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
                    
                    if (prefab && PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab)
                    {
                        if (attr is { LockToFilter: true } && !IsValidPrefab(prefab, newValue, attr))
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
            
            if (GUI.Button(dropdownButtonRect, "▼", EditorStyles.miniButton))
            {
                Rect combinedRect = new Rect(
                    objectFieldRect.x,
                    objectFieldRect.y,
                    objectFieldRect.width + buttonWidth + 2,
                    objectFieldRect.height
                );
                ShowPrefabMenu(property, attr, combinedRect);
            }
        }

        private void ShowContextMenu(SerializedProperty property, PrefabSelectorAttribute attr)
        {
            GenericMenu menu = new GenericMenu();
            
            if (property.objectReferenceValue)
            {
                menu.AddItem(new GUIContent("Ping Asset"), false, () =>
                {
                    property.objectReferenceValue.Ping();
                });
                
                menu.AddSeparator("");
            }
            
            
            if (!string.IsNullOrEmpty(attr.FolderPath))
            {
                menu.AddItem(new GUIContent("Copy Path"), false, () =>
                {
                    attr.FolderPath.CopyToClipboard();
                    Debug.Log($"Copied filter path: {attr.FolderPath}");
                });
            }
            
            menu.AddItem(new GUIContent("Clear Cache"), false, ClearCache);
            
            menu.ShowAsContext();
        }
        
        private void ShowPrefabMenu(SerializedProperty property, PrefabSelectorAttribute attr, Rect fieldRect)
        {
            PrefabInfo[] prefabs = GetPrefabs(property, attr);
            bool showSearch = prefabs.Length >= attr.SearchThreshold;
            
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
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            
            if (!string.IsNullOrEmpty(attr.FolderPath))
            {
                if (!prefabPath.StartsWith(attr.FolderPath))
                    return false;
            }
            
            if (!string.IsNullOrEmpty(attr.SearchFilter))
            {
                if (!prefab.name.ToLower().Contains(attr.SearchFilter.ToLower()))
                    return false;
            }
            
            return true;
        }
        
        private PrefabInfo[] GetPrefabs(SerializedProperty property, PrefabSelectorAttribute attr)
        {
            Type fieldType = fieldInfo.FieldType;
            string cacheKey = $"{fieldType.FullName}|{attr.FolderPath ?? ""}|{attr.SearchFilter ?? ""}";
            
            if (PrefabCache.TryGetValue(cacheKey, out var prefabs))
                return prefabs;
            
            List<PrefabInfo> prefabList = new List<PrefabInfo>();
            
            string searchQuery = "t:Prefab";
            if (!string.IsNullOrEmpty(attr.SearchFilter))
                searchQuery += " " + attr.SearchFilter;
            
            var guids = !string.IsNullOrEmpty(attr.FolderPath) 
                ? AssetDatabase.FindAssets(searchQuery, new[] { attr.FolderPath }) 
                : AssetDatabase.FindAssets(searchQuery);
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (!prefab)
                    continue;
                
                if (typeof(Component).IsAssignableFrom(fieldType))
                {
                    Component comp = prefab.GetComponent(fieldType);
                    if (!comp)
                        continue;
                }
                
                prefabList.Add(new PrefabInfo
                {
                    Prefab = prefab,
                    DisplayName = prefab.name,
                    Path = path
                });
            }
            
            var sortedPrefabs = prefabList.OrderBy(p => p.DisplayName).ToArray();
            PrefabCache[cacheKey] = sortedPrefabs;
            
            return sortedPrefabs;
        }
        
        private void SetPrefab(SerializedProperty property, GameObject prefab)
        {
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