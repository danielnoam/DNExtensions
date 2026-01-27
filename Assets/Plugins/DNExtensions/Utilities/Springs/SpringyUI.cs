
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Utilities.Button;



namespace DNExtensions.Utilities.Springs
{

    [RequireComponent(typeof(RectTransform))]
    public class SpringyUI : MonoBehaviour
    {
        [Header("Animation Triggers")] 
        [SerializeField] private bool animateOnEnable = true;
        [SerializeField] private bool animateOnce;
        [SerializeField] private bool resetOnDisable = true;

        [Header("Position")] 
        [SerializeField] private bool position;
        [SerializeField, ShowIf("position")] private Vector3 positionOffset = new Vector3(0f, 500f, 0f);
        [SerializeField, ShowIf("position")] private Vector3Spring positionSpring = new Vector3Spring();

        [Header("Scale")] 
        [SerializeField] private bool scale;
        [SerializeField, ShowIf("scale")] private Vector3 scaleOffset = new Vector3(0.7f, 1.2f, 1f);
        [SerializeField, ShowIf("scale")] private Vector3Spring scaleSpring = new Vector3Spring();

        [Header("Rotation")] 
        [SerializeField] private bool rotation;
        [SerializeField, ShowIf("rotation")] private Vector3 rotationOffset = new Vector3(0f, 0f, 45f);
        [SerializeField, ShowIf("rotation")] private QuaternionSpring rotationSpring = new QuaternionSpring();

        [Header("Color (Requires Graphic)")] [SerializeField]
        private bool color;
        [SerializeField, ShowIf("color")] private Color colorOffset = Color.clear;
        [SerializeField, ShowIf("color")] private Spring colorAlphaSpring = new Spring();

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
                    colorAlphaSpring.target = _baseColor.a;
                }
            }

            _isInitialized = true;
        }

        private void OnEnable()
        {
            Initialize();

            if (animateOnEnable && (!animateOnce || !_hasAnimated))
            {
                AnimateFromOffset();
                _hasAnimated = true;
            }
        }

        private void OnDisable()
        {
            if (!_isInitialized) return;

            if (resetOnDisable)
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
                    colorAlphaSpring.Reset(_baseColor.a);
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
                colorAlphaSpring.Update(Time.deltaTime);
                Color newColor = _baseColor;
                newColor.a = colorAlphaSpring.Value;
                _targetGraphic.color = newColor;
            }
        }

        public void ToggleSpringsLock(bool resetVelocity)
        {
            if (position)
            {
                if (positionSpring.IsLocked)
                    positionSpring.Unlock();
                else
                    positionSpring.Lock(resetVelocity);
            }

            if (scale)
            {
                if (scaleSpring.IsLocked)
                    scaleSpring.Unlock();
                else
                    scaleSpring.Lock(resetVelocity);
            }

            if (rotation)
            {
                if (rotationSpring.IsLocked)
                    rotationSpring.Unlock();
                else
                    rotationSpring.Lock(resetVelocity);
            }

            if (color)
            {
                if (colorAlphaSpring.IsLocked)
                    colorAlphaSpring.Unlock();
                else
                    colorAlphaSpring.Lock(resetVelocity);
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
                scaleSpring.SetValue(scaleOffset);
            }

            if (rotation)
            {
                rotationSpring.Target = _baseRotation;
                rotationSpring.SetValue(Quaternion.Euler(rotationOffset) * _baseRotation);
            }

            if (color && _targetGraphic)
            {
                colorAlphaSpring.target = _baseColor.a;
                colorAlphaSpring.SetValue(colorOffset.a);

                Color startColor = _baseColor;
                startColor.a = colorOffset.a;
                _targetGraphic.color = startColor;
            }
        }

        [Button]
        public void AnimateToOffset()
        {
            if (position)
            {
                positionSpring.target = _baseAnchoredPosition + positionOffset;
            }

            if (scale)
            {
                scaleSpring.target = scaleOffset;
            }

            if (rotation)
            {
                rotationSpring.Target = Quaternion.Euler(rotationOffset) * _baseRotation;
            }

            if (color && _targetGraphic)
            {
                colorAlphaSpring.target = colorOffset.a;
            }
        }

        public void ResetHasAnimated()
        {
            _hasAnimated = false;
        }
    }
}