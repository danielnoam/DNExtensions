
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(FPCManager))]
public class FPCCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useNormalCamera;
    [SerializeField] [Range(0,0.1f)] private float lookSensitivity = 0.04f;
    [SerializeField] [Range(0,0.1f)] private float lookSmoothing;
    [SerializeField] private Vector2 verticalAxisRange = new (-90, 90);
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical;
    
    [Header("FOV")]
    [SerializeField] private float baseFov = 60f;
    [SerializeField] private float runFovMultiplier = 1.3f;
    [SerializeField] private float fovChangeSmoothing = 5;
    
    [Header("References")]
    [SerializeField] private FPCManager manager;
    [SerializeField] private Transform playerHead;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Camera normalCamera;
    

    private float _currentPanAngle;
    private float _currentTiltAngle;
    private float _targetPanAngle;
    private float _targetTiltAngle;
    private Vector2 _rotationVelocity;
    
    
    private void OnValidate()
    {
        if (!manager) manager = GetComponent<FPCManager>();

        if (useNormalCamera)
        {
            if (normalCamera) normalCamera.fieldOfView = baseFov;
        }
        else
        {
            if (cinemachineCamera) cinemachineCamera.Lens.FieldOfView = baseFov;
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
        manager.FPCInput.OnLookAction += OnLook;
    }
    
    private void OnDisable()
    {
        manager.FPCInput.OnLookAction -= OnLook;
    }
    
    private void Update()
    {
        UpdateFov();
        UpdateHeadRotation();
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        if (!playerHead) return;
        
        Vector2 lookDelta = context.ReadValue<Vector2>();


        float horizontalInput = invertHorizontal ? -lookDelta.x : lookDelta.x;
        float verticalInput = invertVertical ? lookDelta.y : -lookDelta.y;
        
        _targetPanAngle += horizontalInput * lookSensitivity;
        _targetTiltAngle += verticalInput * lookSensitivity;
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
        if (manager.FPCMovement.IsRunning)
        {
            targetFov *= runFovMultiplier;
        }
        
        if (useNormalCamera)
        {
            if (normalCamera) normalCamera.fieldOfView = Mathf.Lerp(normalCamera.fieldOfView, targetFov, Time.deltaTime * fovChangeSmoothing);
        }
        else
        {
            if (cinemachineCamera) cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFov, Time.deltaTime * fovChangeSmoothing);
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