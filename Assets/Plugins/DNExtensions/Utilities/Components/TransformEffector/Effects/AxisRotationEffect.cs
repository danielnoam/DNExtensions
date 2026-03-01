using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Continuously spins the transform on any combination of axes.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Axis Rotation", "Rotation")]
    [SerializableSelectorTooltip("Continuously spins the transform on any combination of axes.")]
    public class AxisRotationEffect : RotationEffect
    {
        [SerializeField] private Vector3 rotationSpeed = Vector3.one;

        private Quaternion _initialRotation = Quaternion.identity;
        private Vector3 _continuousAngles;

        public override void Initialize(Transform target)
        {
            _initialRotation = target.rotation;
            _continuousAngles = Vector3.zero;
        }

        public override void Tick(Transform target)
        {
            _continuousAngles += rotationSpeed * Time.deltaTime;
            target.rotation = _initialRotation * Quaternion.Euler(_continuousAngles);
        }
    }
}