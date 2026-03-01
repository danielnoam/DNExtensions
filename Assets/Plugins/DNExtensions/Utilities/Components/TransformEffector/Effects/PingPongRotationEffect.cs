using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Oscillates the transform rotation back and forth on any combination of axes using a sine wave.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Ping Pong Rotation", "Rotation")]
    [SerializableSelectorTooltip("Oscillates the transform rotation back and forth on any combination of axes using a sine wave.")]
    public class PingPongRotationEffect : RotationEffect
    {
        [SerializeField] private Vector3 oscillationAmount = Vector3.one;
        [SerializeField] private Vector3 oscillationSpeed = Vector3.up;

        private Quaternion _initialRotation = Quaternion.identity;
        private Vector3 _time;

        public override void Initialize(Transform target)
        {
            _initialRotation = target.rotation;
            _time = new Vector3(
                Random.value * Mathf.PI * 2f,
                Random.value * Mathf.PI * 2f,
                Random.value * Mathf.PI * 2f
            );
        }

        public override void Tick(Transform target)
        {
            _time += oscillationSpeed * Time.deltaTime;

            Vector3 angles = new Vector3(
                Mathf.Sin(_time.x) * oscillationAmount.x,
                Mathf.Sin(_time.y) * oscillationAmount.y,
                Mathf.Sin(_time.z) * oscillationAmount.z
            );

            target.rotation = _initialRotation * Quaternion.Euler(angles);
        }
    }
}