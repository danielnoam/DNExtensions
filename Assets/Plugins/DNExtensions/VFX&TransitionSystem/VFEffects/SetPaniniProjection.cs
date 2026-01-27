using System;
using DNExtensions.Utilities.RangedValues;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Utilities.VFXManager
{
    [Serializable]
    public class SetPaniniProjection : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        
        [Header("Distance")]
        [Tooltip("If true, the effect will use the default distance of the panini projection. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultDistance;
        [SerializeField, Range(0f, 1f)] private float startDistance = 0f;
        [SerializeField, Range(0f, 1f)] private float endDistance = 0.5f;
        [SerializeField] private Ease distanceEase = Ease.Linear;
        
        [Header("Crop To Fit")]
        [SerializeField] private bool animateCropToFit;
        [Tooltip("If true, the effect will use the default crop to fit of the panini projection. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultCropToFit;
        [SerializeField, Range(0f, 1f)] private float startCropToFit = 1f;
        [SerializeField, Range(0f, 1f)] private float endCropToFit = 1f;
        [SerializeField] private Ease cropToFitEase = Ease.Linear;

        
        private PaniniProjection _paniniProjection;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_paniniProjection)
            {
                _paniniProjection = VFXManager.Instance.PaniniProjection;
                if (!_paniniProjection)
                {
                    Debug.LogWarning("PaniniProjection component not found in VFXManager!");
                    return;
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);
            
            // Distance animation
            var distance = useDefaultDistance ? VFXManager.Instance.DefaultPaniniProjectionDistance : startDistance;
            _sequence.Group(Tween.Custom(distance, endDistance, effectDurationValue, 
                onValueChange: value => _paniniProjection.distance.value = value, 
                ease: distanceEase, startDelay: startDelay));
            
            // Crop To Fit animation
            if (animateCropToFit)
            {
                var cropToFit = useDefaultCropToFit ? VFXManager.Instance.DefaultPaniniProjectionCropToFit : startCropToFit;
                _sequence.Group(Tween.Custom(cropToFit, endCropToFit, effectDurationValue, 
                    onValueChange: value => _paniniProjection.cropToFit.value = value, 
                    ease: cropToFitEase, startDelay: startDelay));
            }
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_paniniProjection)
            {
                _paniniProjection.distance.value = VFXManager.Instance.DefaultPaniniProjectionDistance;
                _paniniProjection.cropToFit.value = VFXManager.Instance.DefaultPaniniProjectionCropToFit;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}