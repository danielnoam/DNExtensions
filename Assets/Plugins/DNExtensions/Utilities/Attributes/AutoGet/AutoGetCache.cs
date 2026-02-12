using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Caches reflection data for AutoGet fields to improve performance.
    /// Cache is automatically cleared on domain reload (script recompilation).
    /// </summary>
    public static class AutoGetCache
    {
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
        
        /// <summary>
        /// Gets all fields with AutoGetAttribute for the given type.
        /// Uses cache if enabled in settings, otherwise scans directly.
        /// </summary>
        public static FieldInfo[] GetAutoGetFields(Type type)
        {
            if (!AutoGetSettings.Instance.CacheReflectionData)
            {
                return ScanTypeForAutoGetFields(type);
            }
            
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = ScanTypeForAutoGetFields(type);
                _fieldCache[type] = fields;
            }
            
            return fields;
        }
        
        /// <summary>
        /// Clears the entire reflection cache.
        /// </summary>
        public static void Clear()
        {
            _fieldCache.Clear();
        }
        
        /// <summary>
        /// Gets the number of types currently cached.
        /// </summary>
        public static int CacheSize => _fieldCache.Count;
        
        private static FieldInfo[] ScanTypeForAutoGetFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | 
                                      BindingFlags.Public | 
                                      BindingFlags.NonPublic;
            
            var fields = new List<FieldInfo>();
            var currentType = type;
            
            while (currentType != null && currentType != typeof(MonoBehaviour))
            {
                var typeFields = currentType.GetFields(flags | BindingFlags.DeclaredOnly);
                
                foreach (var field in typeFields)
                {
                    if (field.GetCustomAttribute<AutoGetAttribute>() != null)
                    {
                        fields.Add(field);
                    }
                }
                
                currentType = currentType.BaseType;
            }
            
            return fields.ToArray();
        }
    }
}