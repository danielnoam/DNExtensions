#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        
        private enum ErrorType
        {
            None,
            InvalidPropertyType,
            MissingType
        }
        
        private struct ErrorInfo
        {
            public ErrorType Type;
            public string Message;
            public string Details;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ErrorInfo error = ValidateProperty(property);
            
            if (error.Type != ErrorType.None)
            {
                DrawError(position, property, label, error);
                return;
            }
            
            SerializableSelectorAttribute attr = attribute as SerializableSelectorAttribute;
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            bool inArray = IsInArray(property);
            
            Rect dropdownRect = inArray 
                ? new Rect(position.x, position.y, position.width, lineHeight)
                : new Rect(
                    position.x + EditorGUIUtility.labelWidth + 2, 
                    position.y, 
                    position.width - EditorGUIUtility.labelWidth - 2, 
                    lineHeight
                );
            
            if (!inArray)
            {
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
                EditorGUI.LabelField(labelRect, label);
            }
            
            string currentTypeName = GetTypeName(property);
            
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(currentTypeName), FocusType.Keyboard))
            {
                ShowTypeMenu(property, attr, dropdownRect);
            }
            
            Event e = Event.current;
            if (e.type == EventType.ContextClick && dropdownRect.Contains(e.mousePosition))
            {
                ShowContextMenu(property);
                e.Use();
            }
            
            if (property.managedReferenceValue != null)
            {
                Rect contentRect = new Rect(
                    position.x,
                    position.y + lineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    position.height - lineHeight - EditorGUIUtility.standardVerticalSpacing
                );
                
                EditorGUI.indentLevel++;
                
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
            ErrorInfo error = ValidateProperty(property);
            
            if (error.Type != ErrorType.None)
            {
                return EditorGUIUtility.singleLineHeight + 
                       (string.IsNullOrEmpty(error.Details) ? 0 : EditorGUIUtility.singleLineHeight);
            }
            
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
        
        /// <summary>
        /// Validate property and return error information
        /// </summary>
        private ErrorInfo ValidateProperty(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return new ErrorInfo
                {
                    Type = ErrorType.InvalidPropertyType,
                    Message = "[SerializableSelector] requires [SerializeReference]",
                    Details = null
                };
            }
            
            if (property.managedReferenceValue == null && 
                !string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                string[] parts = property.managedReferenceFullTypename.Split(' ');
                if (parts.Length == 2)
                {
                    string assemblyName = parts[0];
                    string fullTypeName = parts[1];
                    
                    Type type = Type.GetType($"{fullTypeName}, {assemblyName}");
                    if (type == null)
                    {
                        return new ErrorInfo
                        {
                            Type = ErrorType.MissingType,
                            Message = "Missing type reference",
                            Details = $"Type '{fullTypeName}' no longer exists.\nSet to null or select a new type."
                        };
                    }
                }
            }
            
            return new ErrorInfo { Type = ErrorType.None };
        }
        
        /// <summary>
        /// Draw error message with context menu option to clear
        /// </summary>
        private void DrawError(Rect position, SerializedProperty property, GUIContent label, ErrorInfo error)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            bool inArray = IsInArray(property);
            
            Rect messageRect = new Rect(
                position.x + (inArray ? 0 : EditorGUIUtility.labelWidth + 2),
                position.y,
                position.width - (inArray ? 0 : EditorGUIUtility.labelWidth + 2),
                lineHeight
            );
            
            if (!inArray)
            {
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lineHeight);
                EditorGUI.LabelField(labelRect, label);
            }
            
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.3f, 0.3f);
            
            string icon = error.Type == ErrorType.MissingType ? "✕" : "⚠";
            GUIContent errorContent = new GUIContent($"{icon} {error.Message}");
            
            EditorGUI.LabelField(messageRect, errorContent);
            GUI.color = previousColor;
            
            // Click to ping object
            Event clickEvent = Event.current;
            if (clickEvent.type == EventType.MouseDown && messageRect.Contains(clickEvent.mousePosition))
            {
                EditorGUIUtility.PingObject(property.serializedObject.targetObject);
                clickEvent.Use();
            }
            
            if (!string.IsNullOrEmpty(error.Details))
            {
                Rect detailsRect = new Rect(
                    messageRect.x,
                    messageRect.y + lineHeight,
                    messageRect.width,
                    lineHeight
                );
                
                GUIStyle detailStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.gray }
                };
                
                EditorGUI.LabelField(detailsRect, error.Details, detailStyle);
            }
            
            if (error.Type == ErrorType.MissingType)
            {
                Event e = Event.current;
                if (e.type == EventType.ContextClick && messageRect.Contains(e.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Set to Null"), false, () =>
                    {
                        property.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                    menu.ShowAsContext();
                    e.Use();
                }
            }
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
            if (types == null)
            {
                Debug.LogError("Failed to retrieve types for SerializableSelector");
                return;
            }
            
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
            
            bool hasValue = property.managedReferenceValue != null;
            bool isInArray = IsInArray(property);
            
            if (hasValue)
            {
                menu.AddItem(new GUIContent("Copy"), false, () => CopyValue(property));
                menu.AddItem(new GUIContent("Set to Null"), false, () => SetType(property, null));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy"));
                menu.AddDisabledItem(new GUIContent("Set to Null"));
            }
            
            if (CanPaste(property))
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteValue(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }
            
            if (isInArray)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Copy List"), false, () => CopyList(property));
                
                if (_listClipboard != null && _listClipboard.Count > 0)
                {
                    menu.AddItem(new GUIContent("Paste List"), false, () => PasteList(property));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Paste List"));
                }
                
                menu.AddItem(new GUIContent("Clear List"), false, () => ClearList(property));
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
                        return null;
            
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
                        return null;
                    
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
                try
                {
                    TypeInfo[] types = SerializableSelectorUtility.GetDerivedTypes(
                        baseType,
                        attr.NamespaceFilter,
                        attr.RequireInterfaces
                    );
                    TypeCache[cacheKey] = types;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to cache types for {baseType.Name}: {e.Message}");
                    return Array.Empty<TypeInfo>();
                }
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