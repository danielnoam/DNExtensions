using System;
using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;

namespace DNExtensions.Systems.MenuSystem
{
    /// <summary>
    /// Represents a UI screen with show/hide animations and transition management.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("DNExtensions/Menu System/Screen")]
    public class Screen : MonoBehaviour
    {
        [Header("Animations")]
        [SerializeField] private bool disableInteractionsDuringAnimation = true;
        [SerializeReference, SerializableSelector] private List<ScreenAnimation> showAnimations;
        [SerializeReference, SerializableSelector] private List<ScreenAnimation> hideAnimations;

        private Sequence _animationSequence;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        public Vector3 TransformOriginalAnchoredPosition { get; private set; }
        public Vector3 TransformOriginalScale { get; private set; }
        public CanvasGroup CanvasGroup => _canvasGroup ? _canvasGroup : _canvasGroup = GetComponent<CanvasGroup>();
        public RectTransform RectTransform => _rectTransform ? _rectTransform : _rectTransform = GetComponent<RectTransform>();

        public event Action OnShowComplete;
        public event Action OnHideComplete;

        private void Awake()
        {
            _canvasGroup = this.GetOrAddComponent<CanvasGroup>();
            _rectTransform = this.GetOrAddComponent<RectTransform>();

            TransformOriginalScale = RectTransform.localScale;
            TransformOriginalAnchoredPosition = RectTransform.anchoredPosition3D;
        }

        /// <summary>
        /// Shows the screen with optional animation and completion callback.
        /// </summary>
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
            CanvasGroup.interactable = !disableInteractionsDuringAnimation;
            CanvasGroup.blocksRaycasts = false;

            _animationSequence = Sequence.Create();

            foreach (var anim in showAnimations)
            {
                if (anim == null) continue;
                _animationSequence.Group(anim.CreateSequence(this));
            }

            _animationSequence.ChainCallback(() =>
            {
                onComplete?.Invoke();
                ShowInstant();
            });
        }

        /// <summary>
        /// Hides the screen with optional animation and completion callback.
        /// </summary>
        public void Hide(bool animated = true, Action onComplete = null)
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;

            if (!animated || hideAnimations == null || hideAnimations.Count == 0)
            {
                onComplete?.Invoke();
                HideInstant();
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
                _animationSequence.Group(anim.CreateSequence(this));
            }

            _animationSequence.ChainCallback(() =>
            {
                onComplete?.Invoke();
                HideInstant();
            });
        }

        private void ShowInstant()
        {
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            RectTransform.localScale = TransformOriginalScale;
            RectTransform.anchoredPosition3D = TransformOriginalAnchoredPosition;
            OnShowComplete?.Invoke();
        }

        private void HideInstant()
        {
            CanvasGroup.alpha = 0f;
            OnHideComplete?.Invoke();
            gameObject.SetActive(false);
        }
    }
}