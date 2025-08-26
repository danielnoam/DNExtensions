using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.VFXManager
{
    public class SetLensDistortion : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        
        [Header("Intensity")]
        [Tooltip("If true, the effect will use the default intensity of the lens distortion. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultIntensity;
        [SerializeField, Range(-1f, 1f)] private float startIntensity = 0f;
        [SerializeField, Range(-1f, 1f)] private float endIntensity = 0.5f;
        [SerializeField] private Ease intensityEase = Ease.Linear;
        
        [Header("X Multiplier")]
        [SerializeField] private bool animateXMultiplier;
        [Tooltip("If true, the effect will use the default X multiplier of the lens distortion. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultXMultiplier;
        [SerializeField, Range(0f, 1f)] private float startXMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float endXMultiplier = 1f;
        [SerializeField] private Ease xMultiplierEase = Ease.Linear;
        
        [Header("Y Multiplier")]
        [SerializeField] private bool animateYMultiplier;
        [Tooltip("If true, the effect will use the default Y multiplier of the lens distortion. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultYMultiplier;
        [SerializeField, Range(0f, 1f)] private float startYMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float endYMultiplier = 1f;
        [SerializeField] private Ease yMultiplierEase = Ease.Linear;
        
        [Header("Center")]
        [SerializeField] private bool animateCenter;
        [Tooltip("If true, the effect will use the default center of the lens distortion. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultCenter;
        [SerializeField] private Vector2 startCenter = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 endCenter = new Vector2(0.5f, 0.5f);
        [SerializeField] private Ease centerEase = Ease.Linear;
        
        [Header("Scale")]
        [SerializeField] private bool animateScale;
        [Tooltip("If true, the effect will use the default scale of the lens distortion. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultScale;
        [SerializeField, Range(0.01f, 5f)] private float startScale = 1f;
        [SerializeField, Range(0.01f, 5f)] private float endScale = 1f;
        [SerializeField] private Ease scaleEase = Ease.Linear;
        

        private LensDistortion _lensDistortion;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_lensDistortion)
            {
                _lensDistortion = VFXManager.Instance.LensDistortion;
                if (!_lensDistortion)
                {
                    Debug.LogWarning("LensDistortion component not found in VFXManager!");
                    return;
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create();
            
            // Intensity animation
            var intensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
            _sequence.Group(Tween.Custom(intensity, endIntensity, effectDurationValue, 
                onValueChange: value => _lensDistortion.intensity.value = value, 
                ease: intensityEase, startDelay: startDelay, useUnscaledTime: true));
            
            // X Multiplier animation
            if (animateXMultiplier)
            {
                var xMultiplier = useDefaultXMultiplier ? VFXManager.Instance.DefaultLensDistortionXMultiplier : startXMultiplier;
                _sequence.Group(Tween.Custom(xMultiplier, endXMultiplier, effectDurationValue, 
                    onValueChange: value => _lensDistortion.xMultiplier.value = value, 
                    ease: xMultiplierEase, startDelay: startDelay, useUnscaledTime: true));
            }
            
            // Y Multiplier animation
            if (animateYMultiplier)
            {
                var yMultiplier = useDefaultYMultiplier ? VFXManager.Instance.DefaultLensDistortionYMultiplier : startYMultiplier;
                _sequence.Group(Tween.Custom(yMultiplier, endYMultiplier, effectDurationValue, 
                    onValueChange: value => _lensDistortion.yMultiplier.value = value, 
                    ease: yMultiplierEase, startDelay: startDelay, useUnscaledTime: true));
            }
            
            // Center animation
            if (animateCenter)
            {
                var center = useDefaultCenter ? VFXManager.Instance.DefaultLensDistortionCenter : startCenter;
                _sequence.Group(Tween.Custom(center, endCenter, effectDurationValue, 
                    onValueChange: value => _lensDistortion.center.value = value, 
                    ease: centerEase, startDelay: startDelay, useUnscaledTime: true));
            }
            
            // Scale animation
            if (animateScale)
            {
                var scale = useDefaultScale ? VFXManager.Instance.DefaultLensDistortionScale : startScale;
                _sequence.Group(Tween.Custom(scale, endScale, effectDurationValue, 
                    onValueChange: value => _lensDistortion.scale.value = value, 
                    ease: scaleEase, startDelay: startDelay, useUnscaledTime: true));
            }
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_lensDistortion)
            {
                _lensDistortion.intensity.value = VFXManager.Instance.DefaultLensDistortionIntensity;
                _lensDistortion.xMultiplier.value = VFXManager.Instance.DefaultLensDistortionXMultiplier;
                _lensDistortion.yMultiplier.value = VFXManager.Instance.DefaultLensDistortionYMultiplier;
                _lensDistortion.center.value = VFXManager.Instance.DefaultLensDistortionCenter;
                _lensDistortion.scale.value = VFXManager.Instance.DefaultLensDistortionScale;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}