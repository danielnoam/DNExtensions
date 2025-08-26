using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.VFXManager
{
    public class SetFullscreenImage : VFEffectsEffectBase
    {

        [SerializeField] private Sprite sprite;
        [SerializeField] private bool setColor;
        [SerializeField] private Color color = Color.white;
        [Tooltip("Delay before applying the effect, in seconds. Set to 0 for immediate application. Should not exceed the sequence duration.")]
        [SerializeField, Min(0)] private float delay;
        
        private Sequence _sequence;
        private Image _image;
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            _image = VFXManager.Instance.FullScreenImage;
            
            if (!_image)
            {
                Debug.Log("No Fullscreen Image found in the scene!");
                return;
            }
            
            
            if (delay > 0)
            {
                delay = Mathf.Min(delay, sequenceDuration);
                
                if (_sequence.isAlive) _sequence.Stop();
                _sequence = Sequence.Create(useUnscaledTime: true)
                    .ChainDelay(delay)
                    .OnComplete(() =>
                    {
                        if (setColor) _image.color = color;
                        _image.sprite = sprite;
                    });
            }
            else
            {
                _image.sprite = sprite;
                if (setColor)
                {
                    _image.color = color;
                }
            }
            
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_image)
            {
                _image.sprite = VFXManager.Instance.DefaultFullScreenSprite;
                if (setColor)
                {
                    _image.color = VFXManager.Instance.DefaultFullScreenColor;
                }
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
    }
}