using DNExtensions.Utilities.AutoGet;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Systems.FirstPersonController
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FpcManager))]
    public class FPCCamera : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] [Range(0, 0.1f)] private float mouseLookSensitivity = 0.04f;
        [SerializeField] [Range(0, 300f)] private float gamepadLookSensitivity = 3f;
        [SerializeField] [Range(0, 0.1f)] private float lookSmoothing;
        [SerializeField] private Vector2 verticalAxisRange = new(-90, 90);
        [SerializeField] private bool invertHorizontal;
        [SerializeField] private bool invertVertical;
        
        [Header("FOV")]
        [SerializeField] private float baseFov = 60f;
        [SerializeField] private float runFovMultiplier = 1.3f;
        [SerializeField] private float fovChangeSmoothing = 5;
        
        [Header("References")]
        [SerializeField, AutoGetSelf] private FpcManager manager;
        [SerializeField, AutoGetChildren] private CinemachineCamera cinemachineCamera;
        [SerializeField] private Transform playerHead;

        private float _currentPanAngle;
        private float _currentTiltAngle;
        private float _targetPanAngle;
        private float _targetTiltAngle;
        private Vector2 _rotationVelocity;
        private Vector2 _lookInput;

        

        private void OnValidate()
        {
            if (cinemachineCamera) 
            {
                cinemachineCamera.Lens.FieldOfView = baseFov;
            }
        }

        private void Awake()
        {
            _currentPanAngle = transform.eulerAngles.y;
            _currentTiltAngle = playerHead.localEulerAngles.x;
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
            UpdateFov();
            UpdateHeadRotation();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void HandleLookInput()
        {
            if (!playerHead) return;
            
            float sensitivity = mouseLookSensitivity;
            if (manager.FpcInput.IsGamepad)
            {
                sensitivity = gamepadLookSensitivity * Time.deltaTime;
            }

            float horizontalInput = invertHorizontal ? -_lookInput.x : _lookInput.x;
            float verticalInput = invertVertical ? _lookInput.y : -_lookInput.y;
            
            _targetPanAngle += horizontalInput * sensitivity;
            _targetTiltAngle += verticalInput * sensitivity;
            _targetTiltAngle = Mathf.Clamp(_targetTiltAngle, verticalAxisRange.x, verticalAxisRange.y);

            if (lookSmoothing <= 0) 
            {
                _currentPanAngle = _targetPanAngle;
                _currentTiltAngle = _targetTiltAngle;
            }
        }

        private void UpdateHeadRotation()   
        {
            if (!playerHead) return;

            if (lookSmoothing > 0)
            {
                _currentPanAngle = Mathf.SmoothDampAngle(_currentPanAngle, _targetPanAngle, ref _rotationVelocity.x, lookSmoothing);
                _currentTiltAngle = Mathf.SmoothDamp(_currentTiltAngle, _targetTiltAngle, ref _rotationVelocity.y, lookSmoothing);
            }
            
            transform.rotation = Quaternion.Euler(0, _currentPanAngle, 0);
            playerHead.localRotation = Quaternion.Euler(_currentTiltAngle, 0, 0);
        }

        private void UpdateFov()
        {
            float targetFov = baseFov;
            if (manager.FpcMovement.IsRunning)
            {
                targetFov *= runFovMultiplier;
            }

            if (cinemachineCamera)
            {
                cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(
                    cinemachineCamera.Lens.FieldOfView, 
                    targetFov, 
                    Time.deltaTime * fovChangeSmoothing
                );
            }
        }
        
        public Vector3 GetMovementDirection()
        {
            Vector3 direction = Quaternion.Euler(0, _currentPanAngle, 0) * Vector3.forward;
            return direction.normalized;
        }

        public Vector3 GetAimDirection()
        {
            return Quaternion.Euler(_currentTiltAngle, _currentPanAngle, 0) * Vector3.forward;
        }

    }
}