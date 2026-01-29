using System;
using UnityEngine;

namespace DNExtensions.Utilities.PrefabSelector
{
    /// <summary>
    /// Enables prefab selection dropdown for GameObject/Component fields.
    /// Searches for all prefabs in the project that match the specified criteria.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PrefabSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Optional folder path to search in (e.g., "Assets/Prefabs/NPCs")
        /// If null, searches entire project
        /// </summary>
        public string FolderPath { get; }
        
        /// <summary>
        /// Optional search filter (e.g., "t:Prefab NPC" or "Enemy")
        /// If null, searches for all prefabs
        /// </summary>
        public string SearchFilter { get; }
        
        /// <summary>
        /// Show search bar when prefab count exceeds this threshold
        /// </summary>
        public int SearchThreshold { get; set; } = 10;
        
        /// <summary>
        /// Allow null/none option
        /// </summary>
        public bool AllowNull { get; set; } = true;
        
        /// <summary>
        /// If true, prevents dragging prefabs that don't match the folder/filter criteria
        /// If false, allows any valid prefab to be dragged (default Unity behavior)
        /// </summary>
        public bool LockDragDrop { get; set; } = false;

        /// <summary>
        /// Basic prefab selector - searches all prefabs in project
        /// </summary>
        public PrefabSelectorAttribute()
        {
            FolderPath = null;
            SearchFilter = null;
        }
        
        /// <summary>
        /// Prefab selector with folder path
        /// </summary>
        /// <param name="folderPath">Folder to search in (e.g., "Assets/Prefabs/NPCs")</param>
        public PrefabSelectorAttribute(string folderPath)
        {
            FolderPath = folderPath;
            SearchFilter = null;
        }
        
        /// <summary>
        /// Prefab selector with folder and search filter
        /// </summary>
        /// <param name="folderPath">Folder to search in (e.g., "Assets/Prefabs/NPCs")</param>
        /// <param name="searchFilter">Additional search terms (e.g., "Boss", "Enemy Goblin", "Player Character")</param>
        public PrefabSelectorAttribute(string folderPath, string searchFilter)
        {
            FolderPath = folderPath;
            SearchFilter = searchFilter;
        }
    }
}