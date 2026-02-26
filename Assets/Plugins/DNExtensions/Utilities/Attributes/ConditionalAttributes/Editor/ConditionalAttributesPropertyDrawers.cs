using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [InitializeOnLoad]
    internal static class ConditionalAttributeEvaluatorInitializer
    {
        static ConditionalAttributeEvaluatorInitializer()
        {
            ConditionalAttributeEvaluator.ClearCaches();
        }
    }

    internal static class ConditionalAttributeEvaluator
    {
        private static readonly Dictionary<string, MemberInfo> MemberCache = new();
        private static readonly Dictionary<string, SerializedProperty> SiblingCache = new();

        internal static void ClearCaches()
        {
            MemberCache.Clear();
            SiblingCache.Clear();
        }

        public static bool Evaluate(string variableName, object variableValue, SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var targetType = targetObject.GetType();
            string cacheKey = $"{targetType.FullName}.{variableName}";

            if (!MemberCache.TryGetValue(cacheKey, out var member))
            {
                member = FindMember(targetType, variableName);
                MemberCache[cacheKey] = member;
            }

            if (member is PropertyInfo propInfo)
                return Equals(propInfo.GetValue(targetObject), variableValue);

            if (member is MethodInfo methodInfo)
                return (bool)methodInfo.Invoke(targetObject, null);

            return FindSiblingProperty(property, variableName) is { } sibling && EvaluateSibling(sibling, variableValue);
        }

        private static bool EvaluateSibling(SerializedProperty sibling, object variableValue)
        {
            if (sibling.propertyType == SerializedPropertyType.Enum && variableValue is Enum)
                return Equals(sibling.enumValueIndex, Convert.ToInt32(variableValue));

            if (sibling.propertyType == SerializedPropertyType.ObjectReference)
                return variableValue == null
                    ? sibling.objectReferenceValue == null
                    : Equals(sibling.objectReferenceValue, variableValue);

            return Equals(GetSerializedPropertyValue(sibling), variableValue);
        }

        private static MemberInfo FindMember(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var current = type;
            while (current != null)
            {
                var prop = current.GetProperty(name, flags);
                if (prop != null) return prop;

                var method = current.GetMethod(name, flags, null, Type.EmptyTypes, null);
                if (method != null && method.ReturnType == typeof(bool)) return method;

                current = current.BaseType;
            }

            return null;
        }

        private static SerializedProperty FindSiblingProperty(SerializedProperty property, string siblingName)
        {
            string path = property.propertyPath;

            while (path.Length > 0)
            {
                int lastDot = path.LastIndexOf('.');
                if (lastDot < 0) break;

                string parent = path.Substring(0, lastDot);

                var found = property.serializedObject.FindProperty($"{parent}.{siblingName}");
                if (found != null) return found;

                if (parent.EndsWith("]"))
                {
                    int bracketOpen = parent.LastIndexOf('[');
                    if (bracketOpen >= 0)
                    {
                        string elementPath = parent.Substring(0, bracketOpen - 1);
                        int arrayDot = elementPath.LastIndexOf('.');
                        if (arrayDot >= 0)
                        {
                            string arrayParent = elementPath.Substring(0, arrayDot);
                            found = property.serializedObject.FindProperty($"{arrayParent}.{siblingName}");
                            if (found != null) return found;
                            path = arrayParent;
                        }
                        else break;
                    }
                    else break;
                    continue;
                }

                path = parent;
            }

            return property.serializedObject.FindProperty(siblingName);
        }

        private static object GetSerializedPropertyValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Boolean         => property.boolValue,
                SerializedPropertyType.Integer         => property.intValue,
                SerializedPropertyType.Float           => property.floatValue,
                SerializedPropertyType.String          => property.stringValue,
                SerializedPropertyType.Enum            => property.enumValueIndex,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                _                                      => null
            };
        }

        public static void DrawProperty(Rect position, SerializedProperty property, GUIContent label, FieldInfo fieldInfo)
        {
            var range = fieldInfo?.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                if (property.propertyType == SerializedPropertyType.Float)
                    property.floatValue = EditorGUI.Slider(position, label, property.floatValue, range.min, range.max);
                else if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = EditorGUI.IntSlider(position, label, property.intValue, (int)range.min, (int)range.max);
                return;
            }

            var min = fieldInfo?.GetCustomAttribute<MinAttribute>();
            if (min != null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                if (property.propertyType == SerializedPropertyType.Float)
                    property.floatValue = Mathf.Max(property.floatValue, min.min);
                else if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = Mathf.Max(property.intValue, (int)min.min);
                return;
            }

            var multiline = fieldInfo?.GetCustomAttribute<MultilineAttribute>();
            if (multiline != null)
            {
                property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
                return;
            }

            var textArea = fieldInfo?.GetCustomAttribute<TextAreaAttribute>();
            if (textArea != null)
            {
                EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), label);
                float textY = position.y + EditorGUIUtility.singleLineHeight;
                float textH = position.height - EditorGUIUtility.singleLineHeight;
                property.stringValue = EditorGUI.TextArea(new Rect(position.x, textY, position.width, textH), property.stringValue);
                return;
            }

            var delayed = fieldInfo?.GetCustomAttribute<DelayedAttribute>();
            if (delayed != null)
            {
                if (property.propertyType == SerializedPropertyType.Float)
                    property.floatValue = EditorGUI.DelayedFloatField(position, label, property.floatValue);
                else if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = EditorGUI.DelayedIntField(position, label, property.intValue);
                else if (property.propertyType == SerializedPropertyType.String)
                    property.stringValue = EditorGUI.DelayedTextField(position, label, property.stringValue);
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        public static float GetPropertyHeight(SerializedProperty property, GUIContent label, FieldInfo fieldInfo)
        {
            var textArea = fieldInfo?.GetCustomAttribute<TextAreaAttribute>();
            if (textArea != null)
            {
                int lines = Mathf.Clamp(property.stringValue.Split('\n').Length, textArea.minLines, textArea.maxLines);
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing;
            }

            var multiline = fieldInfo?.GetCustomAttribute<MultilineAttribute>();
            if (multiline != null)
                return EditorGUIUtility.singleLineHeight * multiline.lines;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            string[] parts = property.propertyPath.Replace(".Array.data[", "[").Split('.');

            FieldInfo field = null;
            var currentType = targetType;

            foreach (var part in parts)
            {
                string fieldName = part.Contains("[") ? part.Substring(0, part.IndexOf('[')) : part;

                field = null;
                var type = currentType;
                while (type != null)
                {
                    field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null) break;
                    type = type.BaseType;
                }

                if (field == null) return null;

                currentType = field.FieldType;
                if (currentType.IsArray) currentType = currentType.GetElementType();
                else if (currentType.IsGenericType) currentType = currentType.GetGenericArguments()[0];
            }

            return field;
        }
    }
    

    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            bool show = ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            return show
                ? ConditionalAttributeEvaluator.GetPropertyHeight(property, label, ConditionalAttributeEvaluator.GetFieldInfo(property))
                : -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            if (!ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property)) return;
            ConditionalAttributeEvaluator.DrawProperty(position, property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
        }
    }

    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (HideIfAttribute)attribute;
            bool hide = ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            return hide
                ? -EditorGUIUtility.standardVerticalSpacing
                : ConditionalAttributeEvaluator.GetPropertyHeight(property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (HideIfAttribute)attribute;
            if (ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property)) return;
            ConditionalAttributeEvaluator.DrawProperty(position, property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
        }
    }

    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ConditionalAttributeEvaluator.GetPropertyHeight(property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (EnableIfAttribute)attribute;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            ConditionalAttributeEvaluator.DrawProperty(position, property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
            GUI.enabled = wasEnabled;
        }
    }

    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ConditionalAttributeEvaluator.GetPropertyHeight(property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (DisableIfAttribute)attribute;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            ConditionalAttributeEvaluator.DrawProperty(position, property, label, ConditionalAttributeEvaluator.GetFieldInfo(property));
            GUI.enabled = wasEnabled;
        }
    }
}