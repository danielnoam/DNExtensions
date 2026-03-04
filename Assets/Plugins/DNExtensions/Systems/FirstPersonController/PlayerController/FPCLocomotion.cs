
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
    public class FPCLocomotion : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 8f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private LayerMask collisionLayers = 0;
        
        [Header("Run")]
        [SerializeField] private bool canRun = true;
        [SerializeField] private bool allowCrouchRun = true;
        [SerializeField, ShowIf("canRun")] private float runSpeed = 12f;
        
        [Header("Crouch")]
        [SerializeField] private bool canCrouch = true;
        [SerializeField] private float crouchSpeedMultiplier = 0.5f;
        [SerializeField] private float crouchHeight = 0.5f;
        [SerializeField] private Vector3 crouchColliderCenter = new Vector3(0, 0.25f, 0);
        
        [Header("Jump")]
        [SerializeField] private float jumpForce = 1.5f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float coyoteTime = 0.1f;
        
        
        [SerializeField, AutoGetSelf, HideInInspector] private FpcManager manager;
        private const float StandingHeightPadding = 0.05f;
        
        private float _standingHeight;
        private float _standingHeadY;
        private Vector3 _standingColliderCenter;
        private Vector3 _velocity;
        private float _jumpBufferCounter;
        private float _coyoteTimeCounter;
        private bool _wasGrounded;
        

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsFalling { get; private set; }
        
        public bool IsRunning => manager.FpcInput.RunInput && canRun && (!IsCrouching || allowCrouchRun);
        public Vector3 Velocity => _velocity;

        public event Action OnJump;
        public event Action<float> OnLanded;

        private void Awake()
        {
            _standingColliderCenter = manager.CharacterController.center;
            _standingHeight = manager.CharacterController.height;
            _standingHeadY = _standingColliderCenter.y + _standingHeight / 2f;
        }

        private void OnEnable()
        {
            manager.FpcInput.OnJumpAction += OnJumpInput;
            manager.FpcInput.OnCrouchAction += OnCrouchInput;
        }

        private void OnDisable()
        {
            manager.FpcInput.OnJumpAction -= OnJumpInput;
            manager.FpcInput.OnCrouchAction -= OnCrouchInput;
        }
        
        private void Update()
        {
            if (!manager.CharacterController.enabled) return;
            
            HandleMovement();
            HandleJump();
            CheckGrounded();
            
            manager.CharacterController.Move(_velocity * Time.deltaTime);
        }
        
            
        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                _jumpBufferCounter = jumpBufferTime;
            }
        }
        
        private void OnCrouchInput(InputAction.CallbackContext context)
        {
            if (!canCrouch) return;

            if (context.phase == InputActionPhase.Started)
            {
                if (IsCrouching) TryStand();
                else Crouch();
            }
            else if (context.phase == InputActionPhase.Canceled && !manager.FpcInput.ToggleCrouch)
            {
                TryStand();
            }
        }
        
        private void Crouch()
        {
            manager.CharacterController.height = crouchHeight;
            manager.CharacterController.center = crouchColliderCenter;
            IsCrouching = true;
        }

        private bool TryStand()
        {
            float crouchHeadY = manager.CharacterController.center.y + manager.CharacterController.height / 2f;
            float rayLength = _standingHeadY - crouchHeadY + StandingHeightPadding;
            Vector3 rayOrigin = transform.position + Vector3.up * crouchHeadY;

            if (Physics.Raycast(rayOrigin, Vector3.up, rayLength, collisionLayers))
            {
                return false;
            }

            manager.CharacterController.height = _standingHeight;
            manager.CharacterController.center = _standingColliderCenter;
            IsCrouching = false;
            return true;
        }

        private void HandleMovement()
        {
            if (!manager.CharacterController.enabled) return;

            Vector3 cameraForward = manager.FpcCamera.GetMovementDirection();
            Vector3 cameraRight = Quaternion.Euler(0, 90, 0) * cameraForward;
            Vector3 moveDir = (cameraForward * manager.FpcInput.MoveInput.y + cameraRight * manager.FpcInput.MoveInput.x).normalized;
            
            float targetMoveSpeed = IsRunning ? runSpeed : walkSpeed;
            
            if (IsCrouching)
            {
                targetMoveSpeed *= crouchSpeedMultiplier;
            }
            
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
                OnJump?.Invoke();
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

        private void OnDrawGizmosSelected()
        {
            if (canCrouch && IsCrouching)
            {
                float crouchHeadY = crouchColliderCenter.y + crouchHeight / 2f;
                float rayLength = _standingHeadY - crouchHeadY + StandingHeightPadding;

                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position + Vector3.up * crouchHeadY, Vector3.up * rayLength);
            }
        }
    }
    
}
