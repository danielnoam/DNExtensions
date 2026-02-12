#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(FoldoutAttribute))]
    public class FoldoutDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();
        private static readonly Dictionary<string, List<string>> GroupCache = new Dictionary<string, List<string>>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string key = GetFoldoutKey(property);
            FoldoutAttribute foldout = attribute as FoldoutAttribute;
            
            if (!FoldoutStates.ContainsKey(key))
            {
                if (foldout != null) FoldoutStates[key] = foldout.DefaultExpanded;
            }

            bool isFirst = IsFirstInGroup(property);
            float height = 0f;

            if (isFirst)
            {
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            if (FoldoutStates[key])
            {
                height += EditorGUI.GetPropertyHeight(property, label, true);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string key = GetFoldoutKey(property);
            FoldoutAttribute foldout = attribute as FoldoutAttribute;
            
            if (!FoldoutStates.ContainsKey(key))
            {
                if (foldout != null) FoldoutStates[key] = foldout.DefaultExpanded;
            }
            
            bool isFirst = IsFirstInGroup(property);

            if (isFirst)
            {
                Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                if (foldout != null)
                    FoldoutStates[key] = EditorGUI.Foldout(foldoutRect, FoldoutStates[key], foldout.GroupName, true);

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                position.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            if (FoldoutStates[key])
            {
                GUIContent propertyLabel = new GUIContent(property.displayName, label.tooltip);
                EditorGUI.PropertyField(position, property, propertyLabel, true);
            }
        }

        private string GetFoldoutKey(SerializedProperty property)
        {
            if (attribute is FoldoutAttribute foldout) return $"{property.serializedObject.targetObject.GetInstanceID()}_{foldout.GroupName}";
            return property.serializedObject.targetObject.GetInstanceID().ToString();
        }

        private string GetCacheKey(SerializedProperty property)
        {
            return $"{property.serializedObject.targetObject.GetType().FullName}_{((FoldoutAttribute)attribute).GroupName}";
        }

        private bool IsFirstInGroup(SerializedProperty property)
        {
            string cacheKey = GetCacheKey(property);
            
            if (!GroupCache.ContainsKey(cacheKey))
            {
                FoldoutAttribute foldout = attribute as FoldoutAttribute;
                List<string> groupFields = new List<string>();
                
                var fields = property.serializedObject.targetObject.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => foldout != null && f.GetCustomAttribute<FoldoutAttribute>()?.GroupName == foldout.GroupName)
                    .Select(f => f.Name);
                
                groupFields.AddRange(fields);
                GroupCache[cacheKey] = groupFields;
            }

            return GroupCache[cacheKey].Count > 0 && GroupCache[cacheKey][0] == property.name;
        }
    }
}
#endif