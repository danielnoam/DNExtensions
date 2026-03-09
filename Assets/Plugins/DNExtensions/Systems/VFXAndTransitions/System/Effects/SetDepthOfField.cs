using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Set Depth Of Field", "Post Processing")]
    public class SetDepthOfField : EffectBase
    {
        [SerializeField] private PropertyAnimation<float> focusDistance = new PropertyAnimation<float>
        {
            animate = true,
            endValue = 10f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> aperture = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 5.6f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> focalLength = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 50f,
            ease = Ease.Linear
        };

        private DepthOfField _depthOfField;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (_depthOfField) return true;
            _depthOfField = VFXManager.Instance.DepthOfField;
            if (!_depthOfField)
            {
                Debug.LogWarning("DepthOfField not found in post-processing profile. SetDepthOfField effect will not play.", VFXManager.Instance);
                return false;
            }
            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var defaults = VFXManager.Instance.DefaultDepthOfField;

            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);

            if (focusDistance.animate)
                _sequence.Group(Tween.Custom(focusDistance.GetStartValue(defaults.FocusDistance), focusDistance.endValue, effectDuration,
                    onValueChange: value => _depthOfField.focusDistance.value = value,
                    ease: focusDistance.ease, startDelay: startDelay));

            if (aperture.animate)
                _sequence.Group(Tween.Custom(aperture.GetStartValue(defaults.Aperture), aperture.endValue, effectDuration,
                    onValueChange: value => _depthOfField.aperture.value = value,
                    ease: aperture.ease, startDelay: startDelay));

            if (focalLength.animate)
                _sequence.Group(Tween.Custom(focalLength.GetStartValue(defaults.FocalLength), focalLength.endValue, effectDuration,
                    onValueChange: value => _depthOfField.focalLength.value = value,
                    ease: focalLength.ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            VFXManager.Instance.DefaultDepthOfField.ApplyTo(_depthOfField);
        }
    }
}