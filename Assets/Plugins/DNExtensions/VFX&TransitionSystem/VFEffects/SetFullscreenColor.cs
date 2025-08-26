using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetFullscreenColor : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [SerializeField] private EffectType type = EffectType.Transition;
        [Tooltip("The color to start the transition from. Only used for Transition type.")]
        [SerializeField] private Color startColor = Color.clear;
        [SerializeField] private Color endColor = Color.black;
        [SerializeField] private Ease ease = Ease.Linear;
        
        private enum EffectType { Transition, Punch}
        private Image _image;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_image)
            {
                _image = VFXManager.Instance.FullScreenImage;
            }

            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;

            if (_sequence.isAlive) _sequence.Stop();
            switch (type)
            {
                case EffectType.Transition:
                    _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.Color(_image, startColor,endColor, effectDurationValue, ease, startDelay: startDelay));
                    break;
                case EffectType.Punch:
                    _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.Color(_image, endColor, effectDurationValue, ease, startDelay: startDelay))
                        .Group(Tween.Color(_image, VFXManager.Instance.DefaultFullScreenColor, effectDurationValue, ease, startDelay: startDelay + effectDurationValue));
                    break;
            }

        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_image)
            {
                _image.color = VFXManager.Instance.DefaultFullScreenColor;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}