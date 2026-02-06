using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Optional value with enabled/disabled state. 
    /// Returns default(T) when accessed while unchecked.
    /// </summary>
    [Serializable]
    public struct OptionalField<T>
    {
        public bool isSet;
        public bool hideValueIfSet;
        [SerializeField] private T value;
        
        
        /// <summary>
        /// Constructor. Creates checked field with given value if isSet is true.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isSet"></param>
        /// <param name="hideValueIfSet"></param>
        public OptionalField(T value, bool isSet = false, bool hideValueIfSet = false)
        {
            this.value = value;
            this.isSet = isSet;
            this.hideValueIfSet = hideValueIfSet;
        }
        
        /// <summary>
        /// Constructor. Creates unchecked field with default(T) value.
        /// </summary>
        /// <param name="isSet"></param>
        /// <param name="hideValueIfSet"></param>
        public OptionalField(bool isSet = false, bool hideValueIfSet = false)
        {
            value = default;
            this.isSet = isSet;
            this.hideValueIfSet = hideValueIfSet;
        }
        
        
        /// <summary>
        /// Returns value when checked, default(T) otherwise.
        /// </summary>
        public T Value
        {
            get => isSet ? value : default;
            set => this.value = value;
        }
        
        
        /// <summary>
        /// Returns value when checked, provided defaultValue otherwise.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetValueOrDefault(T defaultValue) => isSet ? value : defaultValue;

        
        /// <summary>
        /// Implicit conversion to bool returns isSet, allowing for easy checks like "if (optionalField) { ... }"
        /// </summary>
        /// <param name="optional"></param>
        /// <returns></returns>
        public static implicit operator bool(OptionalField<T> optional) => optional.isSet;
    }
}