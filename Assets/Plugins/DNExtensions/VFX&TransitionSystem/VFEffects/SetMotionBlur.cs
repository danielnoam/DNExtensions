using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DNExtensions.VFXManager
{
    public class SetMotionBlur : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        
        [Header("Intensity")]
        [Tooltip("If true, the effect will use the default intensity of the motion blur. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultIntensity;
        [SerializeField, Range(0f, 1f)] private float startIntensity = 0f;
        [SerializeField, Range(0f, 1f)] private float endIntensity = 0.5f;
        [SerializeField] private Ease intensityEase = Ease.Linear;
        
        [Header("Clamp")]
        [SerializeField] private bool animateClamp;
        [Tooltip("If true, the effect will use the default clamp of the motion blur. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultClamp;
        [SerializeField, Range(0f, 0.2f)] private float startClamp = 0.05f;
        [SerializeField, Range(0f, 0.2f)] private float endClamp = 0.05f;
        [SerializeField] private Ease clampEase = Ease.Linear;
        

        private MotionBlur _motionBlur;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_motionBlur)
            {
                _motionBlur = VFXManager.Instance.MotionBlur;
                if (!_motionBlur)
                {
                    Debug.LogWarning("MotionBlur component not found in VFXManager!");
                    return;
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true);
            
            // Intensity animation
            var intensity = useDefaultIntensity ? VFXManager.Instance.DefaultMotionBlurIntensity : startIntensity;
            _sequence.Group(Tween.Custom(intensity, endIntensity, effectDurationValue, 
                onValueChange: value => _motionBlur.intensity.value = value, 
                ease: intensityEase, startDelay: startDelay));
            
            // Clamp animation
            if (animateClamp)
            {
                var clamp = useDefaultClamp ? VFXManager.Instance.DefaultMotionBlurClamp : startClamp;
                _sequence.Group(Tween.Custom(clamp, endClamp, effectDurationValue, 
                    onValueChange: value => _motionBlur.clamp.value = value, 
                    ease: clampEase, startDelay: startDelay));
            }
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_motionBlur)
            {
                _motionBlur.intensity.value = VFXManager.Instance.DefaultMotionBlurIntensity;
                _motionBlur.clamp.value = VFXManager.Instance.DefaultMotionBlurClamp;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}