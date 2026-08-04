using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Abstract base class for all transform effects.
    /// </summary>
    [Serializable]
    [MovedFrom(false, null, "Assembly-CSharp-firstpass", null)]
    public abstract class TransformEffect
    {
        /// <summary>Called once when the effector starts.</summary>
        public abstract void Initialize(Transform target, bool localSpace);

        /// <summary>Called every frame by the effector.</summary>
        public abstract void Tick(Transform target, bool localSpace);
    }

    /// <summary>
    /// Abstract base for effects that modify the transform's position.
    /// </summary>
    [Serializable]
    [SerializableSelectorAllowOnce]
    [MovedFrom(false, null, "Assembly-CSharp-firstpass", null)]
    public abstract class PositionEffect : TransformEffect { }

    /// <summary>
    /// Abstract base for effects that modify the transform's rotation.
    /// </summary>
    [Serializable]
    [SerializableSelectorAllowOnce]
    [MovedFrom(false, null, "Assembly-CSharp-firstpass", null)]
    public abstract class RotationEffect : TransformEffect { }

    /// <summary>
    /// Abstract base for effects that modify the transform's scale.
    /// </summary>
    [Serializable]
    [SerializableSelectorAllowOnce]
    [MovedFrom(false, null, "Assembly-CSharp-firstpass", null)]
    public abstract class ScaleEffect : TransformEffect { }
}