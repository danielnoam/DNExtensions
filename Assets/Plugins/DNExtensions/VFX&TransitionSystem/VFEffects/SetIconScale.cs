using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetIconScale : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [Tooltip("If true, the effect will use the default scale of the icon image. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultScale;
        [SerializeField] private Vector3 startScale = Vector3.one;
        [SerializeField] private Vector3 endScale = Vector3.one;
        [SerializeField] private Ease ease = Ease.Linear;
        

        private Image _image;
        private Sequence _sequence;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_image)
            {
                _image = VFXManager.Instance.IconImage;
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            var scale = useDefaultScale ? VFXManager.Instance.DefaultIconScale : startScale;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Scale(_image.rectTransform, scale, endScale, effectDurationValue, ease, startDelay: startDelay));
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_image)
            {
                _image.rectTransform.localScale = VFXManager.Instance.DefaultIconScale;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}