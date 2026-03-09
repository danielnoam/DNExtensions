using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Set Motion Blur", "Post Processing")]
    public class SetMotionBlur : EffectBase
    {
        [SerializeField] private PropertyAnimation<float> intensity = new PropertyAnimation<float>
        {
            animate = true,
            endValue = 1f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> clamp = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 0.05f,
            ease = Ease.Linear
        };

        private MotionBlur _motionBlur;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (_motionBlur) return true;
            _motionBlur = VFXManager.Instance.MotionBlur;
            if (!_motionBlur)
            {
                Debug.LogWarning("MotionBlur not found in post-processing profile. SetMotionBlur effect will not play.", VFXManager.Instance);
                return false;
            }
            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var defaults = VFXManager.Instance.DefaultMotionBlur;

            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);

            if (intensity.animate)
                _sequence.Group(Tween.Custom(intensity.GetStartValue(defaults.Intensity), intensity.endValue, effectDuration,
                    onValueChange: value => _motionBlur.intensity.value = value,
                    ease: intensity.ease, startDelay: startDelay));

            if (clamp.animate)
                _sequence.Group(Tween.Custom(clamp.GetStartValue(defaults.Clamp), clamp.endValue, effectDuration,
                    onValueChange: value => _motionBlur.clamp.value = value,
                    ease: clamp.ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            VFXManager.Instance.DefaultMotionBlur.ApplyTo(_motionBlur);
        }
    }
}