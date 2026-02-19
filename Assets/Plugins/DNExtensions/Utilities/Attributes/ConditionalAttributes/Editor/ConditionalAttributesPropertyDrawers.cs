using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    internal static class ConditionalAttributeEvaluator
    {
        private static readonly Dictionary<string, MemberInfo> MemberCache = new();
        private static readonly Dictionary<string, SerializedProperty> SiblingCache = new();

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

            var sibling = FindSiblingProperty(property, variableName);

            if (sibling != null)
            {
                if (sibling.propertyType == SerializedPropertyType.Enum && variableValue is Enum)
                    return Equals(sibling.enumValueIndex, Convert.ToInt32(variableValue));

                if (sibling.propertyType == SerializedPropertyType.ObjectReference)
                    return variableValue == null
                        ? sibling.objectReferenceValue == null
                        : Equals(sibling.objectReferenceValue, variableValue);

                return Equals(GetSerializedPropertyValue(sibling), variableValue);
            }

            return false;
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

                if (parent.EndsWith("]"))
                {
                    path = parent;
                    continue;
                }

                var found = property.serializedObject.FindProperty($"{parent}.{siblingName}");
                if (found != null) return found;

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
    }

    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            bool show = ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            return show ? EditorGUI.GetPropertyHeight(property, label, true) : -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            if (!ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property)) return;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (HideIfAttribute)attribute;
            bool hide = ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            return hide ? -EditorGUIUtility.standardVerticalSpacing : EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (HideIfAttribute)attribute;
            if (ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property)) return;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    public class EnableIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (EnableIfAttribute)attribute;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }
    }

    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (DisableIfAttribute)attribute;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !ConditionalAttributeEvaluator.Evaluate(attr.VariableName, attr.VariableValue, property);
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }
    }
}