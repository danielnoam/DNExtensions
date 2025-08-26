
using System.Linq;
using DNExtensions;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace  DNExtensions.MenuSystem
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(EventTrigger))]

    public class MenuSelectableAnimator : MonoBehaviour
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
        [SerializeField, ReadOnly] private Selectable selectable;
        [SerializeField, ReadOnly] private EventTrigger eventTrigger;
        [SerializeField, ReadOnly] private RectTransform rectTransform;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Vector3 _originalRotation;
        private float _originalAlpha;
        private bool _originalInteractionState;
        private bool IsOffsetMode => positionEffectType == PositionEffectType.Offset;
        private bool IsShakeMode => positionEffectType == PositionEffectType.Shake;

        private enum PositionEffectType
        {
            None,
            Offset,
            Shake
        }


        private void OnValidate()
        {
            if (!eventTrigger) eventTrigger = GetComponent<EventTrigger>();
            if (!selectable) selectable = GetComponent<Selectable>();
            if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        }

        private void Awake()
        {

            _originalInteractionState = selectable.interactable;
            _originalScale = selectable.transform.localScale;
            _originalRotation = selectable.transform.localRotation.eulerAngles;
            _originalAlpha = selectable.image.color.a;
            _originalPosition = rectTransform.anchoredPosition3D;

            AddEventTriggerEntry(EventTriggerType.Select, OnSelect);
            AddEventTriggerEntry(EventTriggerType.Deselect, OnDeselect);
        }

        private void OnDisable()
        {
            if (positionEffectType == PositionEffectType.Offset) selectable.transform.position = _originalPosition;
            if (animateScale) selectable.transform.localScale = _originalScale;
            if (animateRotation) selectable.transform.localRotation = Quaternion.Euler(_originalRotation);
            if (animateAlpha)
            {
                var color = selectable.image.color;
                color.a = _originalAlpha;
                selectable.image.color = color;
            }
        }

        private void AddEventTriggerEntry(EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            if (!eventTrigger) return;


            var existingEntry = eventTrigger.triggers.FirstOrDefault(entry => entry.eventID == type);

            if (existingEntry != null)
            {
                existingEntry.callback.AddListener(callback);
            }
            else
            {
                var newEntry = new EventTrigger.Entry
                {
                    eventID = type,
                    callback = new EventTrigger.TriggerEvent()
                };
                newEntry.callback.AddListener(callback);
                eventTrigger.triggers.Add(newEntry);
            }
        }


        private void OnSelect(BaseEventData eventData)
        {
            if (!eventData.selectedObject.activeSelf || !selectable.interactable) return;

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

        private void OnDeselect(BaseEventData eventData)
        {
            if (!eventData.selectedObject.activeSelf || !selectable.interactable) return;

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
            var endAlpha = selected ? selectedAlpha : _originalAlpha;
            var curve = selected ? alphaCurve : AnimationCurve.Linear(0, 0, 1, 1);
            Tween.Alpha(selectable.image, endAlpha, alphaDuration, curve, useUnscaledTime: true);

        }


    }
}