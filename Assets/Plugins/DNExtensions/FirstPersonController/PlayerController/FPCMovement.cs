
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DNExtensions;
using DNExtensions.ControllerRumbleSystem;


[DisallowMultipleComponent]
[RequireComponent(typeof(FPCManager))]
public class FPCMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 8f;
    [SerializeField] private bool canRun = true;
    [ShowIf("canRun")][SerializeField] private float runSpeed = 12f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private ControllerRumbleEffectSettings landingRumbleSettings = new ControllerRumbleEffectSettings(0.5f, 0.7f, 0.2f);
    
    [Header("Jump")]
    [SerializeField] private float jumpForce = 1.5f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    
    
    [Header("References")] 
    [SerializeField] private FPCManager manager;


    
    
    private Vector3 _velocity;
    private Vector3 _dashDirection;
    private Vector2 _moveInput;
    private float _dashTimeRemaining;
    private float _dashCooldownRemaining;
    private float _jumpBufferCounter;
    private float _coyoteTimeCounter;
    private bool _runInput;
    private bool _wasGrounded;
    

    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsFalling { get; private set; }


    private void OnValidate()
    {
        if (!manager) manager = GetComponent<FPCManager>();
    }

    private void OnEnable()
    {
        manager.FPCInput.OnMoveAction += GetMovementInput;
        manager.FPCInput.OnRunAction += GetRunningInput;
        manager.FPCInput.OnJumpAction += GetJumpInput;
    }

    private void OnDisable()
    {
        manager.FPCInput.OnMoveAction -= GetMovementInput;
        manager.FPCInput.OnRunAction -= GetRunningInput;
        manager.FPCInput.OnJumpAction -= GetJumpInput;
    }
    
    private void Update()
    {
        HandleMovement();
        HandleJump();
        CheckGrounded();
    }
    


    private void GetMovementInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }
        
    private void GetRunningInput(InputAction.CallbackContext context)
    {
        _runInput = context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed;
    }
        
    private void GetJumpInput(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            _jumpBufferCounter = jumpBufferTime;
        }
    }
    

    private void HandleMovement()
    {
        Vector3 cameraForward = manager.FPCCamera.GetMovementDirection();
        Vector3 cameraRight = Quaternion.Euler(0, 90, 0) * cameraForward;
        Vector3 moveDir = (cameraForward * _moveInput.y + cameraRight * _moveInput.x).normalized;

        IsRunning = _runInput && canRun;
        float targetMoveSpeed = IsRunning ? runSpeed : walkSpeed;
        if (manager.FPCInteraction.HeldObject)
        {
            targetMoveSpeed /= manager.FPCInteraction.HeldObject.ObjectWeight;
        }
        manager.CharacterController.Move(moveDir * (targetMoveSpeed * Time.deltaTime));
    }
    
    private void HandleJump()
    {
        
        if (_jumpBufferCounter > 0f)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
        
        if (_jumpBufferCounter > 0f && (_coyoteTimeCounter > 0f || IsGrounded))
        {
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;  
        }
        
        _velocity.y += gravity * Time.deltaTime;
        manager.CharacterController.Move(_velocity * Time.deltaTime);
        IsJumping = _velocity.y > 0;
    }

    private void CheckGrounded()
    {
        _wasGrounded  = IsGrounded; 
        
        IsGrounded = manager.CharacterController.isGrounded;
        IsFalling = _velocity.y < 0;
        
        if (IsGrounded && !_wasGrounded)
        {
            manager.ControllerRumbleSource?.Rumble(landingRumbleSettings);
        }
        
        if (IsGrounded)
        {
            if (_velocity.y < 0)
            {
                _velocity.y = -2f;
            }
            _coyoteTimeCounter = coyoteTime; 
        }
        else if (_wasGrounded)
        {
            _coyoteTimeCounter = coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }

    }





}