using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// Base class for ScriptableObject values with change notification and read-only support.
    /// </summary>
    public abstract class SOValue<T> : SOBase 
    {
        [SerializeField] private T value;
        [Tooltip("If true, this value cannot be changed by code at runtime.")]
        [SerializeField] private bool isReadOnly; 

        public event UnityAction<T> OnValueChanged;

        public T Value
        {
            get => value;
            set
            {
                if (isReadOnly)
                {
                    Debug.LogWarning($"Attempted to modify read-only value: {name}");
                    return;
                }
                
                if (!EqualityComparer<T>.Default.Equals(this.value, value))
                {
                    this.value = value;
                    OnValueChanged?.Invoke(this.value);
                }
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                OnValueChanged?.Invoke(value);
            }
        }
        
        /// <summary>
        /// Implicitly converts an <see cref="SOValue{T}"/> to its underlying value.
        /// </summary>
        public static implicit operator T(SOValue<T> soValue)
        {
            if (!soValue)
            {
                throw new System.NullReferenceException(
                    $"Attempted to use a null SOValue<{typeof(T).Name}> as {typeof(T).Name}.");
            }

            return soValue.Value;
        }
    }
}