#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.SerializableSelector.Editor
{
    using TypeInfo = SerializableSelectorUtility.TypeInfo;
    
    [CustomPropertyDrawer(typeof(SerializableSelectorAttribute))]
    public class SerializableSelectorDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, TypeInfo[]> TypeCache = new Dictionary<string, TypeInfo[]>();
        private static object _clipboard;
        private static Type _clipboardType;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "[SerializableSelector] requires [SerializeReference]");
                return;
            }
            
            SerializableSelectorAttribute attr = attribute as SerializableSelectorAttribute;
            
            // Calculate rects
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
            Rect dropdownRect = new Rect(
                position.x + EditorGUIUtility.labelWidth + 2, 
                position.y, 
                position.width - EditorGUIUtility.labelWidth - 2, 
                lineHeight
            );
            
            // Draw label
            EditorGUI.LabelField(labelRect, label);
            
            // Get current type name
            string currentTypeName = GetTypeName(property);
            
            // Draw dropdown button
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(currentTypeName), FocusType.Keyboard))
            {
                ShowTypeMenu(property, attr, dropdownRect);
            }
            
            // Handle right-click context menu
            Event e = Event.current;
            if (e.type == EventType.ContextClick && dropdownRect.Contains(e.mousePosition))
            {
                ShowContextMenu(property);
                e.Use();
            }
            
            // Draw property fields if value is not null
            if (property.managedReferenceValue != null)
            {
                Rect contentRect = new Rect(
                    position.x,
                    position.y + lineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    position.height - lineHeight - EditorGUIUtility.standardVerticalSpacing
                );
                
                EditorGUI.indentLevel++;
                
                // Draw all properties including base class fields
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = iterator.GetEndProperty();
                
                bool enterChildren = true;
                float yOffset = 0;
                
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;
                    
                    float propertyHeight = EditorGUI.GetPropertyHeight(iterator, true);
                    Rect propertyRect = new Rect(
                        contentRect.x,
                        contentRect.y + yOffset,
                        contentRect.width,
                        propertyHeight
                    );
                    
                    EditorGUI.PropertyField(propertyRect, iterator, true);
                    yOffset += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                    
                    enterChildren = false;
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            
            if (property.managedReferenceValue != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = iterator.GetEndProperty();
                
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;
                    
                    height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
            }
            
            return height;
        }
        
        private string GetTypeName(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
                return "<null>";
            
            // Get the actual Type object
            object value = property.managedReferenceValue;
            if (value != null)
            {
                Type actualType = value.GetType();
                // Return custom display name if it exists
                return SerializableSelectorUtility.GetTypeDisplayName(actualType);
            }
            
            // Fallback: parse the typename
            string[] parts = property.managedReferenceFullTypename.Split(' ');
            if (parts.Length == 2)
            {
                string fullTypeName = parts[1];
                string[] typeParts = fullTypeName.Split('.');
                return typeParts[^1];
            }
            
            return property.managedReferenceFullTypename;
        }
        
        private void ShowTypeMenu(SerializedProperty property, SerializableSelectorAttribute attr, Rect buttonRect)
        {
            Type baseType = GetBaseType(property);
            if (baseType == null)
            {
                Debug.LogError("Could not determine base type for SerializableSelector");
                return;
            }
    
            // Get types with caching
            TypeInfo[] types = GetCachedTypes(baseType, attr);
    
            // Get existing types in list for AllowOnce checking
            HashSet<Type> existingTypes = GetExistingTypesInList(property);
    
            // Determine if we should show search field
            bool showSearch = attr.SearchThreshold >= 0 && types.Length >= attr.SearchThreshold;
    
            // Show popup window
            SerializableSelectorPopup.Show(
                buttonRect,
                types,
                attr.AllowNull,
                showSearch,
                attr.ShowCategoryHeaders,
                existingTypes,
                selectedType => SetType(property, selectedType)
            );
        }
        
        
        
        private void ShowContextMenu(SerializedProperty property)
        {
            GenericMenu menu = new GenericMenu();
            
            if (property.managedReferenceValue != null)
            {
                menu.AddItem(new GUIContent("Copy"), false, () => CopyValue(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
            }
            
            if (_clipboard != null && CanPaste(property))
            {
                menu.AddItem(new GUIContent($"Paste ({_clipboardType.Name})"), false, () => PasteValue(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }
            
            menu.ShowAsContext();
        }
        
        private void CopyValue(SerializedProperty property)
        {
            object value = property.managedReferenceValue;
            if (value == null) return;
            
            _clipboardType = value.GetType();
            _clipboard = value.CopyReference();
            
        }
        
        private bool CanPaste(SerializedProperty property)
        {
            if (_clipboard == null) return false;
            
            Type baseType = GetBaseType(property);
            return baseType != null && baseType.IsAssignableFrom(_clipboardType);
        }
        
        private void PasteValue(SerializedProperty property)
        {
            if (_clipboard == null) return;
            
            property.managedReferenceValue = _clipboard.CopyReference();
            property.serializedObject.ApplyModifiedProperties();
            
        }
        
        private bool IsInArray(SerializedProperty property)
        {
            return property.propertyPath.Contains(".Array.data[");
        }
        
        private HashSet<Type> GetExistingTypesInList(SerializedProperty property)
        {
            var existingTypes = new HashSet<Type>();
    
            if (!IsInArray(property))
                return existingTypes;
    
            // Navigate to parent array
            string arrayPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal));
            SerializedProperty arrayProperty = property.serializedObject.FindProperty(arrayPath);
    
            if (arrayProperty == null || !arrayProperty.isArray)
                return existingTypes;
    
            // Collect all types in the array
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
        
                if (element.managedReferenceValue != null)
                {
                    Type elementType = element.managedReferenceValue.GetType();
                    existingTypes.Add(elementType);
                }
            }
    
            return existingTypes;
        }
        
        private void SetType(SerializedProperty property, Type type)
        {
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            property.serializedObject.ApplyModifiedProperties();
        }
        
        private Type GetBaseType(SerializedProperty property)
        {
            Type parentType = property.serializedObject.targetObject.GetType();
            
            string[] pathParts = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            
            foreach (string part in pathParts)
            {
                if (part.Contains("["))
                {
                    string fieldName = part.Substring(0, part.IndexOf("[", StringComparison.Ordinal));
                    if (parentType != null)
                    {
                        FieldInfo field = parentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                        if (field == null) return null;
                    
                        Type fieldType = field.FieldType;
                        if (fieldType.IsArray)
                        {
                            parentType = fieldType.GetElementType();
                        }
                        else if (fieldType.IsGenericType)
                        {
                            parentType = fieldType.GetGenericArguments()[0];
                        }
                    }
                }
                else
                {
                    if (parentType != null)
                    {
                        FieldInfo field = parentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field == null) return null;
                        parentType = field.FieldType;
                    }
                }
            }
            
            return parentType;
        }
        
        private TypeInfo[] GetCachedTypes(Type baseType, SerializableSelectorAttribute attr)
        {
            // Create cache key including all filters
            string interfaceKey = attr.RequireInterfaces != null 
                ? string.Join(",", Array.ConvertAll(attr.RequireInterfaces, t => t.FullName))
                : "";
            
            string cacheKey = $"{baseType.FullName}|{attr.NamespaceFilter ?? ""}|{interfaceKey}";
            
            if (!TypeCache.ContainsKey(cacheKey))
            {
                TypeInfo[] types = SerializableSelectorUtility.GetDerivedTypes(
                    baseType,
                    attr.NamespaceFilter,
                    attr.RequireInterfaces
                );
                TypeCache[cacheKey] = types;
            }
            
            return TypeCache[cacheKey];
        }
    }
}
#endif