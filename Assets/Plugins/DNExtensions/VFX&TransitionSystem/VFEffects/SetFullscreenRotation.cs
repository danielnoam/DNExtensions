using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetFullscreenRotation : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [Tooltip("If true, the effect will use the default rotation of the fullscreen image. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultRotation;
        [SerializeField] private Vector3 startRotation = Vector3.zero;
        [SerializeField] private Vector3 endRotation = Vector3.zero;
        [SerializeField] private Ease ease = Ease.Linear;
        

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
            var rotation = useDefaultRotation ? VFXManager.Instance.DefaultFullScreenRotation : startRotation;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.LocalEulerAngles(_image.rectTransform, rotation, endRotation, effectDurationValue, ease, startDelay: startDelay));
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_image)
            {
                _image.rectTransform.localEulerAngles = VFXManager.Instance.DefaultFullScreenRotation;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}