using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Set Chromatic Aberration", "Post Processing")]
    public class SetChromaticAberration : EffectBase
    {
        [SerializeField] private PropertyAnimation<float> intensity = new PropertyAnimation<float>
        {
            animate = true,
            endValue = 1f,
            ease = Ease.Linear
        };

        private ChromaticAberration _chromaticAberration;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (_chromaticAberration) return true;
            _chromaticAberration = VFXManager.Instance.ChromaticAberration;
            if (!_chromaticAberration)
            {
                Debug.LogWarning("ChromaticAberration not found in post-processing profile. SetChromaticAberration effect will not play.", VFXManager.Instance);
                return false;
            }
            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var defaults = VFXManager.Instance.DefaultChromaticAberration;

            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);

            if (intensity.animate)
                _sequence.Group(Tween.Custom(intensity.GetStartValue(defaults.Intensity), intensity.endValue, effectDuration,
                    onValueChange: value => _chromaticAberration.intensity.value = value,
                    ease: intensity.ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            VFXManager.Instance.DefaultChromaticAberration.ApplyTo(_chromaticAberration);
        }
    }
}