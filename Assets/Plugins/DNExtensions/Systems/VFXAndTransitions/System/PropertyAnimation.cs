using System;
using DNExtensions.Utilities.CustomFields;
using PrimeTween;
using UnityEngine;

namespace DNExtensions.Systems.VFXManager
{
    /// <summary>
    /// Encapsulates settings for animating a single property.
    /// When <see cref="startValue"/> is disabled, the default value from VFXManager is used.
    /// </summary>
    [Serializable]
    public struct PropertyAnimation<T>
    {
        [Tooltip("If false, this property will not be animated")]
        public bool animate;
        public OptionalField<T> startValue;
        public T endValue;
        public Ease ease;

        public T GetStartValue(T defaultValue)
        {
            return startValue.isSet ? startValue.Value : defaultValue;
        }
    }
}