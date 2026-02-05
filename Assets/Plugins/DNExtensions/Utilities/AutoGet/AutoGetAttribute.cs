using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Base attribute for all AutoGet attributes.
    /// Provides common functionality for automatic component/asset reference retrieval.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class AutoGetAttribute : PropertyAttribute
    {
        /// <summary>
        /// Include inactive GameObjects when searching for components.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
        
        /// <summary>
        /// Controls when this field should be auto-populated.
        /// Default uses the global setting from AutoGetSettings.
        /// </summary>
        public AutoPopulateMode PopulateMode { get; set; } = AutoPopulateMode.Default;
        
        /// <summary>
        /// Gets candidate objects that match the search criteria.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour containing the field.</param>
        /// <param name="fieldType">The type of the field to populate.</param>
        /// <returns>All potential candidates before filtering.</returns>
        protected internal abstract IEnumerable<UnityEngine.Object> GetCandidates(
            MonoBehaviour behaviour, 
            Type fieldType
        );
        
        /// <summary>
        /// Applies additional filtering to the candidates.
        /// Override this to add custom filtering logic.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour containing the field.</param>
        /// <param name="candidates">The candidate objects to filter.</param>
        /// <returns>Filtered candidates.</returns>
        protected internal virtual IEnumerable<UnityEngine.Object> FilterCandidates(
            MonoBehaviour behaviour,
            IEnumerable<UnityEngine.Object> candidates
        )
        {
            return candidates;
        }
    }
}
