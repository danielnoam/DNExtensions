using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Punch Color", "Fullscreen")]
    public class PunchFullscreenColor : EffectBase
    {
        [SerializeField] private Color punchColor = Color.black;
        [SerializeField] private Ease easeIn = Ease.Linear;
        [SerializeField] private Ease easeOut = Ease.Linear;

        private Image _image;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (!base.Initialize()) return false;

            _image = VFXManager.Instance.FullScreenImage;
            if (_image == null)
            {
                Debug.LogWarning("FullScreenImage not found in VFXManager. PunchFullscreenColor effect will not play.", VFXManager.Instance);
                return false;
            }

            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var halfDuration = effectDuration * 0.5f;
            var defaultColor = VFXManager.Instance.DefaultFullScreenImage.Color;

            if (_sequence.isAlive) _sequence.Stop();

            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Color(_image, defaultColor, punchColor, halfDuration, easeIn, startDelay: startDelay))
                .Chain(Tween.Color(_image, punchColor, defaultColor, halfDuration, easeOut));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            _image.color = VFXManager.Instance.DefaultFullScreenImage.Color;
        }
    }
}