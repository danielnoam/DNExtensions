using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DNExtensions.Systems.FirstPersonController.Interactable;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Base class for first-person locomotion. Handles ground detection, crouching, jumping and gravity,
    /// and leaves the horizontal velocity model to the derived controller.
    /// </summary>
    [RequireComponent(typeof(FpcManager))]
    public abstract class FPCLocomotionBase : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] protected float gravity = -15f;
        [SerializeField] protected LayerMask collisionLayers = 0;

        [Header("Run")]
        [SerializeField] protected bool allowRun = true;
        [SerializeField, ShowIf(nameof(allowRun))] protected bool allowStartRunInAir;
        [SerializeField, ShowIf(nameof(allowRun))] protected bool allowCrouchRun = true;
        [Tooltip("Running while crouched stands the player up instead of the run being ignored. Needs the headroom to stand, and while run is held a crouch will not take")]
        [SerializeField, ShowIf(nameof(ShowStandUpWhenRunning))] protected bool standUpWhenRunning;

        [Header("Crouch")]
        [SerializeField] protected bool allowCrouch = true;
        [SerializeField, ShowIf(nameof(allowCrouch))] protected bool disableCrouchWhenJumping = true;
        [SerializeField, ShowIf(nameof(allowCrouch))] protected float crouchSpeedMultiplier = 0.5f;
        [SerializeField, ShowIf(nameof(allowCrouch))] protected float crouchHeight = 1.5f;
        [Tooltip("Crouching in the air holds the head still and tucks the feet up instead, so a crouch jump clears a ledge a normal jump cannot")]
        [SerializeField, ShowIf(nameof(allowCrouch))] private bool airCrouchRaisesFeet = true;

        [Header("Jump")]
        [SerializeField] protected bool allowJump = true;
        [SerializeField, ShowIf(nameof(allowJump))] protected float jumpForce = 1.5f;
        [SerializeField, ShowIf(nameof(allowJump))] protected float jumpBufferTime = 0.1f;
        [SerializeField, ShowIf(nameof(allowJump))] protected float coyoteTime = 0.1f;

        [SerializeField, AutoGetSelf, HideInInspector] protected FpcManager manager;
        private const float StandingHeightPadding = 0.05f;
        private const float GroundedVerticalVelocity = -2f;
        private const float LandingMinAirTime = 0.05f;

        private float _standingHeight;
        private Vector3 _standingColliderCenter;
        private float _jumpBufferCounter;
        private float _coyoteTimeCounter;
        private bool _wasGrounded;
        private bool _runInitiatedOnGround;
        private float _airTime;
        private Vector3 _velocity;

        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsFalling { get; private set; }
        public bool IsRunning { get; private set; }
        public Vector3 Velocity => _velocity;
        public Vector3 HorizontalVelocity => new Vector3(_velocity.x, 0f, _velocity.z);
        public float HorizontalSpeed => HorizontalVelocity.magnitude;

        protected bool HasBufferedJump => _jumpBufferCounter > 0f;
        private bool IsAirCrouching => airCrouchRaisesFeet && !IsGrounded;

        public event Action OnJump;
        public event Action<float> OnLanded;

        protected virtual void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        /// <summary>Standing up to run only means anything while crouch running is off, so it is only offered there.</summary>
        private bool ShowStandUpWhenRunning() => allowRun && !allowCrouchRun;

        protected virtual void Awake()
        {
            _standingColliderCenter = manager.CharacterController.center;
            _standingHeight = manager.CharacterController.height;

            if (allowCrouch && !Mathf.Approximately(_standingColliderCenter.y, _standingHeight / 2f))
            {
                Debug.LogError($"{name} character collider is not anchored to the feet, so crouching will move the player. Run Anchor Collider To Feet on the FPC Manager.", this);
            }
        }

        protected virtual void OnEnable()
        {
            manager.FpcInput.OnJumpAction += OnJumpInput;
            manager.FpcInput.OnCrouchAction += OnCrouchInput;
        }

        protected virtual void OnDisable()
        {
            manager.FpcInput.OnJumpAction -= OnJumpInput;
            manager.FpcInput.OnCrouchAction -= OnCrouchInput;
        }

        protected virtual void Update()
        {
            if (!manager.CharacterController.enabled) return;

            float deltaTime = Time.deltaTime;

            CheckGrounded();
            CheckRunning();
            HandleMovement(deltaTime);
            HandleJump(deltaTime);
            HandleGravity(deltaTime);

            manager.CharacterController.Move(_velocity * deltaTime);
        }

        /// <summary>
        /// Sets the horizontal velocity for this frame. Vertical velocity is handled by the base class.
        /// </summary>
        protected abstract void HandleMovement(float deltaTime);

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                _jumpBufferCounter = jumpBufferTime;
            }
        }

        private void OnCrouchInput(InputAction.CallbackContext context)
        {
            if (!allowCrouch) return;

            if (context.phase == InputActionPhase.Started)
            {
                OnCrouchPressed();
            }
            else if (context.phase == InputActionPhase.Canceled && !manager.FpcInput.ToggleCrouch)
            {
                OnCrouchReleased();
            }
        }

        protected virtual void OnCrouchPressed()
        {
            if (IsCrouching) TryStand();
            else Crouch();
        }

        protected virtual void OnCrouchReleased()
        {
            TryStand();
        }

        protected void Crouch()
        {
            SetColliderHeight(crouchHeight);
            IsCrouching = true;
        }

        /// <summary>
        /// Resizes the character collider around its feet, so only the top of the capsule moves. In the air the
        /// player is shifted to keep the head still instead, leaving the feet to move.
        /// </summary>
        private void SetColliderHeight(float height)
        {
            CharacterController controller = manager.CharacterController;
            float heightDelta = controller.height - height;

            controller.height = height;
            controller.center = new Vector3(_standingColliderCenter.x, height / 2f, _standingColliderCenter.z);

            if (IsAirCrouching)
            {
                MoveWithoutCollision(Vector3.up * heightDelta);
            }
        }

        private void MoveWithoutCollision(Vector3 offset)
        {
            CharacterController controller = manager.CharacterController;

            // A CharacterController holds its own copy of the position, so it has to be taken out of the way for a
            // direct transform write to survive.
            controller.enabled = false;
            transform.position += offset;
            controller.enabled = true;
        }

        /// <summary>
        /// Restores the standing collider, unless there is no room for it.
        /// </summary>
        protected bool TryStand()
        {
            if (!IsCrouching) return true;
            if (!HasRoomToStand()) return false;

            SetColliderHeight(_standingHeight);
            IsCrouching = false;
            return true;
        }

        private bool HasRoomToStand()
        {
            float rayLength = _standingHeight - manager.CharacterController.height + StandingHeightPadding;

            // Standing up in the air puts the feet back down rather than the head up, so the room has to be below.
            if (IsAirCrouching)
            {
                return !Physics.Raycast(transform.position, Vector3.down, rayLength, collisionLayers);
            }

            Vector3 headTop = transform.position + Vector3.up * manager.CharacterController.height;

            return !Physics.Raycast(headTop, Vector3.up, rayLength, collisionLayers);
        }

        /// <summary>
        /// Standing out of a crouch because run is being held. Its own method rather than a call
        /// straight to TryStand so a controller where a crouch means more than a crouch — a slide,
        /// say — can refuse or take more with it. Returns whether the player is now standing.
        /// </summary>
        protected virtual bool StandUpToRun() => TryStand();

        protected virtual void CheckRunning()
        {
            if (!manager.FpcInput.RunInput || !allowRun)
            {
                _runInitiatedOnGround = false;
                IsRunning = false;
                return;
            }

            if (IsGrounded)
            {
                _runInitiatedOnGround = true;
            }

            // Only worth trying where the run would otherwise be refused outright. It can still fail
            // on headroom, and then the crouch simply stays and the run does not start. Run winning
            // over crouch also means a crouch pressed while run is held is undone on the same frame,
            // which is the point of the option: one key cancels the other rather than blocking it.
            if (standUpWhenRunning && IsCrouching && !allowCrouchRun) StandUpToRun();

            bool canRun = !IsCrouching || allowCrouchRun;
            bool canRunInAir = allowStartRunInAir || _runInitiatedOnGround;
            IsRunning = canRun && (IsGrounded || canRunInAir);
        }

        protected virtual void HandleJump(float deltaTime)
        {
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter -= deltaTime;
            }

            if (!allowJump) return;

            if (_jumpBufferCounter > 0f && (_coyoteTimeCounter > 0f || IsGrounded))
            {
                _jumpBufferCounter = 0f;
                _coyoteTimeCounter = 0f;
                PerformJump();
            }
        }

        protected virtual void PerformJump()
        {
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (disableCrouchWhenJumping && IsCrouching) TryStand();
            OnJump?.Invoke();
        }

        protected virtual void HandleGravity(float deltaTime)
        {
            _velocity.y += gravity * deltaTime;
        }

        protected virtual void CheckGrounded()
        {
            _wasGrounded = IsGrounded;

            IsGrounded = manager.CharacterController.isGrounded;

            // Grounded is part of the question, not just the sign of the vertical velocity. Standing
            // on the floor holds that velocity at GroundedVerticalVelocity and gravity pushes it
            // further down every frame, so a test on the sign alone was true the whole time the
            // player was stood still.
            IsFalling = !IsGrounded && _velocity.y < 0f;

            if (IsGrounded)
            {
                // Ground contact can drop for a single frame when the collider is resized, which is not a landing.
                if (!_wasGrounded && _airTime > LandingMinAirTime)
                {
                    OnLanded?.Invoke(Mathf.Abs(_velocity.y));
                }

                _airTime = 0f;

                if (_velocity.y < 0)
                {
                    _velocity.y = GroundedVerticalVelocity;
                }
                _coyoteTimeCounter = coyoteTime;
            }
            else if (_wasGrounded)
            {
                _airTime += Time.deltaTime;
                _coyoteTimeCounter = coyoteTime;
            }
            else
            {
                _airTime += Time.deltaTime;
                _coyoteTimeCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// The camera relative direction the player is trying to move in, or zero when there is no input.
        /// </summary>
        protected Vector3 GetWishDirection()
        {
            Vector3 cameraForward = manager.FpcCamera.GetMovementDirection();
            Vector3 cameraRight = Quaternion.Euler(0, 90, 0) * cameraForward;
            Vector2 moveInput = manager.FpcInput.MoveInput;

            return (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
        }

        /// <summary>
        /// Speed scale applied by a carried object, based on its weight.
        /// </summary>
        protected float GetHeldObjectSpeedMultiplier()
        {
            PickableObject heldObject = manager.FpcInteraction.HeldObject;

            return heldObject && heldObject.ObjectWeight > 0f ? 1f / heldObject.ObjectWeight : 1f;
        }

        protected void SetHorizontalVelocity(Vector3 horizontalVelocity)
        {
            _velocity.x = horizontalVelocity.x;
            _velocity.z = horizontalVelocity.z;
        }

        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        public void AddVelocity(Vector3 velocity)
        {
            _velocity += velocity;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (allowCrouch && IsCrouching)
            {
                float crouchHeadY = manager.CharacterController.height;
                float rayLength = _standingHeight - crouchHeadY + StandingHeightPadding;

                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position + Vector3.up * crouchHeadY, Vector3.up * rayLength);
            }
        }
    }
}
