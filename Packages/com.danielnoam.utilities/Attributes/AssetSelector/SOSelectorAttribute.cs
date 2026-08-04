using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Enables ScriptableObject selection dropdown for SO fields.
    /// Searches for all ScriptableObjects in the project that match the specified criteria.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SOSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Optional folder path to search in (e.g., "Assets/Data/Weapons")
        /// If null, searches entire project
        /// </summary>
        public string FolderPath { get; }
        
        /// <summary>
        /// Optional type filter - only show SOs of this type or derived types
        /// If null, shows all ScriptableObjects compatible with field type
        /// </summary>
        public Type TypeFilter { get; }
        
        /// <summary>
        /// Optional interface filter - only show SOs that implement this interface
        /// Can be combined with TypeFilter
        /// </summary>
        public Type InterfaceFilter { get; }
        
        /// <summary>
        /// Optional search filter for asset names (e.g., "Weapon", "Player")
        /// If null, searches for all matching ScriptableObjects
        /// </summary>
        public string SearchFilter { get; }
        
        /// <summary>
        /// Show search bar when SO count exceeds this threshold
        /// </summary>
        public int SearchThreshold { get; set; } = 10;
        
        /// <summary>
        /// Allow null/none option
        /// </summary>
        public bool AllowNull { get; set; } = true;
        
        /// <summary>
        /// If true, prevents dragging SOs that don't match the folder/filter criteria
        /// If false, allows any valid SO to be dragged (default Unity behavior)
        /// </summary>
        public bool LockToFilter { get; set; } = false;

        /// <summary>
        /// Basic SO selector - searches all ScriptableObjects in project
        /// </summary>
        public SOSelectorAttribute()
        {
            FolderPath = null;
            TypeFilter = null;
            InterfaceFilter = null;
            SearchFilter = null;
        }
        
        /// <summary>
        /// SO selector with folder path
        /// </summary>
        /// <param name="folderPath">Folder to search in (e.g., "Assets/Data/Weapons")</param>
        public SOSelectorAttribute(string folderPath)
        {
            FolderPath = folderPath;
            TypeFilter = null;
            InterfaceFilter = null;
            SearchFilter = null;
        }
        
        /// <summary>
        /// SO selector with type filter (can be a class or interface)
        /// </summary>
        /// <param name="typeFilter">Only show SOs of this type/interface or derived types</param>
        public SOSelectorAttribute(Type typeFilter)
        {
            FolderPath = null;
            
            if (typeFilter != null && typeFilter.IsInterface)
            {
                TypeFilter = null;
                InterfaceFilter = typeFilter;
            }
            else
            {
                TypeFilter = typeFilter;
                InterfaceFilter = null;
            }
            
            SearchFilter = null;
        }
        
        /// <summary>
        /// SO selector with folder and type filter
        /// </summary>
        /// <param name="folderPath">Folder to search in (e.g., "Assets/Data/Weapons")</param>
        /// <param name="typeFilter">Only show SOs of this type or derived types</param>
        public SOSelectorAttribute(string folderPath, Type typeFilter)
        {
            FolderPath = folderPath;
            
            if (typeFilter != null && typeFilter.IsInterface)
            {
                TypeFilter = null;
                InterfaceFilter = typeFilter;
            }
            else
            {
                TypeFilter = typeFilter;
                InterfaceFilter = null;
            }
            
            SearchFilter = null;
        }
        
        /// <summary>
        /// SO selector with folder and search filter
        /// </summary>
        /// <param name="folderPath">Folder to search in (e.g., "Assets/Data/Weapons")</param>
        /// <param name="searchFilter">Additional search terms for asset names (e.g., "Legendary", "Common")</param>
        public SOSelectorAttribute(string folderPath, string searchFilter)
        {
            FolderPath = folderPath;
            TypeFilter = null;
            InterfaceFilter = null;
            SearchFilter = searchFilter;
        }
        
        /// <summary>
        /// SO selector with type and interface filters
        /// </summary>
        /// <param name="typeFilter">Only show SOs of this type or derived types</param>
        /// <param name="interfaceFilter">Only show SOs that implement this interface</param>
        public SOSelectorAttribute(Type typeFilter, Type interfaceFilter)
        {
            FolderPath = null;
            TypeFilter = typeFilter;
            InterfaceFilter = interfaceFilter;
            SearchFilter = null;
            
            if (interfaceFilter != null && !interfaceFilter.IsInterface)
            {
                Debug.LogWarning($"[SOSelector] {interfaceFilter.Name} is not an interface. Use TypeFilter instead.");
            }
        }
        
        /// <summary>
        /// SO selector with folder, type, and interface filters
        /// </summary>
        /// <param name="folderPath">Folder to search in</param>
        /// <param name="typeFilter">Only show SOs of this type or derived types</param>
        /// <param name="interfaceFilter">Only show SOs that implement this interface</param>
        public SOSelectorAttribute(string folderPath, Type typeFilter, Type interfaceFilter)
        {
            FolderPath = folderPath;
            TypeFilter = typeFilter;
            InterfaceFilter = interfaceFilter;
            SearchFilter = null;
            
            if (interfaceFilter != null && !interfaceFilter.IsInterface)
            {
                Debug.LogWarning($"[SOSelector] {interfaceFilter.Name} is not an interface. Use TypeFilter instead.");
            }
        }
    }
}