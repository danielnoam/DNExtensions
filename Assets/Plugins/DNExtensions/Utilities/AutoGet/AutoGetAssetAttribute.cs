using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Automatically gets asset references from the project.
    /// Editor-only attribute - has no effect in builds.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGetAssetAttribute : AutoGetAttribute
    {
        /// <summary>
        /// Folders to search in. Defaults to ["Assets"] if not specified.
        /// Example: new[] { "Assets/Data", "Assets/Prefabs" }
        /// </summary>
        public string[] Folders { get; set; }
        
        /// <summary>
        /// Only include assets with this exact name (case-insensitive).
        /// </summary>
        public string AssetName { get; set; }
        
        protected internal override IEnumerable<UnityEngine.Object> GetCandidates(
            MonoBehaviour behaviour, 
            Type fieldType)
        {
#if UNITY_EDITOR
            var searchFilter = $"t:{fieldType.Name}";
            var searchFolders = Folders != null && Folders.Length > 0 
                ? Folders 
                : new[] { "Assets" };
            
            var guids = AssetDatabase.FindAssets(searchFilter, searchFolders);
            
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath(path, fieldType))
                .Where(asset => asset != null);
#else
            return Array.Empty<UnityEngine.Object>();
#endif
        }
        
        protected internal override IEnumerable<UnityEngine.Object> FilterCandidates(
            MonoBehaviour behaviour,
            IEnumerable<UnityEngine.Object> candidates)
        {
            if (!string.IsNullOrEmpty(AssetName))
            {
                candidates = candidates.Where(c => 
                    c.name.Equals(AssetName, StringComparison.OrdinalIgnoreCase)
                );
            }
            
            return candidates;
        }
    }
}
