
using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;


namespace DNExtensions.MenuSystem
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class Screen : MonoBehaviour
    {
        [Header("Animations")]
        [SerializeReference, SerializableSelector] 
        private List<ScreenAnimation> showAnimations;
        [SerializeReference, SerializableSelector]
        private List<ScreenAnimation> hideAnimations;

        private Sequence _animationSequence;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        public Vector3 TransformOriginalAnchoredPosition { get; private set; }
        public Vector3 TransformOriginalScale { get; private set; }
        public CanvasGroup CanvasGroup => _canvasGroup ? _canvasGroup : _canvasGroup = GetComponent<CanvasGroup>();
        public RectTransform RectTransform => _rectTransform ? _rectTransform : _rectTransform = GetComponent<RectTransform>();



        private void Awake()
        {
            _canvasGroup = this.GetOrAddComponent<CanvasGroup>();
            _rectTransform = this.GetOrAddComponent<RectTransform>();
            
            TransformOriginalScale = RectTransform.localScale;
            TransformOriginalAnchoredPosition = RectTransform.anchoredPosition3D;
        }

        public void Show(bool animated = true, Action onComplete = null)
        {
            gameObject.SetActive(true);

            if (!animated || showAnimations == null || showAnimations.Count == 0)
            {
                ShowInstant();
                onComplete?.Invoke();
                return;
            }

            if (_animationSequence.isAlive)
            {
                _animationSequence.Stop();
            }

            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;

            _animationSequence = Sequence.Create();

            foreach (var anim in showAnimations)
            {
                if (anim == null) continue;
                var sequence = anim.CreateSequence(this);
                _animationSequence.Group(sequence);
            }

            _animationSequence.ChainCallback(() =>
            {
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
                onComplete?.Invoke();
            });
        }

        public void Hide(bool animated = true, Action onComplete = null)
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;

            if (!animated || hideAnimations == null || hideAnimations.Count == 0)
            {
                HideInstant();
                onComplete?.Invoke();
                return;
            }

            if (_animationSequence.isAlive)
            {
                _animationSequence.Stop();
            }

            _animationSequence = Sequence.Create();

            foreach (var anim in hideAnimations)
            {
                if (anim == null) continue;
                var sequence = anim.CreateSequence(this);
                _animationSequence.Group(sequence);
            }

            _animationSequence.ChainCallback(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        private void ShowInstant()
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            RectTransform.localScale = TransformOriginalScale;
            RectTransform.anchoredPosition3D = TransformOriginalAnchoredPosition;
        }

        private void HideInstant()
        {
            CanvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }


}