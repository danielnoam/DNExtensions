using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetFullscreenPosition : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [Tooltip("If true, the effect will use the default position of the fullscreen image. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultPosition;
        [SerializeField] private Vector3 startPosition = Vector3.zero;
        [SerializeField] private Vector3 endPosition = Vector3.zero;
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
            var position = useDefaultPosition ? VFXManager.Instance.DefaultFullScreenPosition : startPosition;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.LocalPosition(_image.rectTransform, position, endPosition, effectDurationValue, ease, startDelay: startDelay));
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_image)
            {
                _image.rectTransform.localPosition = VFXManager.Instance.DefaultFullScreenPosition;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}