using System;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Fade Color", "Fullscreen")]
    public class SetFullscreenColor : EffectBase
    {
        [Tooltip("When enabled, overrides the transition start color. Otherwise uses the image's current color.")]
        [SerializeField] private OptionalField<Color> overrideStartColor = new OptionalField<Color>(Color.clear, true);
        [SerializeField] private Color endColor = Color.black;
        [SerializeField] private Ease ease = Ease.Linear;


        private Image _image;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (!base.Initialize()) return false;

            _sequence.Stop();
            _image = VFXManager.Instance.FullScreenImage;
            if (!_image)
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
            var startColor = overrideStartColor.isSet ? overrideStartColor.Value : _image.color;

            if (_sequence.isAlive) _sequence.Stop();

            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Color(_image, startColor, endColor, effectDuration, ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            _image.color = VFXManager.Instance.DefaultFullScreenImage.Color;
        }
    }
}