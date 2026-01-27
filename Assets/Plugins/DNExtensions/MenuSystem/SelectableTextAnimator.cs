
using UnityEngine;
using TMPro;
using PrimeTween;

namespace DNExtensions.Utilities.MenuSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SelectableTextAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SelectableAnimator selectableAnimator;
        
        [Header("Position")] 
        [SerializeField] private PositionEffectType positionEffectType = PositionEffectType.Shake;
        [ShowIf("IsOffsetMode"), SerializeField] private Vector3 positionOffset = new Vector3(0, 10, 0);
        [ShowIf("IsOffsetMode"), SerializeField] private float positionDuration = 0.15f;
        [ShowIf("IsOffsetMode"), SerializeField] private Ease positionEase = Ease.InOutBounce;
        [ShowIf("IsShakeMode"), SerializeField] private bool shakeOnDeselect;
        [ShowIf("IsShakeMode"), SerializeField] private Vector3 shakeStrength = new Vector3(3, 3, 0);
        [ShowIf("IsShakeMode"), SerializeField] private float shakeFrequency = 10f;
        [ShowIf("IsShakeMode"), SerializeField] private float shakeDuration = 0.5f;
        [ShowIf("IsShakeMode"), SerializeField] private Ease shakeEase = Ease.Default;

        [Header("Rotate")] 
        [SerializeField] private bool animateRotation;
        [SerializeField] private Vector3 rotationOffset = new Vector3(0, 0, 15);
        [SerializeField] private float rotationDuration = 0.15f;
        [SerializeField] private Ease rotationEase = Ease.InOutBounce;

        [Header("Scale")] 
        [SerializeField] private bool animateScale;
        [SerializeField] private float scaleMultiplier = 1.1f;
        [SerializeField] private float scaleDuration = 0.15f;
        [SerializeField] private Ease scaleEase = Ease.InOutBounce;
        

        [Header("Color")]
        [SerializeField] private bool animateColor;
        [SerializeField] private Color selectedColor = Color.white;
        [SerializeField] private float colorDuration = 0.15f;
        [SerializeField] private Ease colorEase = Ease.InOutQuad;

        [Space(10)] 
        [SerializeField, ReadOnly] private TextMeshProUGUI textMeshPro;
        [SerializeField, ReadOnly] private RectTransform rectTransform;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private Color _originalColor;
        
        private bool IsOffsetMode => positionEffectType == PositionEffectType.Offset;
        private bool IsShakeMode => positionEffectType == PositionEffectType.Shake;

        private enum PositionEffectType { None, Offset, Shake }

        private void OnValidate()
        {
            if (!textMeshPro) textMeshPro = GetComponent<TextMeshProUGUI>();
            if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            if (!textMeshPro) textMeshPro = GetComponent<TextMeshProUGUI>();
            if (!rectTransform) rectTransform = GetComponent<RectTransform>();
            
            _originalScale = transform.localScale;
            _originalRotation = transform.localRotation.eulerAngles;
            _originalPosition = rectTransform.anchoredPosition3D;
            _originalColor = textMeshPro.color;
        }

        private void OnEnable()
        {
            SubscribeToMenuAnimator();
        }

        private void OnDisable()
        {
            UnsubscribeFromMenuAnimator();
            ResetToOriginalState();
        }

        private void SubscribeToMenuAnimator()
        {
            if (selectableAnimator == null) return;

            selectableAnimator.OnSelectEvent += Select;
            selectableAnimator.OnDeselectEvent += Deselect;
        }

        private void UnsubscribeFromMenuAnimator()
        {
            if (selectableAnimator == null) return;

            selectableAnimator.OnSelectEvent -= Select;
            selectableAnimator.OnDeselectEvent -= Deselect;
        }

        private void ResetToOriginalState()
        {
            if (positionEffectType == PositionEffectType.Offset) 
                rectTransform.anchoredPosition3D = _originalPosition;
            if (animateScale) 
                transform.localScale = _originalScale;
            if (animateRotation) 
                transform.localRotation = Quaternion.Euler(_originalRotation);
            if (animateColor)
            {
                var color = animateColor ? _originalColor : textMeshPro.color;
                textMeshPro.color = color;
            }
        }

        private void Select()
        {
            switch (positionEffectType)
            {
                case PositionEffectType.Offset:
                    PlayPositionAnimation(true);
                    break;
                case PositionEffectType.Shake:
                    PlayShakeAnimation();
                    break;
            }

            if (animateScale) PlayScaleAnimation(true);
            if (animateRotation) PlayRotateAnimation(true);
            if (animateColor) PlayColorAnimation(true);
        }

        private void Deselect()
        {
            switch (positionEffectType)
            {
                case PositionEffectType.Offset:
                    PlayPositionAnimation(false);
                    break;
                case PositionEffectType.Shake when shakeOnDeselect:
                    PlayShakeAnimation();
                    break;
            }

            if (animateScale) PlayScaleAnimation(false);
            if (animateRotation) PlayRotateAnimation(false);
            if (animateColor) PlayColorAnimation(false);
        }

        private void PlayPositionAnimation(bool selected)
        {
            if (!rectTransform) return;

            Vector3 endPosition = selected ? _originalPosition + positionOffset : _originalPosition;
            Tween.UIAnchoredPosition3D(rectTransform, endPosition, positionDuration, positionEase, useUnscaledTime: true);
        }

        private void PlayRotateAnimation(bool selected)
        {
            Vector3 endRotation = selected ? _originalRotation + rotationOffset : _originalRotation;
            Tween.LocalRotation(transform, endRotation, rotationDuration, rotationEase, useUnscaledTime: true);
        }

        private void PlayScaleAnimation(bool selected)
        {
            Vector3 endScale = selected ? _originalScale * scaleMultiplier : _originalScale;
            Tween.Scale(transform, endScale, scaleDuration, scaleEase, useUnscaledTime: true);
        }

        private void PlayShakeAnimation()
        {
            Tween.ShakeLocalPosition(transform, shakeStrength, shakeDuration, shakeFrequency,
                easeBetweenShakes: shakeEase, useUnscaledTime: true);
        }

        private void PlayColorAnimation(bool selected)
        {
            var endColor = selected ? selectedColor : _originalColor;
            Tween.Color(textMeshPro, endColor, colorDuration, colorEase, useUnscaledTime: true);
        }
        
    }
}