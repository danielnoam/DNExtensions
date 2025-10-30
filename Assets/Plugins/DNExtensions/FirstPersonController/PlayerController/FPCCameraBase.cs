using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(FPCManager))]
public abstract class FPCCameraBase : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] [Range(0, 0.1f)] private float lookSensitivity = 0.04f;
    [SerializeField] [Range(0, 0.1f)] private float lookSmoothing;
    [SerializeField] private Vector2 verticalAxisRange = new(-90, 90);
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical;
    
    [Header("FOV")]
    [SerializeField] protected float baseFov = 60f;
    [SerializeField] protected float runFovMultiplier = 1.3f;
    [SerializeField] protected float fovChangeSmoothing = 5;
    
    [Header("References")]
    [SerializeField] protected FPCManager manager;
    [SerializeField] protected Transform playerHead;

    private float _currentPanAngle;
    private float _currentTiltAngle;
    private float _targetPanAngle;
    private float _targetTiltAngle;
    private Vector2 _rotationVelocity;

    

    protected virtual void OnValidate()
    {
        if (!manager) manager = GetComponent<FPCManager>();
        UpdateFovInEditor();
    }

    protected virtual void Awake()
    {
        _currentPanAngle = transform.eulerAngles.y;
        _currentTiltAngle = playerHead.localEulerAngles.x;
        _targetPanAngle = _currentPanAngle;
        _targetTiltAngle = _currentTiltAngle;
    }

    protected virtual void OnEnable()
    {
        manager.FPCInput.OnLookAction += OnLook;
    }

    protected virtual void OnDisable()
    {
        manager.FPCInput.OnLookAction -= OnLook;
    }

    protected virtual void Update()
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

        SetFieldOfView(targetFov);
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
    
    protected abstract void UpdateFovInEditor();
    protected abstract void SetFieldOfView(float targetFov);
}