using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Set Lens Distortion", "Post Processing")]
    public class SetLensDistortion : EffectBase
    {
        [SerializeField] private PropertyAnimation<float> intensity = new PropertyAnimation<float>
        {
            animate = true,
            endValue = 0.5f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> xMultiplier = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 1f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> yMultiplier = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 1f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<Vector2> center = new PropertyAnimation<Vector2>
        {
            animate = false,
            endValue = new Vector2(0.5f, 0.5f),
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> scale = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 1f,
            ease = Ease.Linear
        };

        private LensDistortion _lensDistortion;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (_lensDistortion) return true;
            _lensDistortion = VFXManager.Instance.LensDistortion;
            if (!_lensDistortion)
            {
                Debug.LogWarning("LensDistortion not found in post-processing profile. SetLensDistortion effect will not play.", VFXManager.Instance);
                return false;
            }
            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var defaults = VFXManager.Instance.DefaultLensDistortion;

            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);

            if (intensity.animate)
                _sequence.Group(Tween.Custom(intensity.GetStartValue(defaults.Intensity), intensity.endValue, effectDuration,
                    onValueChange: value => _lensDistortion.intensity.value = value,
                    ease: intensity.ease, startDelay: startDelay));

            if (xMultiplier.animate)
                _sequence.Group(Tween.Custom(xMultiplier.GetStartValue(defaults.XMultiplier), xMultiplier.endValue, effectDuration,
                    onValueChange: value => _lensDistortion.xMultiplier.value = value,
                    ease: xMultiplier.ease, startDelay: startDelay));

            if (yMultiplier.animate)
                _sequence.Group(Tween.Custom(yMultiplier.GetStartValue(defaults.YMultiplier), yMultiplier.endValue, effectDuration,
                    onValueChange: value => _lensDistortion.yMultiplier.value = value,
                    ease: yMultiplier.ease, startDelay: startDelay));

            if (center.animate)
                _sequence.Group(Tween.Custom(center.GetStartValue(defaults.Center), center.endValue, effectDuration,
                    onValueChange: value => _lensDistortion.center.value = value,
                    ease: center.ease, startDelay: startDelay));

            if (scale.animate)
                _sequence.Group(Tween.Custom(scale.GetStartValue(defaults.Scale), scale.endValue, effectDuration,
                    onValueChange: value => _lensDistortion.scale.value = value,
                    ease: scale.ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            VFXManager.Instance.DefaultLensDistortion.ApplyTo(_lensDistortion);
        }
    }
}