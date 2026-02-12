using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Core system for AutoGet validation and population.
    /// </summary>
    public static class AutoGetSystem
    {
        /// <summary>
        /// Validates and optionally populates all AutoGet fields on a MonoBehaviour.
        /// </summary>
        public static void Process(MonoBehaviour behaviour)
        {
            if (behaviour == null) return;
            
#if UNITY_EDITOR
            var settings = AutoGetSettings.Instance;
            
            // Check if we should process prefabs
            if (PrefabStageUtility.GetCurrentPrefabStage() != null && !settings.AutoPopulateInPrefabs)
            {
                return;
            }
            
            var fields = AutoGetCache.GetAutoGetFields(behaviour.GetType());
            
            foreach (var field in fields)
            {
                ProcessField(behaviour, field);
            }
#endif
        }
        
        /// <summary>
        /// Populates a specific field on a MonoBehaviour.
        /// </summary>
        public static void PopulateField(MonoBehaviour behaviour, string fieldName)
        {
#if UNITY_EDITOR
            if (behaviour == null) return;
            
            var field = behaviour.GetType().GetField(
                fieldName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            
            if (field == null) return;
            
            var attribute = field.GetCustomAttribute<AutoGetAttribute>();
            if (attribute == null) return;
            
            PopulateFieldInternal(behaviour, field, attribute);
            EditorUtility.SetDirty(behaviour);
#endif
        }
        
        /// <summary>
        /// Checks if a MonoBehaviour type has any AutoGet fields.
        /// </summary>
        public static bool HasAutoGetFields(Type behaviourType)
        {
            if (behaviourType == null || !typeof(MonoBehaviour).IsAssignableFrom(behaviourType))
                return false;
            
            var fields = AutoGetCache.GetAutoGetFields(behaviourType);
            return fields.Length > 0;
        }
        
        /// <summary>
        /// Checks if a MonoBehaviour instance has any AutoGet fields.
        /// </summary>
        public static bool HasAutoGetFields(MonoBehaviour behaviour)
        {
            return behaviour != null && HasAutoGetFields(behaviour.GetType());
        }
        
#if UNITY_EDITOR
        private static void ProcessField(MonoBehaviour behaviour, FieldInfo field)
        {
            var attribute = field.GetCustomAttribute<AutoGetAttribute>();
            if (attribute == null) return;
            
            var currentValue = field.GetValue(behaviour);
            var isFieldEmpty = IsFieldEmpty(currentValue, field.FieldType);
            
            // Determine populate mode
            var populateMode = attribute.PopulateMode == AutoPopulateMode.Default
                ? AutoGetSettings.Instance.AutoPopulateMode
                : attribute.PopulateMode;
            
            var shouldPopulate = populateMode switch
            {
                AutoPopulateMode.Never => false,
                AutoPopulateMode.WhenEmpty => isFieldEmpty,
                AutoPopulateMode.Always => true,
                _ => false
            };
            
            if (shouldPopulate)
            {
                PopulateFieldInternal(behaviour, field, attribute);
            }
            else
            {
                ValidateFieldInternal(behaviour, field, attribute, currentValue);
            }
        }
        
        private static void PopulateFieldInternal(
            MonoBehaviour behaviour, 
            FieldInfo field, 
            AutoGetAttribute attribute)
        {
            try
            {
                var candidates = attribute.GetCandidates(behaviour, GetElementType(field.FieldType));
                candidates = attribute.FilterCandidates(behaviour, candidates);
                
                var value = ConvertToFieldType(candidates, field.FieldType);
                field.SetValue(behaviour, value);
                
                EditorUtility.SetDirty(behaviour);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to populate field '{field.Name}' on {behaviour.GetType().Name}: {e.Message}", behaviour);
            }
        }
        
        private static void ValidateFieldInternal(
            MonoBehaviour behaviour,
            FieldInfo field,
            AutoGetAttribute attribute,
            object currentValue)
        {
            // For now, just silent validation
            // Can add logging based on settings later
        }
        
        private static bool IsFieldEmpty(object value, Type fieldType)
        {
            if (value == null)
                return true;
            
            if (fieldType.IsArray)
            {
                var array = value as Array;
                return array == null || array.Length == 0;
            }
            
            if (IsListType(fieldType))
            {
                var list = value as IList;
                return list == null || list.Count == 0;
            }
            
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                return value as UnityEngine.Object == null;
            }
            
            return false;
        }
        
        private static object ConvertToFieldType(IEnumerable<UnityEngine.Object> candidates, Type fieldType)
        {
            var candidateList = candidates.ToList();
            
            // Single component field
            if (!fieldType.IsArray && !IsListType(fieldType))
            {
                return candidateList.FirstOrDefault();
            }
            
            // Array field
            if (fieldType.IsArray)
            {
                var elementType = fieldType.GetElementType();
                var array = Array.CreateInstance(elementType, candidateList.Count);
                for (int i = 0; i < candidateList.Count; i++)
                {
                    array.SetValue(candidateList[i], i);
                }
                return array;
            }
            
            // List field
            if (IsListType(fieldType))
            {
                var elementType = fieldType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType);
                foreach (var item in candidateList)
                {
                    list.Add(item);
                }
                return list;
            }
            
            return null;
        }
        
        private static Type GetElementType(Type fieldType)
        {
            if (fieldType.IsArray)
                return fieldType.GetElementType();
            
            if (IsListType(fieldType))
                return fieldType.GetGenericArguments()[0];
            
            return fieldType;
        }
        
        private static bool IsListType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
#endif
    }
}