
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;

namespace DNExtensions.Systems.FirstPersonController
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FpcManager))]
    [AddComponentMenu("")]
    public class FPCMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 8f;
        [SerializeField] private bool canRun = true;
        [SerializeField, ShowIf("canRun")] private float runSpeed = 12f;
        [SerializeField] private float gravity = -15f;
        
        [Header("Jump")]
        [SerializeField] private float jumpForce = 1.5f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField, AutoGetSelf, HideInInspector] private FpcManager manager;

        
        private Vector3 _velocity;
        private Vector3 _dashDirection;
        private float _dashTimeRemaining;
        private float _dashCooldownRemaining;
        private float _jumpBufferCounter;
        private float _coyoteTimeCounter;
        private bool _wasGrounded;
        

        public bool IsGrounded { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsFalling { get; private set; }
        
        public bool IsRunning => manager.FpcInput.RunInput && canRun;
        public Vector3 Velocity => _velocity;
        
        public event Action<float> OnLanded;

        private void OnEnable()
        {
            manager.FpcInput.OnJumpAction += GetJumpInput;
        }

        private void OnDisable()
        {
            manager.FpcInput.OnJumpAction -= GetJumpInput;
        }
        
        private void Update()
        {
            if (!manager.CharacterController.enabled) return;
            
            HandleMovement();
            HandleJump();
            CheckGrounded();
            
            manager.CharacterController.Move(_velocity * Time.deltaTime);
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
            if (!manager.CharacterController.enabled) return;

            Vector3 cameraForward = manager.FpcCamera.GetMovementDirection();
            Vector3 cameraRight = Quaternion.Euler(0, 90, 0) * cameraForward;
            Vector3 moveDir = (cameraForward * manager.FpcInput.MoveInput.y + cameraRight * manager.FpcInput.MoveInput.x).normalized;
            
            float targetMoveSpeed = IsRunning ? runSpeed : walkSpeed;
            if (manager.FpcInteraction.HeldObject)
            {
                targetMoveSpeed /= manager.FpcInteraction.HeldObject.ObjectWeight;
            }

            _velocity.x = moveDir.x * targetMoveSpeed;
            _velocity.z = moveDir.z * targetMoveSpeed;
        }

        private void HandleJump()
        {
            if (!manager.CharacterController.enabled) return;

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
        }

        private void CheckGrounded()
        {
            _wasGrounded  = IsGrounded; 
            
            IsGrounded = manager.CharacterController.isGrounded;
            IsFalling = _velocity.y < 0;
            
            if (IsGrounded && !_wasGrounded)
            {
               OnLanded?.Invoke(Mathf.Abs(_velocity.y));
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
    
}
