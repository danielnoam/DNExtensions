using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Automatically gets component references from child GameObjects in the hierarchy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGetChildrenAttribute : AutoGetAttribute
    {
        /// <summary>
        /// Maximum depth to search down the child hierarchy.
        /// -1 means unlimited depth (searches all children).
        /// 1 means only direct children.
        /// </summary>
        public int MaxDepth { get; set; } = -1;
        
        /// <summary>
        /// Exclude components on the same GameObject as the MonoBehaviour.
        /// </summary>
        public bool ExcludeSelf { get; set; } = false;
        
        /// <summary>
        /// Only include GameObjects on these layers.
        /// </summary>
        public string[] Layers { get; set; }
        
        /// <summary>
        /// Only include GameObjects with these tags.
        /// </summary>
        public string[] Tags { get; set; }
        
        /// <summary>
        /// Only include GameObjects whose name contains this string (case-insensitive).
        /// </summary>
        public string NameContains { get; set; }
        
        protected internal override IEnumerable<UnityEngine.Object> GetCandidates(
            MonoBehaviour behaviour, 
            Type fieldType)
        {
            if (MaxDepth < 0)
            {
                return behaviour.GetComponentsInChildren(fieldType, IncludeInactive);
            }
            
            return GetComponentsInChildrenWithDepth(behaviour.transform, fieldType, MaxDepth);
        }
        
        protected internal override IEnumerable<UnityEngine.Object> FilterCandidates(
            MonoBehaviour behaviour,
            IEnumerable<UnityEngine.Object> candidates)
        {
            if (ExcludeSelf)
            {
                candidates = candidates.Where(c => 
                    ((Component)c).transform != behaviour.transform
                );
            }
            
            if (Layers != null && Layers.Length > 0)
            {
                var layerMasks = Layers
                    .Select(LayerMask.NameToLayer)
                    .Where(layer => layer >= 0)
                    .ToArray();
                
                if (layerMasks.Length > 0)
                {
                    candidates = candidates.Where(c => 
                        layerMasks.Contains(((Component)c).gameObject.layer)
                    );
                }
            }
            
            if (Tags != null && Tags.Length > 0)
            {
                candidates = candidates.Where(c => 
                    Tags.Any(tag => ((Component)c).CompareTag(tag))
                );
            }
            
            if (!string.IsNullOrEmpty(NameContains))
            {
                candidates = candidates.Where(c => 
                    c.name.IndexOf(NameContains, StringComparison.OrdinalIgnoreCase) >= 0
                );
            }
            
            return candidates;
        }
        
        private IEnumerable<UnityEngine.Object> GetComponentsInChildrenWithDepth(
            Transform transform, 
            Type type, 
            int maxDepth)
        {
            return GetComponentsInChildrenWithDepthRecursive(transform, type, maxDepth, 0);
        }
        
        private IEnumerable<UnityEngine.Object> GetComponentsInChildrenWithDepthRecursive(
            Transform transform, 
            Type type, 
            int maxDepth, 
            int currentDepth)
        {
            if (currentDepth > maxDepth)
                yield break;
            
            var components = transform.GetComponents(type);
            foreach (var component in components)
            {
                if (IncludeInactive || ((Component)component).gameObject.activeInHierarchy)
                {
                    yield return component;
                }
            }
            
            foreach (Transform child in transform)
            {
                foreach (var component in GetComponentsInChildrenWithDepthRecursive(
                    child, type, maxDepth, currentDepth + 1))
                {
                    yield return component;
                }
            }
        }
    }
}
