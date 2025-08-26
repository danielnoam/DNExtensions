using System;
using UnityEngine;
using PrimeTween;
using UnityEditor;
using UnityEngine.UI;


namespace DNExtensions.MenuSystem
{
    [Serializable]

    public class AnimatedPageObject
    {

        [Header("Animation")]
        [Min(0.1f)] public float duration = 1f;

        [Header("Alpha")]
        [SerializeField] private bool animateAlpha;
        [SerializeField] private float startAlpha;
        [SerializeField] private float endAlpha = 1;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Position")]
        [SerializeField] private bool animatePosition;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 endPosition;
        [SerializeField] private Ease positionEase = Ease.Default;
        [SerializeField] private RectTransform animatedObject;
        
        
        private Sequence _animationSequence;


        public void Animate(float delay = 0)
        {
            
            if (_animationSequence.isAlive) _animationSequence.Stop();

            _animationSequence = Sequence.Create(useUnscaledTime: true);

            if (animatePosition && animatedObject)
            {
                _animationSequence.Group(Tween.UIAnchoredPosition3D(animatedObject, startPosition, endPosition, duration, positionEase, startDelay: delay));
            }

            if (animateAlpha&& canvasGroup)
            {
                _animationSequence.Group(Tween.Alpha(canvasGroup, startAlpha, endAlpha, duration));
            }
        }

        public void Reverse(float delay = 0)
        {
            if (_animationSequence.isAlive) _animationSequence.Stop();

            _animationSequence = Sequence.Create(useUnscaledTime: true);

            
            if (animatePosition && animatedObject)
            {
                _animationSequence.Group(Tween.UIAnchoredPosition3D(animatedObject, endPosition, startPosition, duration, positionEase, startDelay: delay));
            }
            if (animateAlpha&& canvasGroup)
            {
                _animationSequence.Group(Tween.Alpha(canvasGroup, endAlpha, startAlpha, duration));
            }
        }
    }
}