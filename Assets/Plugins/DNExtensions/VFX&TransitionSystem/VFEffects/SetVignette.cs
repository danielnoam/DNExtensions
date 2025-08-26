using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.VFXManager
{
    public class SetVignette : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        
        [Header("Intensity")]
        [Tooltip("If true, the effect will use the default intensity of the vignette. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultIntensity;
        [SerializeField, Range(0f, 1f)] private float startIntensity = 0f;
        [SerializeField, Range(0f, 1f)] private float endIntensity = 0.5f;
        [SerializeField] private Ease intensityEase = Ease.Linear;
        
        [Header("Smoothness")]
        [SerializeField] private bool animateSmoothness;
        [Tooltip("If true, the effect will use the default smoothness of the vignette. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultSmoothness;
        [SerializeField, Range(0.01f, 1f)] private float startSmoothness = 0.2f;
        [SerializeField, Range(0.01f, 1f)] private float endSmoothness = 0.2f;
        [SerializeField] private Ease smoothnessEase = Ease.Linear;
        
        [Header("Center")]
        [SerializeField] private bool animateCenter;
        [Tooltip("If true, the effect will use the default center of the vignette. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultCenter;
        [SerializeField] private Vector2 startCenter = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 endCenter = new Vector2(0.5f, 0.5f);
        [SerializeField] private Ease centerEase = Ease.Linear;
        
        [Header("Roundness")]
        [Tooltip("If true, the effect will use the default roundness of the vignette. If false, it will set the roundness to the specified value.")]
        [SerializeField] private bool useDefaultRoundness = true;
        [SerializeField] private bool setRounded;

        
        private Vignette _vignette;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_vignette)
            {
                _vignette = VFXManager.Instance.Vignette;
                if (!_vignette)
                {
                    Debug.LogWarning("Vignette component not found in VFXManager!");
                    return;
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);
            
            // Intensity animation
            var intensity = useDefaultIntensity ? VFXManager.Instance.DefaultVignetteIntensity : startIntensity;
            _sequence.Group(Tween.Custom(intensity, endIntensity, effectDurationValue, 
                onValueChange: value => _vignette.intensity.value = value, 
                ease: intensityEase, startDelay: startDelay));
            
            // Smoothness animation
            if (animateSmoothness)
            {
                var smoothness = useDefaultSmoothness ? VFXManager.Instance.DefaultVignetteSmoothness : startSmoothness;
                _sequence.Group(Tween.Custom(smoothness, endSmoothness, effectDurationValue, 
                    onValueChange: value => _vignette.smoothness.value = value, 
                    ease: smoothnessEase, startDelay: startDelay));
            }
            
            // Center animation
            if (animateCenter)
            {
                var center = useDefaultCenter ? VFXManager.Instance.DefaultVignetteCenter : startCenter;
                _sequence.Group(Tween.Custom(center, endCenter, effectDurationValue, 
                    onValueChange: value => _vignette.center.value = value, 
                    ease: centerEase, startDelay: startDelay));
            }
            
            // Roundness animation
            _vignette.rounded.value = !useDefaultRoundness ? setRounded : VFXManager.Instance.DefaultVignetteRounded;

        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_vignette)
            {
                _vignette.intensity.value = VFXManager.Instance.DefaultVignetteIntensity;
                _vignette.smoothness.value = VFXManager.Instance.DefaultVignetteSmoothness;
                _vignette.center.value = VFXManager.Instance.DefaultVignetteCenter;
                _vignette.rounded.value = VFXManager.Instance.DefaultVignetteRounded;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}