using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Automatically gets component references from anywhere in the scene.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGetSceneAttribute : AutoGetAttribute
    {
        /// <summary>
        /// Only include GameObjects on these layers.
        /// </summary>
        public string[] Layers { get; set; }
        
        /// <summary>
        /// Only include GameObjects with these tags.
        /// </summary>
        public string[] Tags { get; set; }
        
        /// <summary>
        /// Only include components on root GameObjects (no parent).
        /// </summary>
        public bool MustBeRootObject { get; set; } = false;
        
        protected internal override IEnumerable<UnityEngine.Object> GetCandidates(
            MonoBehaviour behaviour, 
            Type fieldType)
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindObjectsByType(
                fieldType,
                IncludeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
#else
            return UnityEngine.Object.FindObjectsOfType(fieldType, IncludeInactive);
#endif
        }
        
        protected internal override IEnumerable<UnityEngine.Object> FilterCandidates(
            MonoBehaviour behaviour,
            IEnumerable<UnityEngine.Object> candidates)
        {
            if (MustBeRootObject)
            {
                candidates = candidates.Where(c => 
                    ((Component)c).transform.parent == null
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
            
            return candidates;
        }
    }
}
