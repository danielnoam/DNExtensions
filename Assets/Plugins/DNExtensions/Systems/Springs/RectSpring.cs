using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Utilities.Button;

namespace DNExtensions.Systems.Springs
{
    [AddComponentMenu("DNExtensions/Springs/Rect Spring")]
    [RequireComponent(typeof(RectTransform))]
    public class RectSpring : MonoBehaviour
    {
        private enum OnEnableBehavior
        {
            Nothing,
            StartAtOffset,
            AnimateFromOffset,
            AnimateToOffset,
        }

        [Header("Animation Triggers")]
        [SerializeField] private OnEnableBehavior onEnableBehavior = OnEnableBehavior.AnimateFromOffset;
        [Tooltip("If true, the animation will only play the first time the object is enabled.")]
        [SerializeField] private bool animateOnce;
        [Tooltip("If true, the UI will snap back to its original state when disabled.")]
        [SerializeField] private bool resetStateOnDisable = true;

        [Header("Position")]
        [SerializeField] private bool position;
        [SerializeField, ShowIf("position")] private Vector3 positionOffset = new Vector3(0f, 500f, 0f);
        [SerializeField, ShowIf("position")] private Vector3Spring positionSpring = new Vector3Spring();

        [Header("Scale")]
        [SerializeField] private bool scale;
        [SerializeField, ShowIf("scale")] private Vector3 scaleOffset = new Vector3(0.2f, 0.2f, 0.2f);
        [SerializeField, ShowIf("scale")] private Vector3Spring scaleSpring = new Vector3Spring();

        [Header("Rotation")]
        [SerializeField] private bool rotation;
        [SerializeField, ShowIf("rotation")] private Vector3 rotationOffset = new Vector3(0f, 0f, 45f);
        [SerializeField, ShowIf("rotation")] private QuaternionSpring rotationSpring = new QuaternionSpring();

        [Header("Color (Requires Graphic)")]
        [SerializeField] private bool color;
        [SerializeField, ShowIf("color")] private Color colorOffset = new Color(0, 0, 0, -1f);
        [SerializeField, ShowIf("color")] private ColorSpring colorSpring = new ColorSpring();

        private Graphic _targetGraphic;
        private RectTransform _rectTransform;
        private Vector3 _baseAnchoredPosition;
        private Vector3 _baseScale;
        private Quaternion _baseRotation;
        private Color _baseColor;
        private bool _isInitialized;
        private bool _hasAnimated;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _rectTransform = GetComponent<RectTransform>();
            _baseAnchoredPosition = _rectTransform.anchoredPosition3D;
            _baseScale = _rectTransform.localScale;
            _baseRotation = _rectTransform.localRotation;

            if (position) positionSpring.target = _baseAnchoredPosition;
            if (scale) scaleSpring.target = _baseScale;
            if (rotation) rotationSpring.Target = _baseRotation;

            if (color)
            {
                _targetGraphic = GetComponent<Graphic>();
                if (_targetGraphic)
                {
                    _baseColor = _targetGraphic.color;
                    colorSpring.target = _baseColor;
                }
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            Initialize();
            if (animateOnce && _hasAnimated) return;

            switch (onEnableBehavior)
            {
                case OnEnableBehavior.Nothing: break;
                case OnEnableBehavior.StartAtOffset:
                    SnapToOffset();
                    _hasAnimated = true;
                    break;
                case OnEnableBehavior.AnimateFromOffset:
                    AnimateFromOffset();
                    _hasAnimated = true;
                    break;
                case OnEnableBehavior.AnimateToOffset:
                    AnimateToOffset();
                    _hasAnimated = true;
                    break;
            }
        }

        private void OnDisable()
        {
            if (!_isInitialized) return;

            if (resetStateOnDisable)
            {
                if (position)
                {
                    positionSpring.Reset(_baseAnchoredPosition);
                    _rectTransform.anchoredPosition3D = _baseAnchoredPosition;
                }
                if (scale)
                {
                    scaleSpring.Reset(_baseScale);
                    _rectTransform.localScale = _baseScale;
                }
                if (rotation)
                {
                    rotationSpring.Reset(_baseRotation);
                    _rectTransform.localRotation = _baseRotation;
                }
                if (color && _targetGraphic)
                {
                    colorSpring.Reset(_baseColor);
                    _targetGraphic.color = _baseColor;
                }
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (position)
            {
                positionSpring.Update(Time.deltaTime);
                _rectTransform.anchoredPosition3D = positionSpring.Value;
            }
            if (scale)
            {
                scaleSpring.Update(Time.deltaTime);
                _rectTransform.localScale = scaleSpring.Value;
            }
            if (rotation)
            {
                rotationSpring.Update(Time.deltaTime);
                _rectTransform.localRotation = rotationSpring.Value;
            }
            if (color && _targetGraphic)
            {
                colorSpring.Update(Time.deltaTime);
                _targetGraphic.color = colorSpring.Value;
            }
        }

        [Button]
        public void SnapToOffset()
        {
            if (position)
            {
                Vector3 targetPos = _baseAnchoredPosition + positionOffset;
                positionSpring.Reset(targetPos);
                _rectTransform.anchoredPosition3D = targetPos;
            }
            if (scale)
            {
                Vector3 targetScale = _baseScale + scaleOffset;
                scaleSpring.Reset(targetScale);
                _rectTransform.localScale = targetScale;
            }
            if (rotation)
            {
                Quaternion targetRot = Quaternion.Euler(rotationOffset) * _baseRotation;
                rotationSpring.Reset(targetRot);
                _rectTransform.localRotation = targetRot;
            }
            if (color && _targetGraphic)
            {
                Color targetColor = _baseColor + colorOffset;
                colorSpring.Reset(targetColor);
                _targetGraphic.color = targetColor;
            }
        }

        [Button]
        public void AnimateFromOffset()
        {
            if (position)
            {
                positionSpring.target = _baseAnchoredPosition;
                positionSpring.SetValue(_baseAnchoredPosition + positionOffset);
            }
            if (scale)
            {
                scaleSpring.target = _baseScale;
                scaleSpring.SetValue(_baseScale + scaleOffset);
            }
            if (rotation)
            {
                rotationSpring.Target = _baseRotation;
                rotationSpring.SetValue(Quaternion.Euler(rotationOffset) * _baseRotation);
            }
            if (color && _targetGraphic)
            {
                colorSpring.target = _baseColor;
                colorSpring.SetValue(_baseColor + colorOffset);
                _targetGraphic.color = _baseColor + colorOffset;
            }
        }

        [Button]
        public void AnimateToOffset()
        {
            if (position) positionSpring.target = _baseAnchoredPosition + positionOffset;
            if (scale) scaleSpring.target = _baseScale + scaleOffset;
            if (rotation) rotationSpring.Target = Quaternion.Euler(rotationOffset) * _baseRotation;
            if (color && _targetGraphic) colorSpring.target = _baseColor + colorOffset;
        }

        public void ResetHasAnimated() => _hasAnimated = false;
    }
}