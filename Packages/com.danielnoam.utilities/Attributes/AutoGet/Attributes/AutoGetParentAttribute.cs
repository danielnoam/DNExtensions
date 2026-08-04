using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Automatically gets a component reference from parent GameObjects in the hierarchy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGetParentAttribute : AutoGetAttribute
    {
        /// <summary>
        /// Maximum depth to search up the parent hierarchy.
        /// -1 means unlimited depth (searches all parents).
        /// 1 means only the direct parent.
        /// </summary>
        public int MaxDepth { get; set; } = -1;
        
        /// <summary>
        /// Exclude components on the same GameObject as the MonoBehaviour.
        /// </summary>
        public bool ExcludeSelf { get; set; } = false;
        
        protected internal override IEnumerable<UnityEngine.Object> GetCandidates(
            MonoBehaviour behaviour, 
            Type fieldType)
        {
            if (MaxDepth < 0)
            {
                return behaviour.GetComponentsInParent(fieldType, IncludeInactive);
            }
            
            return GetComponentsInParentWithDepth(behaviour.transform, fieldType, MaxDepth);
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
            
            return candidates;
        }
        
        private IEnumerable<UnityEngine.Object> GetComponentsInParentWithDepth(
            Transform transform, 
            Type type, 
            int maxDepth)
        {
            var current = transform;
            var depth = 0;
            
            while (current != null && depth <= maxDepth)
            {
                var components = current.GetComponents(type);
                foreach (var component in components)
                {
                    if (IncludeInactive || ((Component)component).gameObject.activeInHierarchy)
                    {
                        yield return component;
                    }
                }
                
                current = current.parent;
                depth++;
            }
        }
    }
}
