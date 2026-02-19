#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(SOSelectorAttribute))]
    public class SOSelectorDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, AssetInfo<ScriptableObject>[]> SOCache = new Dictionary<string, AssetInfo<ScriptableObject>[]>();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "[SOSelector] requires Object reference field");
                return;
            }
    
            SOSelectorAttribute attr = attribute as SOSelectorAttribute;
    
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
                    ScriptableObject so = newValue as ScriptableObject;
                    
                    if (so)
                    {
                        if (attr is { LockToFilter: true } && !IsValidSO(so, attr))
                        {
                            Debug.LogWarning($"ScriptableObject '{so.name}' does not match the folder/filter criteria for this field.");
                        }
                        else
                        {
                            property.objectReferenceValue = newValue;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Only ScriptableObject assets can be assigned to fields with [SOSelector]");
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
                ShowSOMenu(property, attr, combinedRect);
            }
        }

        private void ShowContextMenu(SerializedProperty property, SOSelectorAttribute attr)
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
        
        private void ShowSOMenu(SerializedProperty property, SOSelectorAttribute attr, Rect fieldRect)
        {
            AssetInfo<ScriptableObject>[] scriptableObjects = GetScriptableObjects(property, attr);
            bool showSearch = scriptableObjects.Length >= attr.SearchThreshold;
            
            SOSelectorPopup.Show(
                fieldRect,
                scriptableObjects,
                attr.AllowNull,
                showSearch,
                selectedSO => SetSO(property, selectedSO)
            );
        }
        
        private bool IsValidSO(ScriptableObject so, SOSelectorAttribute attr)
        {
            string soPath = AssetDatabase.GetAssetPath(so);
            
            if (!string.IsNullOrEmpty(attr.FolderPath))
            {
                if (!soPath.StartsWith(attr.FolderPath))
                    return false;
            }
            
            if (attr.TypeFilter != null)
            {
                if (!attr.TypeFilter.IsInstanceOfType(so))
                    return false;
            }
            
            if (attr.InterfaceFilter != null)
            {
                if (!attr.InterfaceFilter.IsInstanceOfType(so))
                    return false;
            }
            
            if (!string.IsNullOrEmpty(attr.SearchFilter))
            {
                if (!so.name.ToLower().Contains(attr.SearchFilter.ToLower()))
                    return false;
            }
            
            return true;
        }
        
        private AssetInfo<ScriptableObject>[] GetScriptableObjects(SerializedProperty property, SOSelectorAttribute attr)
        {
            Type fieldType = fieldInfo.FieldType;
            Type searchType = attr.TypeFilter ?? fieldType;
            
            string cacheKey = $"{searchType.FullName}|{attr.InterfaceFilter?.FullName ?? ""}|{attr.FolderPath ?? ""}|{attr.SearchFilter ?? ""}";
            
            if (SOCache.TryGetValue(cacheKey, out var scriptableObjects))
                return scriptableObjects;
            
            List<AssetInfo<ScriptableObject>> soList = new List<AssetInfo<ScriptableObject>>();
            
            string searchQuery = $"t:{searchType.Name}";
            if (!string.IsNullOrEmpty(attr.SearchFilter))
                searchQuery += " " + attr.SearchFilter;
            
            var guids = !string.IsNullOrEmpty(attr.FolderPath) 
                ? AssetDatabase.FindAssets(searchQuery, new[] { attr.FolderPath }) 
                : AssetDatabase.FindAssets(searchQuery);
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                
                if (!so)
                    continue;
                
                if (!fieldType.IsInstanceOfType(so))
                    continue;
                
                if (attr.TypeFilter != null && !attr.TypeFilter.IsInstanceOfType(so))
                    continue;
                
                if (attr.InterfaceFilter != null && !attr.InterfaceFilter.IsInstanceOfType(so))
                    continue;
                
                soList.Add(new AssetInfo<ScriptableObject>
                {
                    Asset = so,
                    DisplayName = so.name,
                    Path = path
                });
            }
            
            var sortedSOs = soList.OrderBy(s => s.DisplayName).ToArray();
            SOCache[cacheKey] = sortedSOs;
            
            return sortedSOs;
        }
        
        private void SetSO(SerializedProperty property, ScriptableObject so)
        {
            property.objectReferenceValue = so;
            property.serializedObject.ApplyModifiedProperties();
        }

        public static void ClearCache()
        {
            SOCache.Clear();
        }
    }
}
#endif