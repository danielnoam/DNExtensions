using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Smoothly scales the transform in and out using a sine wave evaluated through an animation curve.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Breath", "Scale")]
    [SerializableSelectorTooltip("Smoothly scales the transform in and out using a sine wave evaluated through an animation curve.")]
    public class BreathScaleEffect : ScaleEffect
    {
        [SerializeField] private float breatheSpeed = 1f;
        [SerializeField] private Vector3 breatheAmount = Vector3.one * 0.2f;
        [SerializeField] private AnimationCurve breatheCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _initialScale = Vector3.one;
        private float _breatheTime;

        public override void Initialize(Transform target)
        {
            _initialScale = target.localScale;
            _breatheTime = Random.value * Mathf.PI * 2f;
        }

        public override void Tick(Transform target)
        {
            _breatheTime += breatheSpeed * Time.deltaTime;

            float normalizedCycle = (Mathf.Sin(_breatheTime) + 1f) * 0.5f;
            float t = breatheCurve.Evaluate(normalizedCycle);

            target.localScale = Vector3.Scale(_initialScale, Vector3.one + breatheAmount * t);
        }
    }
}