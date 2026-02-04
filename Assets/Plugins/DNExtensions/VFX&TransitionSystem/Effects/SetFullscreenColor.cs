using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    [Serializable]
    public class SetFullscreenColor : EffectBase
    {
        [SerializeField] private EffectType type = EffectType.Transition;
        
        [Tooltip("The color to start the transition from. Only used for Transition type.")]
        [SerializeField] private Color startColor = Color.clear;
        [SerializeField] private Color endColor = Color.black;
        [SerializeField] private Ease ease = Ease.Linear;
        
        private enum EffectType 
        { 
            Transition, 
            Punch
        }
        
        private Image _image;
        private Sequence _sequence;
        
        protected override bool Initialize()
        {
            if (!base.Initialize()) return false;
            
            _image = VFXManager.Instance.FullScreenImage;
            if (_image == null)
            {
                Debug.LogWarning("FullScreenImage not found in VFXManager. SetFullscreenColor effect will not play.", VFXManager.Instance);
                return false;
            }
            
            return true;
        }
        
        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);

            if (_sequence.isAlive) _sequence.Stop();
            
            switch (type)
            {
                case EffectType.Transition:
                    _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.Color(_image, startColor, endColor, effectDuration, ease, startDelay: startDelay));
                    break;
                    
                case EffectType.Punch:
                    _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.Color(_image, endColor, effectDuration, ease, startDelay: startDelay))
                        .Group(Tween.Color(_image, VFXManager.Instance.DefaultFullScreenColor, effectDuration, ease, startDelay: startDelay + effectDuration));
                    break;
            }
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            _image.color = VFXManager.Instance.DefaultFullScreenColor;
        }
    }
}