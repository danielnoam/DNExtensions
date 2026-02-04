using System;
using PrimeTween;
using UnityEngine;

namespace DNExtensions.VFXManager
{
    /// <summary>
    /// Encapsulates settings for animating a single property
    /// </summary>
    [Serializable]
    public struct PropertyAnimation<T>
    {
        [Tooltip("If false, this property will not be animated")]
        public bool animate;
        
        [Tooltip("If true, will use the default value from VFXManager as the start value")]
        public bool useDefault;
        
        public T startValue;
        public T endValue;
        public Ease ease;
        
        /// <summary>
        /// Gets the start value, using default if specified
        /// </summary>
        public T GetStartValue(T defaultValue)
        {
            return useDefault ? defaultValue : startValue;
        }
    }
}