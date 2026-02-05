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
        [SerializeField] private T value;
        
        public T Value
        {
            get => isSet ? value : default;
            set => this.value = value;
        }

        public OptionalField(T value, bool isSet = false)
        {
            this.value = value;
            this.isSet = isSet;
        }

        public static OptionalField<T> WithValue(T value) => new OptionalField<T>(value, true);
        public static OptionalField<T> None() => new OptionalField<T>(default, false);

        public T GetValueOrDefault(T defaultValue) => isSet ? value : defaultValue;

        public static implicit operator bool(OptionalField<T> optional) => optional.isSet;
    }
}