using UnityEngine;
using DNExtensions.Utilities.Button;
using DNExtensions.Utilities.CustomFields;

namespace DNExtensions.Utilities.Springs
{
    public enum SpaceMode
    {
        Local,
        World
    }

    [AddComponentMenu("DNExtensions/Transform Spring")]
    public class TransformSpring : MonoBehaviour
    {
        [Header("Position")]
        [SerializeField] private bool usePosition;
        [SerializeField, ShowIf("usePosition")] private SpaceMode positionSpace = SpaceMode.Local;
        [SerializeField, ShowIf("usePosition")] private PositionField positionTarget;
        [SerializeField, ShowIf("usePosition")] private Vector3 positionOffset;
        [SerializeField, ShowIf("usePosition")] private Vector3Spring positionSpring = new Vector3Spring();

        [Header("Rotation")]
        [SerializeField] private bool useRotation;
        [SerializeField, ShowIf("useRotation")] private SpaceMode rotationSpace = SpaceMode.Local;
        [SerializeField, ShowIf("useRotation")] private Vector3 rotationOffset;
        [SerializeField, ShowIf("useRotation")] private QuaternionSpring rotationSpring = new QuaternionSpring();

        [Header("Scale")]
        [SerializeField] private bool useScale;
        [SerializeField, ShowIf("useScale")] private Vector3 scaleOffset = Vector3.one;
        [SerializeField, ShowIf("useScale")] private Vector3Spring scaleSpring = new Vector3Spring();

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _baseScale;
        private bool _isInitialized;

        public Vector3Spring PositionSpring => positionSpring;
        public QuaternionSpring RotationSpring => rotationSpring;
        public Vector3Spring ScaleSpring => scaleSpring;
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            CaptureBase();

            if (usePosition)
                positionSpring.Reset(_basePosition);

            if (useRotation)
                rotationSpring.Reset(_baseRotation);

            if (useScale)
                scaleSpring.Reset(_baseScale);

            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (usePosition)
            {
                SyncPositionTarget();
                positionSpring.Update(Time.deltaTime);
                ApplyPosition(positionSpring.Value);
            }

            if (useRotation)
            {
                rotationSpring.Update(Time.deltaTime);
                ApplyRotation(rotationSpring.Value);
            }

            if (useScale)
            {
                scaleSpring.Update(Time.deltaTime);
                transform.localScale = scaleSpring.Value;
            }
        }

        /// <summary>
        /// Set spring targets to current PositionField/base values. Springs animate smoothly toward them.
        /// </summary>
        [Button]
        public void SetTarget()
        {
            Initialize();

            if (usePosition)
                positionSpring.target = GetPositionTarget();

            if (useRotation)
                rotationSpring.Target = _baseRotation;

            if (useScale)
                scaleSpring.target = _baseScale;
        }

        /// <summary>
        /// Set spring targets explicitly.
        /// </summary>
        public void SetTarget(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
        {
            Initialize();

            if (usePosition && position.HasValue)
                positionSpring.target = position.Value;

            if (useRotation && rotation.HasValue)
                rotationSpring.Target = rotation.Value;

            if (useScale && scale.HasValue)
                scaleSpring.target = scale.Value;
        }

        /// <summary>
        /// Snap to offset, then spring back to base/target.
        /// </summary>
        [Button]
        public void AnimateFromOffset()
        {
            Initialize();

            if (usePosition)
            {
                Vector3 target = GetPositionTarget();
                positionSpring.target = target;
                positionSpring.SetValue(target + positionOffset);
            }

            if (useRotation)
            {
                rotationSpring.Target = _baseRotation;
                rotationSpring.SetValue(Quaternion.Euler(rotationOffset) * _baseRotation);
            }

            if (useScale)
            {
                scaleSpring.target = _baseScale;
                scaleSpring.SetValue(scaleOffset);
            }
        }

        /// <summary>
        /// Animate from current values toward offset.
        /// </summary>
        [Button]
        public void AnimateToOffset()
        {
            Initialize();

            if (usePosition)
                positionSpring.target = GetPositionTarget() + positionOffset;

            if (useRotation)
                rotationSpring.Target = Quaternion.Euler(rotationOffset) * _baseRotation;

            if (useScale)
                scaleSpring.target = scaleOffset;
        }

        [Button]
        public void LockAll()
        {
            if (usePosition) positionSpring.Lock();
            if (useRotation) rotationSpring.Lock();
            if (useScale) scaleSpring.Lock();
        }

        [Button]
        public void UnlockAll()
        {
            if (usePosition) positionSpring.Unlock();
            if (useRotation) rotationSpring.Unlock();
            if (useScale) scaleSpring.Unlock();
        }

        [Button]
        public void ResetAll()
        {
            Initialize();

            if (usePosition)
            {
                positionSpring.Reset(GetPositionTarget());
                ApplyPosition(positionSpring.Value);
            }

            if (useRotation)
            {
                rotationSpring.Reset(_baseRotation);
                ApplyRotation(_baseRotation);
            }

            if (useScale)
            {
                scaleSpring.Reset(_baseScale);
                transform.localScale = _baseScale;
            }
        }

        /// <summary>
        /// Re-capture the current transform as the new base state.
        /// </summary>
        public void RecaptureBase()
        {
            CaptureBase();
        }

        private void CaptureBase()
        {
            _basePosition = positionSpace == SpaceMode.Local ? transform.localPosition : transform.position;
            _baseRotation = rotationSpace == SpaceMode.Local ? transform.localRotation : transform.rotation;
            _baseScale = transform.localScale;
        }

        private Vector3 GetPositionTarget()
        {
            var positionField = positionTarget;
            
            if (positionField != null && positionField.Transform) return positionField.Position;

            return _basePosition;
        }

        private void SyncPositionTarget()
        {
            if (positionTarget != null && positionTarget.Transform) positionSpring.target = positionTarget.Position;
        }

        private void ApplyPosition(Vector3 value)
        {
            if (positionSpace == SpaceMode.Local)
                transform.localPosition = value;
            else
                transform.position = value;
        }

        private void ApplyRotation(Quaternion value)
        {
            if (rotationSpace == SpaceMode.Local)
                transform.localRotation = value;
            else
                transform.rotation = value;
        }
    }
}