
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DNExtensions;

[SelectionBase]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(FPCPlayerCamera))]
[RequireComponent(typeof(FPCPlayerInput))]
[RequireComponent(typeof(FPCPlayerInteraction))]
[RequireComponent(typeof(FPCPlayerRigidBodyPush))]
[DisallowMultipleComponent]

public class FpcPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private bool canRun = true;
    [ShowIf("canRun")][SerializeField] private float runSpeed = 7f;
    [SerializeField] private float gravity = -15f;
    
    [Header("Jump")]
    [SerializeField] private float jumpForce = 1.5f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("References")] 
    [SerializeField] private CharacterController controller;
    [SerializeField] private FPCPlayerCamera fpcPlayerCamera;
    [SerializeField] private FPCPlayerInteraction fpcPlayerInteraction;
    [SerializeField] private FPCPlayerInput fpcPlayerInput;
    [SerializeField] private AudioSource audioSource;


    
    
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
        if (!controller) controller = GetComponent<CharacterController>();
        if (!fpcPlayerCamera) fpcPlayerCamera = GetComponent<FPCPlayerCamera>();
        if (!fpcPlayerInput) fpcPlayerInput = GetComponent<FPCPlayerInput>();
        if (!fpcPlayerInteraction) fpcPlayerInteraction = GetComponent<FPCPlayerInteraction>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        fpcPlayerInput.OnMoveAction += GetMovementInput;
        fpcPlayerInput.OnRunAction += GetRunningInput;
        fpcPlayerInput.OnJumpAction += GetJumpInput;
    }

    private void OnDisable()
    {
        fpcPlayerInput.OnMoveAction -= GetMovementInput;
        fpcPlayerInput.OnRunAction -= GetRunningInput;
        fpcPlayerInput.OnJumpAction -= GetJumpInput;
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
        Vector3 cameraForward = fpcPlayerCamera.GetMovementDirection();
        Vector3 cameraRight = Quaternion.Euler(0, 90, 0) * cameraForward;
        Vector3 moveDir = (cameraForward * _moveInput.y + cameraRight * _moveInput.x).normalized;

        IsRunning = _runInput && canRun;
        float targetMoveSpeed = IsRunning ? runSpeed : walkSpeed;
        if (fpcPlayerInteraction.HeldObject)
        {
            targetMoveSpeed /= fpcPlayerInteraction.HeldObject.ObjectWeight;
        }
        controller.Move(moveDir * (targetMoveSpeed * Time.deltaTime));
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
        controller.Move(_velocity * Time.deltaTime);
        IsJumping = _velocity.y > 0;
    }

    private void CheckGrounded()
    {
        _wasGrounded  = IsGrounded;
        IsGrounded = controller.isGrounded;
        IsFalling = _velocity.y < 0;
        
        if (IsGrounded)
        {
            if (_velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }
        else if (IsGrounded || _wasGrounded)
        {
            _coyoteTimeCounter = coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }

    }





}