using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.SerializedInterface
{
    /// <summary>
    /// A serializable reference to a UnityEngine.Object that implements a specific interface.
    /// Provides type-safe interface references in the Unity Inspector.
    /// </summary>
    /// <typeparam name="TInterface">The interface type that the object must implement</typeparam>
    /// <typeparam name="TObject">The Unity Object type constraint (e.g., MonoBehaviour, ScriptableObject)</typeparam>
    [Serializable]
    public class InterfaceReference<TInterface, TObject> : ISerializationCallbackReceiver
        where TObject : Object 
        where TInterface : class
    {
        [SerializeField, HideInInspector] 
        private TObject underlyingValue;
        
        // Cached validation state
        [NonSerialized]
        private bool hasValidated;
        [NonSerialized]
        private bool isValid;
        [NonSerialized]
        private string lastValidationError;

        /// <summary>
        /// Gets or sets the interface value with type safety and validation.
        /// </summary>
        /// <value>The interface implementation or null</value>
        /// <exception cref="InvalidOperationException">Thrown when the underlying object doesn't implement the required interface</exception>
        /// <exception cref="ArgumentException">Thrown when setting a value that doesn't match the required object type</exception>
        public TInterface Value
        {
            get
            {
                ValidateInternal();
                
                return underlyingValue switch
                {
                    null => null,
                    TInterface @interface => @interface,
                    _ => throw new InvalidOperationException(
                        $"Object '{underlyingValue.name}' (type: {underlyingValue.GetType().Name}) " +
                        $"does not implement required interface '{typeof(TInterface).Name}'. " +
                        $"Please assign an object that implements this interface.")
                };
            }
            set
            {
                if (value == null)
                {
                    underlyingValue = null;
                    InvalidateCache();
                    return;
                }

                if (value is TObject newValue)
                {
                    underlyingValue = newValue;
                    InvalidateCache();
                }
                else
                {
                    var actualType = value.GetType().Name;
                    throw new ArgumentException(
                        $"Cannot assign value of type '{actualType}' to InterfaceReference. " +
                        $"Expected type: '{typeof(TObject).Name}' or derived type. " +
                        $"Ensure the assigned object inherits from '{typeof(TObject).Name}'.",
                        nameof(value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the underlying Unity Object without interface validation.
        /// Use this for direct access when interface validation is not needed.
        /// </summary>
        /// <value>The underlying Unity Object</value>
        public TObject UnderlyingValue
        {
            get => underlyingValue;
            set
            {
                underlyingValue = value;
                InvalidateCache();
            }
        }

        /// <summary>
        /// Gets whether the current reference is valid (not null and implements the interface).
        /// This property does not throw exceptions.
        /// </summary>
        public bool IsValid
        {
            get
            {
                ValidateInternal();
                return isValid;
            }
        }

        /// <summary>
        /// Gets whether the underlying value is null.
        /// </summary>
        public bool IsNull => underlyingValue == null;

        /// <summary>
        /// Gets the last validation error message, or null if validation succeeded.
        /// </summary>
        public string LastValidationError
        {
            get
            {
                ValidateInternal();
                return lastValidationError;
            }
        }

        /// <summary>
        /// Creates an empty InterfaceReference.
        /// </summary>
        public InterfaceReference()
        {
            underlyingValue = null;
        }

        /// <summary>
        /// Creates an InterfaceReference from a Unity Object.
        /// </summary>
        /// <param name="target">The Unity Object to reference</param>
        /// <exception cref="ArgumentException">Thrown if the target doesn't implement the required interface</exception>
        public InterfaceReference(TObject target)
        {
            underlyingValue = target;
            ValidateOrThrow();
        }

        /// <summary>
        /// Creates an InterfaceReference from an interface implementation.
        /// </summary>
        /// <param name="interface">The interface implementation to reference</param>
        /// <exception cref="ArgumentException">Thrown if the interface is not a valid Unity Object type</exception>
        public InterfaceReference(TInterface @interface)
        {
            if (@interface == null)
            {
                underlyingValue = null;
                return;
            }

            if (@interface is TObject obj)
            {
                underlyingValue = obj;
                ValidateOrThrow();
            }
            else
            {
                throw new ArgumentException(
                    $"Interface implementation must be of type '{typeof(TObject).Name}' or derived type.",
                    nameof(@interface));
            }
        }

        /// <summary>
        /// Validates that the underlying value implements the required interface.
        /// </summary>
        /// <returns>True if valid or null, false if the object doesn't implement the interface</returns>
        public bool Validate()
        {
            ValidateInternal();
            return isValid;
        }

        /// <summary>
        /// Validates the reference and throws an exception if invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        public void ValidateOrThrow()
        {
            if (!Validate() && underlyingValue != null)
            {
                throw new InvalidOperationException(lastValidationError);
            }
        }

        /// <summary>
        /// Tries to get the interface value without throwing exceptions.
        /// </summary>
        /// <param name="value">The interface value if successful</param>
        /// <returns>True if the value was retrieved successfully, false otherwise</returns>
        public bool TryGetValue(out TInterface value)
        {
            if (Validate())
            {
                value = underlyingValue as TInterface;
                return value != null || underlyingValue == null;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Implicitly converts the InterfaceReference to the interface type.
        /// </summary>
        /// <param name="reference">The InterfaceReference to convert</param>
        /// <returns>The interface implementation or null</returns>
        public static implicit operator TInterface(InterfaceReference<TInterface, TObject> reference)
        {
            return reference?.Value;
        }

        /// <summary>
        /// Checks equality with another object.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the underlying values are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is InterfaceReference<TInterface, TObject> other)
            {
                return Equals(underlyingValue, other.underlyingValue);
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code of the underlying value.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return underlyingValue != null ? underlyingValue.GetHashCode() : 0;
        }

        /// <summary>
        /// Returns a string representation of the InterfaceReference.
        /// </summary>
        /// <returns>A string describing the reference state</returns>
        public override string ToString()
        {
            if (underlyingValue == null)
                return $"InterfaceReference<{typeof(TInterface).Name}> (null)";
            
            return $"InterfaceReference<{typeof(TInterface).Name}> ({underlyingValue.name})";
        }

        #region ISerializationCallbackReceiver Implementation

        /// <summary>
        /// Called before Unity serializes the object.
        /// </summary>
        public void OnBeforeSerialize()
        {
            // Validate before serialization to catch issues early
            ValidateInternal();
        }

        /// <summary>
        /// Called after Unity deserializes the object.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Invalidate cache after deserialization
            InvalidateCache();
        }

        #endregion

        #region Private Methods

        private void ValidateInternal()
        {
            if (hasValidated)
                return;

            if (underlyingValue == null)
            {
                isValid = true;
                lastValidationError = null;
            }
            else if (underlyingValue is TInterface)
            {
                isValid = true;
                lastValidationError = null;
            }
            else
            {
                isValid = false;
                lastValidationError = $"Object '{underlyingValue.name}' (type: {underlyingValue.GetType().Name}) " +
                                     $"does not implement required interface '{typeof(TInterface).Name}'.";
            }

            hasValidated = true;
        }

        private void InvalidateCache()
        {
            hasValidated = false;
            isValid = false;
            lastValidationError = null;
        }

        #endregion
    }

    /// <summary>
    /// A simplified InterfaceReference that uses UnityEngine.Object as the base type.
    /// Use this when you don't need to constrain to a specific Unity Object type.
    /// </summary>
    /// <typeparam name="TInterface">The interface type that the object must implement</typeparam>
    [Serializable]
    public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> 
        where TInterface : class 
    {
        /// <summary>
        /// Creates an empty InterfaceReference.
        /// </summary>
        public InterfaceReference() : base() { }

        /// <summary>
        /// Creates an InterfaceReference from a Unity Object.
        /// </summary>
        /// <param name="target">The Unity Object to reference</param>
        public InterfaceReference(Object target) : base(target) { }

        /// <summary>
        /// Creates an InterfaceReference from an interface implementation.
        /// </summary>
        /// <param name="interface">The interface implementation to reference</param>
        public InterfaceReference(TInterface @interface) : base(@interface) { }
    }
}