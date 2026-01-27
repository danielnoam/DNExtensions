using System;
using UnityEngine;

namespace DNExtensions.SerializableSelector
{
    /// <summary>
    /// Enables type selection dropdown for [SerializeReference] fields
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializableSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Filter types by namespace prefix
        /// </summary>
        public string NamespaceFilter { get; }
        
        /// <summary>
        /// Show null option in dropdown
        /// </summary>
        public bool AllowNull { get; }
        
        /// <summary>
        /// Require types to implement these interfaces
        /// </summary>
        public Type[] RequireInterfaces { get; }
        
        /// <summary>
        /// Show search bar when type count exceeds this threshold (0 = always show, -1 = never show)
        /// </summary>
        public int SearchThreshold { get; set; } = 5;

        public SerializableSelectorAttribute(
            string namespaceFilter = null, 
            bool allowNull = true)
        {
            NamespaceFilter = namespaceFilter;
            AllowNull = allowNull;
            RequireInterfaces = null;
        }
        
        /// <summary>
        /// Constructor with interface constraints
        /// </summary>
        public SerializableSelectorAttribute(
            Type[] requireInterfaces,
            string namespaceFilter = null, 
            bool allowNull = true)
        {
            NamespaceFilter = namespaceFilter;
            AllowNull = allowNull;
            RequireInterfaces = requireInterfaces;
        }
    }
}