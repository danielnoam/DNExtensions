using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Scales the transform using an animation curve cycling on a cooldown timer.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Pulse", "Scale")]
    [SerializableSelectorTooltip("Scales the transform using an animation curve cycling on a cooldown timer.")]
    public class PulseScaleEffect : ScaleEffect
    {
        [SerializeField] private float pulseCooldown = 1f;
        [SerializeField] private Vector3 pulseAmount = Vector3.one * 0.2f;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _initialScale = Vector3.one;
        private float _pulseTimer;

        public override void Initialize(Transform target)
        {
            _initialScale = target.localScale;
            _pulseTimer = 0f;
        }

        public override void Tick(Transform target)
        {
            _pulseTimer += Time.deltaTime;

            if (_pulseTimer >= pulseCooldown)
                _pulseTimer = 0f;

            float t = pulseCurve.Evaluate(_pulseTimer / pulseCooldown);

            target.localScale = Vector3.Scale(_initialScale, Vector3.one + pulseAmount * t);
        }
    }
}