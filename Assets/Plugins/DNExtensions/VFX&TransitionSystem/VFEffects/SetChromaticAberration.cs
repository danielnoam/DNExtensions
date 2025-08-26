using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.VFXManager
{
    public class SetChromaticAberration : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [Tooltip("If true, the effect will use the default intensity of the chromatic aberration. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultIntensity;
        [SerializeField, Range(0f, 1f)] private float startIntensity = 0f;
        [SerializeField, Range(0f, 1f)] private float endIntensity = 0.5f;
        [SerializeField] private Ease ease = Ease.Linear;
        

        private ChromaticAberration _chromaticAberration;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_chromaticAberration)
            {
                _chromaticAberration = VFXManager.Instance.ChromaticAberration;
                if (!_chromaticAberration)
                {
                    Debug.LogWarning("ChromaticAberration component not found in VFXManager!");
                    return;
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            var intensity = useDefaultIntensity ? _chromaticAberration.intensity.value : startIntensity;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.Custom(intensity, endIntensity, effectDurationValue, 
                            onValueChange: value => _chromaticAberration.intensity.value = value, 
                            ease: ease, startDelay: startDelay));
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_chromaticAberration)
            {
                _chromaticAberration.intensity.value = VFXManager.Instance.DefaultChromaticAberrationIntensity;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}