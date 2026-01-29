#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DNExtensions.Utilities.SerializableSelector.Editor
{
    public static class SerializableSelectorUtility
    {
        public struct TypeInfo
        {
            public Type Type;
            public string DisplayName;
            public string Category;
            public string Tooltip;
            public int SearchScore;
            public bool AllowOnce;
        }
        
        /// <summary>
        /// Get tooltip from attribute or return empty string
        /// </summary>
        public static string GetTypeTooltip(Type type) 
        {
            var attr = type.GetCustomAttribute<SerializableSelectorTooltipAttribute>(); 
            return attr?.Tooltip ?? string.Empty; 
        }
        
        /// <summary>
        /// Check if type meets all interface constraints
        /// </summary>
        public static bool MeetsConstraints(Type type, Type[] constraints)
        {
            if (constraints == null || constraints.Length == 0)
                return true;
            
            foreach (Type constraint in constraints)
            {
                if (!constraint.IsAssignableFrom(type))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Check if type is marked with AllowOnce attribute
        /// </summary>
        public static bool IsAllowOnce(Type type)
        {
            return type.GetCustomAttribute<SerializableSelectorAllowOnceAttribute>() != null;
        }
        
        /// <summary>
        /// Get custom display name from attribute or fallback to type name
        /// </summary>
        public static string GetTypeDisplayName(Type type)
        {
            var attr = type.GetCustomAttribute<SerializableSelectorNameAttribute>();
            return attr?.DisplayName ?? type.Name;
        }

        /// <summary>
        /// Get custom category path from attribute
        /// </summary>
        public static string GetTypeCategoryPath(Type type)
        {
            var attr = type.GetCustomAttribute<SerializableSelectorNameAttribute>();
            return attr?.CategoryPath ?? string.Empty;
        }
        
        /// <summary>
        /// Filter and score types based on search query
        /// </summary>
        public static TypeInfo[] FilterBySearch(TypeInfo[] types, string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                // No search - return all with score 0
                return types.Select(t => 
                {
                    t.SearchScore = 0;
                    return t;
                }).ToArray();
            }
            
            searchQuery = searchQuery.ToLower();
            
            var scored = types.Select(t =>
            {
                t.SearchScore = CalculateSearchScore(t, searchQuery);
                return t;
            })
            .Where(t => t.SearchScore > 0)
            .OrderByDescending(t => t.SearchScore)
            .ToArray();
            
            return scored;
        }
        
        private static int CalculateSearchScore(TypeInfo typeInfo, string query)
        {
            string displayName = typeInfo.DisplayName.ToLower();
            string categoryPath = typeInfo.Category.ToLower();
            string fullPath = string.IsNullOrEmpty(categoryPath) 
                ? displayName 
                : $"{categoryPath}/{displayName}".ToLower();
    
            // Exact match on display name - highest score
            if (displayName == query)
                return 1000;
    
            // Starts with query - high score
            if (displayName.StartsWith(query))
                return 500;
    
            // Contains query - medium score
            if (displayName.Contains(query))
                return 250;
    
            // Full path contains - low score (only if category exists)
            if (!string.IsNullOrEmpty(categoryPath) && fullPath.Contains(query))
                return 100;
    
            // Tooltip contains - lowest score
            if (!string.IsNullOrEmpty(typeInfo.Tooltip) && 
                typeInfo.Tooltip.ToLower().Contains(query))
                return 50;
    
            return 0;
        }
        
        /// <summary>
        /// Get all types derived from base type with filters applied
        /// </summary>
        public static TypeInfo[] GetDerivedTypes(
            Type baseType, 
            string namespaceFilter = null,
            Type[] requiredInterfaces = null)
        {
            var derivedTypes = new List<TypeInfo>();
    
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        // Must be assignable, concrete, and serializable
                        if (!baseType.IsAssignableFrom(type) || 
                            type.IsAbstract || 
                            type.IsInterface ||
                            type.GetCustomAttribute<SerializableAttribute>() == null)
                            continue;
                
                        // Get custom category path or use namespace
                        string categoryPath = GetTypeCategoryPath(type);
                
                        // Apply namespace filter 
                        if (!string.IsNullOrEmpty(namespaceFilter) && 
                            !categoryPath.StartsWith(namespaceFilter))
                            continue;
                
                        // Apply interface constraints
                        if (!MeetsConstraints(type, requiredInterfaces))
                            continue;
                
                        derivedTypes.Add(new TypeInfo
                        {
                            Type = type,
                            DisplayName = GetTypeDisplayName(type),   
                            Category = categoryPath,                 
                            Tooltip = GetTypeTooltip(type),
                            SearchScore = 0,
                            AllowOnce = IsAllowOnce(type)
                        });
                    }
                }
                catch
                {
                    // Skip problematic assemblies
                }
            }
    
            return derivedTypes.ToArray();
        }
    }
}
#endif