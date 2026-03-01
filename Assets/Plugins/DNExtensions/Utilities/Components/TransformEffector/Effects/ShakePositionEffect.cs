using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Applies a smooth Perlin noise shake to the transform position.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Shake", "Position")]
    [SerializableSelectorTooltip("Applies a smooth Perlin noise shake to the transform position.")]
    public class ShakePositionEffect : PositionEffect
    {
        [SerializeField] private Vector3 shakeAmount = Vector3.one * 0.1f;
        [SerializeField] private float shakeSpeed = 1f;

        private Vector3 _startPosition;
        private Vector3 _timeOffset;

        public override void Initialize(Transform target)
        {
            _startPosition = target.localPosition;
            _timeOffset = new Vector3(
                Random.value * 100f,
                Random.value * 100f,
                Random.value * 100f
            );
        }

        public override void Tick(Transform target)
        {
            float t = Time.time * shakeSpeed;

            Vector3 noise = new Vector3(
                (Mathf.PerlinNoise(t + _timeOffset.x, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(t + _timeOffset.y, 1f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(t + _timeOffset.z, 2f) - 0.5f) * 2f
            );

            target.localPosition = _startPosition + Vector3.Scale(noise, shakeAmount);
        }
    }
}