using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetFullscreenWipe : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        [SerializeField] private WipeDirection startDirection = WipeDirection.OffScreenRight;
        [SerializeField] private WipeDirection endDirection = WipeDirection.Center;
        [SerializeField] private Ease ease = Ease.Linear;
        
        private enum WipeDirection
        {
            Center,
            OffScreenLeft,
            OffScreenRight,
            OffScreenTop,
            OffScreenBottom
        }

        private Image _image;
        private Sequence _sequence;
        private RectTransform _canvasRect;
        
        
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_image)
            {
                _image = VFXManager.Instance.FullScreenImage;
                
                Canvas canvas = _image.GetComponentInParent<Canvas>();
                if (canvas)
                {
                    _canvasRect = canvas.GetComponent<RectTransform>();
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            
            Vector3 startPos = GetPositionForDirection(startDirection);
            Vector3 endPos = GetPositionForDirection(endDirection);
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                        .Group(Tween.LocalPosition(_image.rectTransform, startPos, endPos, effectDurationValue, ease, startDelay: startDelay));
        }

        private Vector3 GetPositionForDirection(WipeDirection direction)
        {
            if (!_canvasRect) return Vector3.zero;
            
            float screenWidth = _canvasRect.rect.width;
            float screenHeight = _canvasRect.rect.height;
            
            switch (direction)
            {
                case WipeDirection.Center:
                    return Vector3.zero;
                    
                case WipeDirection.OffScreenLeft:
                    return new Vector3(-screenWidth, 0, 0);
                    
                case WipeDirection.OffScreenRight:
                    return new Vector3(screenWidth, 0, 0);
                    
                case WipeDirection.OffScreenTop:
                    return new Vector3(0, screenHeight, 0);
                    
                case WipeDirection.OffScreenBottom:
                    return new Vector3(0, -screenHeight, 0);
                    
                default:
                    return Vector3.zero;
            }
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