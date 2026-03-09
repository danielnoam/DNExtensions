using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Set Panini Projection", "Post Processing")]
    public class SetPaniniProjection : EffectBase
    {
        [SerializeField] private PropertyAnimation<float> distance = new PropertyAnimation<float>
        {
            animate = true,
            endValue = 1f,
            ease = Ease.Linear
        };
        [SerializeField] private PropertyAnimation<float> cropToFit = new PropertyAnimation<float>
        {
            animate = false,
            endValue = 1f,
            ease = Ease.Linear
        };

        private PaniniProjection _paniniProjection;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (_paniniProjection) return true;
            _paniniProjection = VFXManager.Instance.PaniniProjection;
            if (!_paniniProjection)
            {
                Debug.LogWarning("PaniniProjection not found in post-processing profile. SetPaniniProjection effect will not play.", VFXManager.Instance);
                return false;
            }
            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var defaults = VFXManager.Instance.DefaultPaniniProjection;

            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);

            if (distance.animate)
                _sequence.Group(Tween.Custom(distance.GetStartValue(defaults.Distance), distance.endValue, effectDuration,
                    onValueChange: value => _paniniProjection.distance.value = value,
                    ease: distance.ease, startDelay: startDelay));

            if (cropToFit.animate)
                _sequence.Group(Tween.Custom(cropToFit.GetStartValue(defaults.CropToFit), cropToFit.endValue, effectDuration,
                    onValueChange: value => _paniniProjection.cropToFit.value = value,
                    ease: cropToFit.ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            VFXManager.Instance.DefaultPaniniProjection.ApplyTo(_paniniProjection);
        }
    }
}