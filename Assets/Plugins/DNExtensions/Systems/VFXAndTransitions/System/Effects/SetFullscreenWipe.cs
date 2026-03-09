using System;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    [SerializableSelectorName("Wipe", "Fullscreen")]
    public class SetFullscreenWipe : EffectBase
    {
        private enum WipeDirection
        {
            Center,
            OffScreenLeft,
            OffScreenRight,
            OffScreenTop,
            OffScreenBottom
        }

        [SerializeField] private WipeDirection startDirection = WipeDirection.OffScreenRight;
        [SerializeField] private WipeDirection endDirection = WipeDirection.Center;
        [SerializeField] private Ease ease = Ease.Linear;

        private Image _image;
        private RectTransform _canvasRect;
        private Sequence _sequence;

        protected override bool Initialize()
        {
            if (_image) return true;

            _image = VFXManager.Instance.FullScreenImage;
            if (!_image)
            {
                Debug.LogWarning("FullScreenImage not found in VFXManager. SetFullscreenWipe effect will not play.", VFXManager.Instance);
                return false;
            }

            var canvas = _image.GetComponentInParent<Canvas>();
            if (!canvas)
            {
                Debug.LogWarning("No Canvas found above FullScreenImage. SetFullscreenWipe effect will not play.", VFXManager.Instance);
                return false;
            }

            _canvasRect = canvas.GetComponent<RectTransform>();
            return true;
        }

        protected override void OnPlayEffect(float sequenceDuration)
        {
            var startDelay = GetStartDelay(sequenceDuration);
            var effectDuration = GetEffectDuration(sequenceDuration);
            var startPos = GetPositionForDirection(startDirection);
            var endPos = GetPositionForDirection(endDirection);

            if (_sequence.isAlive) _sequence.Stop();

            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.LocalPosition(_image.rectTransform, startPos, endPos, effectDuration, ease, startDelay: startDelay));
        }

        protected override void OnResetEffect()
        {
            if (_sequence.isAlive) _sequence.Stop();
            _image.rectTransform.localPosition = VFXManager.Instance.DefaultFullScreenImage.LocalPosition;
        }

        private Vector3 GetPositionForDirection(WipeDirection direction)
        {
            var screenWidth = _canvasRect.rect.width;
            var screenHeight = _canvasRect.rect.height;

            return direction switch
            {
                WipeDirection.OffScreenLeft   => new Vector3(-screenWidth, 0f, 0f),
                WipeDirection.OffScreenRight  => new Vector3(screenWidth, 0f, 0f),
                WipeDirection.OffScreenTop    => new Vector3(0f, screenHeight, 0f),
                WipeDirection.OffScreenBottom => new Vector3(0f, -screenHeight, 0f),
                _                             => Vector3.zero
            };
        }
    }
}