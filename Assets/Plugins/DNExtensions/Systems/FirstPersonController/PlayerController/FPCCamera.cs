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
        [SerializeField] private float crouchHeadHeight = 0.2f;
        [SerializeField] private float crouchHeadTransitionSpeed = 10f;
        
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

        private const float MouseSensitivityMultiplier = 0.05f;
        private const float GamepadSensitivityMultiplier = 100f;
        
        public CinemachineCamera Cam => cam;

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
            UpdateHeadRotation();
            UpdateHeadHeight();
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
        
        private void UpdateHeadHeight()
        {
            float targetY = manager.FpcLocomotion.IsCrouching ? crouchHeadHeight : _standingHeadHeight;
            Vector3 pos = head.localPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * crouchHeadTransitionSpeed);
            head.localPosition = pos;
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
            head.localRotation = Quaternion.Euler(_currentTiltAngle, 0, 0);
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