using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.Utilities.VFXManager
{
    [Serializable]
    public class SetVignette : EffectBase
    {
        [SerializeField] private PropertyAnimation<float> intensity = new PropertyAnimation<float>
        {
            animate = true,
            useDefault = false,
            startValue = 0f,
            endValue = 0.5f,
            ease = Ease.Linear
        };
        
        [SerializeField] private PropertyAnimation<float> smoothness = new PropertyAnimation<float>
        {
            animate = false,
            useDefault = false,
            startValue = 0.2f,
            endValue = 0.2f,
            ease = Ease.Linear
        };
        
        [SerializeField] private PropertyAnimation<Vector2> center = new PropertyAnimation<Vector2>
        {
            animate = false,
            useDefault = false,
            startValue = new Vector2(0.5f, 0.5f),
            endValue = new Vector2(0.5f, 0.5f),
            ease = Ease.Linear
        };
        
        [Tooltip("If true, will use the default roundness value from VFXManager")]
        [SerializeField] private bool useDefaultRoundness = true;
        [SerializeField] private bool setRounded;

        private Vignette _vignette;
        private Sequence _sequence;
        
        protected override bool Initialize()
        {
            if (!base.Initialize()) return false;
            
            _vignette = VFXManager.Instance.Vignette;
            if (_vignette == null)
            {
                Debug.LogWarning("Vignette not found in post-processing profile. SetVignette effect will not play.", VFXManager.Instance);
                return false;
            }
            
            return true;
        }
        
        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);
            
            if (intensity.animate)
            {
                var intensityStart = intensity.GetStartValue(VFXManager.Instance.DefaultVignetteIntensity);
                _sequence.Group(Tween.Custom(intensityStart, intensity.endValue, effectDuration, 
                    onValueChange: value => _vignette.intensity.value = value, 
                    ease: intensity.ease, 
                    startDelay: startDelay));
            }
            
            if (smoothness.animate)
            {
                var smoothnessStart = smoothness.GetStartValue(VFXManager.Instance.DefaultVignetteSmoothness);
                _sequence.Group(Tween.Custom(smoothnessStart, smoothness.endValue, effectDuration, 
                    onValueChange: value => _vignette.smoothness.value = value, 
                    ease: smoothness.ease, 
                    startDelay: startDelay));
            }
            
            if (center.animate)
            {
                var centerStart = center.GetStartValue(VFXManager.Instance.DefaultVignetteCenter);
                _sequence.Group(Tween.Custom(centerStart, center.endValue, effectDuration, 
                    onValueChange: value => _vignette.center.value = value, 
                    ease: center.ease, 
                    startDelay: startDelay));
            }
            
            _vignette.rounded.value = useDefaultRoundness 
                ? VFXManager.Instance.DefaultVignetteRounded 
                : setRounded;
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            _vignette.intensity.value = VFXManager.Instance.DefaultVignetteIntensity;
            _vignette.smoothness.value = VFXManager.Instance.DefaultVignetteSmoothness;
            _vignette.center.value = VFXManager.Instance.DefaultVignetteCenter;
            _vignette.rounded.value = VFXManager.Instance.DefaultVignetteRounded;
        }
    }
}