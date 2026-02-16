using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DNExtensions.Systems.MenuSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class SelectableAnimator : MonoBehaviour
    {
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

        [Header("Alpha")] 
        [Tooltip("Set selectable transition to none for this to work")]
        [SerializeField] private bool animateAlpha;
        [SerializeField] private float selectedAlpha = 1f;
        [SerializeField] private float alphaDuration = 0.5f;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Space(10)] 
        [SerializeField, ReadOnly, AutoGetSelf] private Selectable selectable;
        [SerializeField, ReadOnly, AutoGetSelf] private RectTransform rectTransform;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private float _originalAlpha;
        private bool IsOffsetMode => positionEffectType == PositionEffectType.Offset;
        private bool IsShakeMode => positionEffectType == PositionEffectType.Shake;
        
        private enum PositionEffectType { None, Offset, Shake }

        private void Awake()
        {
            _originalScale = selectable.transform.localScale;
            _originalRotation = selectable.transform.localRotation.eulerAngles;
            _originalAlpha = selectable.targetGraphic.color.a;
            _originalPosition = rectTransform.anchoredPosition3D;

            selectable.AddEventListener(EventTriggerType.Select, OnSelect);
            selectable.AddEventListener(EventTriggerType.Deselect, OnDeselect);
        }

        private void OnDisable()
        {
            if (positionEffectType == PositionEffectType.Offset) rectTransform.anchoredPosition3D = _originalPosition;
            if (animateScale) selectable.transform.localScale = _originalScale;
            if (animateRotation) selectable.transform.localRotation = Quaternion.Euler(_originalRotation);
            if (animateAlpha && selectable.targetGraphic)
            {
                var color = selectable.targetGraphic.color;
                color.a = _originalAlpha;
                selectable.targetGraphic.color = color;
            }
        }
        

        private void OnSelect(BaseEventData eventData)
        {
            PlaySelectAnimations();
        }

        private void OnDeselect(BaseEventData eventData)
        {
            PlayDeselectAnimations();
        }
        
        
        public void PlaySelectAnimations()
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
            if (animateAlpha) PlayAlphaAnimation(true);
            
        }

        public void PlayDeselectAnimations()
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
            if (animateAlpha) PlayAlphaAnimation(false);
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

        private void PlayAlphaAnimation(bool selected)
        {
            if (!selectable.targetGraphic) return;
            var endAlpha = selected ? selectedAlpha : _originalAlpha;
            var curve = selected ? alphaCurve : AnimationCurve.Linear(0, 0, 1, 1);
            Tween.Alpha(selectable.targetGraphic, endAlpha, alphaDuration, curve, useUnscaledTime: true);
        }
    }
}