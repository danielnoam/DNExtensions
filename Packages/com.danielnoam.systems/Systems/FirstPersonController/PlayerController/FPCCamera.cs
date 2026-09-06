using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Handles first-person camera rotation, head positioning, and look input for the player controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FpcManager))]
    [AddComponentMenu("")]
    public class FPCCamera : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField, Range(1f, 10f)] private float mouseLookSensitivity = 5f;
        [SerializeField, Range(1f, 10f)] private float gamepadLookSensitivity = 5f;
        [SerializeField, Range(0, 0.1f)] private float lookSmoothing;
        [SerializeField] private Vector2 verticalAxisRange = new(-90, 90);
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;
        
        [Header("Crouch")]
        [Tooltip("How far the head lowers when crouching, measured from its standing height")]
        [SerializeField] private float crouchHeadDrop = 0.4f;
        [SerializeField] private float crouchHeadTransitionSpeed = 10f;
        
        [Header("Lean")]
        [SerializeField] private bool allowLean = true;
        [SerializeField, ShowIf(nameof(allowLean))] private float leanAngle = 15f;
        [SerializeField, ShowIf(nameof(allowLean))] private float leanDistance = 0.4f;
        [SerializeField, ShowIf(nameof(allowLean))] private float leanSpeed = 8f;
        [Tooltip("Radius of the check that keeps the head from leaning through walls, 0 to lean freely")]
        [SerializeField, ShowIf(nameof(allowLean))] private float leanCheckRadius = 0.2f;
        [SerializeField, ShowIf(nameof(allowLean))] private LayerMask leanCollisionLayers = 0;

        [Header("References")]
        [SerializeField] private Transform head;
        [SerializeField, AutoGetChildren] private CinemachineCamera cam;
        [SerializeField, AutoGetSelf, HideInInspector] private FpcManager manager;

        private float _standingHeadHeight;
        private float _currentPanAngle;
        private float _currentTiltAngle;
        private float _targetPanAngle;
        private float _targetTiltAngle;
        private Vector2 _rotationVelocity;
        private Vector2 _lookInput;
        private float _currentLean;

        private const float MouseSensitivityMultiplier = 0.05f;
        private const float GamepadSensitivityMultiplier = 100f;
        
        public CinemachineCamera Cam => cam;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            _standingHeadHeight = head.localPosition.y;
            _currentPanAngle = transform.eulerAngles.y;
            _currentTiltAngle = head.localEulerAngles.x;
            _targetPanAngle = _currentPanAngle;
            _targetTiltAngle = _currentTiltAngle;
        }

        private void OnEnable()
        {
            manager.FpcInput.OnLookAction += OnLook;
        }

        private void OnDisable()
        {
            manager.FpcInput.OnLookAction -= OnLook;
        }

        private void Update()
        {
            HandleLookInput();
            UpdateLean();
            UpdateHeadRotation();
            UpdateHeadPosition();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void HandleLookInput()
        {
            if (!head) return;

            float horizontalInput = invertHorizontal ? -_lookInput.x : _lookInput.x;
            float verticalInput = invertVertical ? _lookInput.y : -_lookInput.y;

            if (manager.FpcInput.IsGamepad)
            {
                float sensitivity = gamepadLookSensitivity * GamepadSensitivityMultiplier;
                _targetPanAngle += horizontalInput * sensitivity * Time.deltaTime;
                _targetTiltAngle += verticalInput * sensitivity * Time.deltaTime;
            }
            else
            {
                float sensitivity = mouseLookSensitivity * MouseSensitivityMultiplier;
                _targetPanAngle += horizontalInput * sensitivity;
                _targetTiltAngle += verticalInput * sensitivity;
            }

            _targetTiltAngle = Mathf.Clamp(_targetTiltAngle, verticalAxisRange.x, verticalAxisRange.y);

            if (lookSmoothing <= 0)
            {
                _currentPanAngle = _targetPanAngle;
                _currentTiltAngle = _targetTiltAngle;
            }
        }
        
        private void UpdateHeadPosition()
        {
            float targetY = manager.FpcLocomotion.IsCrouching ? _standingHeadHeight - crouchHeadDrop : _standingHeadHeight;
            Vector3 pos = head.localPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * crouchHeadTransitionSpeed);
            pos.x = _currentLean * leanDistance;
            head.localPosition = pos;
        }

        private void UpdateLean()
        {
            if (!head) return;

            float targetLean = allowLean ? Mathf.Clamp(manager.FpcInput.LeanInput, -1f, 1f) : 0f;
            _currentLean = Mathf.Lerp(_currentLean, GetUnobstructedLean(targetLean), Time.deltaTime * leanSpeed);
        }

        /// <summary>
        /// Shortens the lean so the head stops at a wall instead of passing through it.
        /// </summary>
        private float GetUnobstructedLean(float targetLean)
        {
            if (leanCheckRadius <= 0f || Mathf.Approximately(targetLean, 0f)) return targetLean;

            Vector3 headLocalPosition = head.localPosition;
            Vector3 origin = transform.TransformPoint(new Vector3(0f, headLocalPosition.y, headLocalPosition.z));
            Vector3 direction = transform.right * Mathf.Sign(targetLean);
            float wantedDistance = Mathf.Abs(targetLean) * leanDistance;

            if (!Physics.SphereCast(origin, leanCheckRadius, direction, out RaycastHit hit, wantedDistance, leanCollisionLayers))
            {
                return targetLean;
            }

            return Mathf.Sign(targetLean) * Mathf.Min(Mathf.Abs(targetLean), hit.distance / leanDistance);
        }

        private void UpdateHeadRotation()
        {
            if (!head) return;

            if (lookSmoothing > 0)
            {
                _currentPanAngle = Mathf.SmoothDampAngle(_currentPanAngle, _targetPanAngle, ref _rotationVelocity.x, lookSmoothing);
                _currentTiltAngle = Mathf.SmoothDamp(_currentTiltAngle, _targetTiltAngle, ref _rotationVelocity.y, lookSmoothing);
            }

            transform.rotation = Quaternion.Euler(0, _currentPanAngle, 0);
            head.localRotation = Quaternion.Euler(_currentTiltAngle, 0, -_currentLean * leanAngle);
        }

        /// <summary>
        /// Gets the horizontal movement direction based on the current camera pan angle.
        /// </summary>
        public Vector3 GetMovementDirection()
        {
            return (Quaternion.Euler(0, _currentPanAngle, 0) * Vector3.forward).normalized;
        }

        /// <summary>
        /// Gets the aim direction based on both pan and tilt angles.
        /// </summary>
        public Vector3 GetAimDirection()
        {
            return Quaternion.Euler(_currentTiltAngle, _currentPanAngle, 0) * Vector3.forward;
        }
    }
}