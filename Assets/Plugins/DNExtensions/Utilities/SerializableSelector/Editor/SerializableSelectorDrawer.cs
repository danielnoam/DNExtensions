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
        private static List<object> _listClipboard; 
        
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
            bool inArray = IsInArray(property);
            
            Rect dropdownRect;
            
            if (inArray)
            {
                dropdownRect = new Rect(position.x, position.y, position.width, lineHeight);
            }
            else
            {
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
                EditorGUI.LabelField(labelRect, label);
                
                dropdownRect = new Rect(
                    position.x + EditorGUIUtility.labelWidth + 2, 
                    position.y, 
                    position.width - EditorGUIUtility.labelWidth - 2, 
                    lineHeight
                );
            }
            
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
                    
                    int oldIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    
                    EditorGUI.PropertyField(propertyRect, iterator, true);
                    
                    EditorGUI.indentLevel = oldIndent;
                    
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
    
            object value = property.managedReferenceValue;
            if (value != null)
            {
                Type actualType = value.GetType();
                return SerializableSelectorUtility.GetTypeDisplayName(actualType);
            }
    
            // Check if type still exists
            if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                string[] parts = property.managedReferenceFullTypename.Split(' ');
                if (parts.Length == 2)
                {
                    string assemblyName = parts[0];
                    string fullTypeName = parts[1];
            
                    Type type = Type.GetType($"{fullTypeName}, {assemblyName}");
                    if (type == null)
                    {
                        return "<Missing Type>";
                    }
                }
            }
    
            // Fallback
            string[] typeParts = property.managedReferenceFullTypename.Split(' ');
            if (typeParts.Length == 2)
            {
                string fullTypeName = typeParts[1];
                string[] nameParts = fullTypeName.Split('.');
                return nameParts[^1];
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
    
            TypeInfo[] types = GetCachedTypes(baseType, attr);
            HashSet<Type> existingTypes = GetExistingTypesInList(property);
            bool showSearch = attr.SearchThreshold >= 0 && types.Length >= attr.SearchThreshold;
    
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
    
            bool isBroken = !string.IsNullOrEmpty(property.managedReferenceFullTypename) 
                            && property.managedReferenceValue == null;
    
            if (isBroken)
            {
                menu.AddItem(new GUIContent("Clear Broken Reference"), false, () => 
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.AddSeparator("");
            }
    
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
    
            if (IsInArray(property))
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("List/Copy Entire List"), false, () => CopyList(property));
        
                if (_listClipboard is { Count: > 0 })
                {
                    menu.AddItem(new GUIContent($"List/Paste Entire List ({_listClipboard.Count} items)"), false, () => PasteList(property));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("List/Paste Entire List"));
                }
        
                menu.AddItem(new GUIContent("List/Clear Entire List"), false, () => ClearList(property));
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
    
            string arrayPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal));
            SerializedProperty arrayProperty = property.serializedObject.FindProperty(arrayPath);
    
            if (arrayProperty == null || !arrayProperty.isArray)
                return existingTypes;
    
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
                    FieldInfo field = GetFieldIncludingBase(parentType, fieldName);
            
                    if (field == null)
                    {
                        Debug.LogError($"Could not find field '{fieldName}' in type '{parentType?.Name}' or its base classes");
                        return null;
                    }
            
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
                else
                {
                    FieldInfo field = GetFieldIncludingBase(parentType, part);
                    if (field == null)
                    {
                        Debug.LogError($"Could not find field '{part}' in type '{parentType?.Name}' or its base classes");
                        return null;
                    }
                    parentType = field.FieldType;
                }
            }
    
            return parentType;
        }

        private FieldInfo GetFieldIncludingBase(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    
            FieldInfo field = null;
            Type currentType = type;
    
            while (currentType != null && field == null)
            {
                field = currentType.GetField(fieldName, flags);
                currentType = currentType.BaseType;
            }
    
            return field;
        }
        
        private TypeInfo[] GetCachedTypes(Type baseType, SerializableSelectorAttribute attr)
        {
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
        
        private void CopyList(SerializedProperty property)
        {
            SerializedProperty arrayProperty = GetArrayProperty(property);
            if (arrayProperty == null) return;
            
            _listClipboard = new List<object>();
            
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue != null)
                {
                    _listClipboard.Add(element.managedReferenceValue.CopyReference());
                }
                else
                {
                    _listClipboard.Add(null);
                }
            }
        }

        private void PasteList(SerializedProperty property)
        {
            if (_listClipboard == null) return;
            
            SerializedProperty arrayProperty = GetArrayProperty(property);
            if (arrayProperty == null) return;
            
            arrayProperty.ClearArray();
            
            for (int i = 0; i < _listClipboard.Count; i++)
            {
                arrayProperty.InsertArrayElementAtIndex(i);
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
                
                if (_listClipboard[i] != null)
                {
                    element.managedReferenceValue = _listClipboard[i].CopyReference();
                }
                else
                {
                    element.managedReferenceValue = null;
                }
            }
            
            arrayProperty.serializedObject.ApplyModifiedProperties();
        }

        private void ClearList(SerializedProperty property)
        {
            SerializedProperty arrayProperty = GetArrayProperty(property);
            if (arrayProperty == null) return;
            
            arrayProperty.ClearArray();
            arrayProperty.serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty GetArrayProperty(SerializedProperty property)
        {
            string arrayPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(".Array.data[", StringComparison.Ordinal));
            return property.serializedObject.FindProperty(arrayPath);
        }
    }
}
#endif