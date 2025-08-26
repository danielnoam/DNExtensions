using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetIconShake : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [Tooltip("The strength of the shake effect.")]
        [SerializeField] private Vector3 shakeStrength = new Vector3(10f, 10f, 0f);
        [Tooltip("Number of frequency during the shake.")]
        [SerializeField, Min(1)] private int frequency = 10;
        [Tooltip("Whether to use snapping for pixel-perfect movement.")]
        [SerializeField] private bool snapping = false;
        [SerializeField] private Ease ease = Ease.OutQuad;
        

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
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.PunchLocalPosition(_image.rectTransform, shakeStrength, effectDurationValue, frequency, snapping, ease, startDelay: startDelay));
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_image)
            {
                _image.rectTransform.localPosition =  VFXManager.Instance.DefaultIconPosition;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}