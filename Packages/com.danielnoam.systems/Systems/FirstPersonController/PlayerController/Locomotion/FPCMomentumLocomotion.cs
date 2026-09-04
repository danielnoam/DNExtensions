using DNExtensions.Utilities;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Momentum based locomotion. Horizontal velocity is built up through acceleration and bled off through
    /// friction, so speed carries between jumps, air strafing steers instead of setting velocity, and crouching
    /// at speed starts a slide.
    /// </summary>
    [AddComponentMenu("")]
    public class FPCMomentumLocomotion : FPCLocomotionBase
    {
        [Header("Ground Movement")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float runSpeed = 11f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float groundFriction = 8f;
        [Tooltip("Speed below which friction is applied at a constant rate, so the player comes to a full stop")]
        [SerializeField] private float stopSpeed = 2f;

        [Header("Air Movement")]
        [Tooltip("How hard the player can steer while airborne")]
        [SerializeField] private float airAcceleration = 50f;
        [Tooltip("Speed that can be gained in the steering direction while airborne, keep low to allow air strafing")]
        [SerializeField] private float airSpeedCap = 1.5f;
        [SerializeField] private float airFriction;

        [Header("Momentum")]
        [Tooltip("Hard limit on horizontal speed, 0 for no limit")]
        [SerializeField, Min(0f)] private float maxSpeed = 30f;
        [Tooltip("Skips ground friction on the frame a buffered jump fires, so chained jumps keep their speed")]
        [SerializeField] private bool allowBunnyHop = true;
        [Tooltip("Extra speed added along the current direction on every jump")]
        [SerializeField, ShowIf(nameof(allowBunnyHop))] private float bunnyHopSpeedBonus = 0.5f;

        [Header("Slide")]
        [SerializeField] private bool allowSlide = true;
        [Tooltip("Minimum speed needed to start a slide instead of a normal crouch")]
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideMinSpeed = 6f;
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideExitSpeed = 3f;
        [Tooltip("Speed added in the current direction when a slide starts")]
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideImpulse = 4f;
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideFriction = 1.5f;
        [Tooltip("How much the player can steer while sliding")]
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideSteerAcceleration = 8f;
        [Tooltip("Speed gained per second when sliding downhill, 0 to ignore slopes")]
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideSlopeAcceleration = 12f;
        [Tooltip("Maximum slide length in seconds, 0 for no limit")]
        [SerializeField, ShowIf(nameof(allowSlide))] private float slideMaxDuration = 1.5f;

        private bool _isSliding;
        private float _slideTimer;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _pendingGroundNormal = Vector3.up;

        public bool IsSliding => _isSliding;

        private bool CrouchHeld => manager.FpcInput.ToggleCrouch || manager.FpcInput.CrouchInput;

        protected override void CheckGrounded()
        {
            base.CheckGrounded();

            _groundNormal = IsGrounded ? _pendingGroundNormal : Vector3.up;
            _pendingGroundNormal = Vector3.up;
        }

        protected override void HandleMovement(float deltaTime)
        {
            UpdateSlide(deltaTime);

            Vector3 velocity = HorizontalVelocity;
            Vector3 wishDirection = GetWishDirection();
            float wishSpeed = GetWishSpeed();

            if (_isSliding)
            {
                ApplyFriction(ref velocity, slideFriction, deltaTime);
                ApplySlopeAcceleration(ref velocity, deltaTime);
                Accelerate(ref velocity, wishDirection, wishSpeed, slideSteerAcceleration, deltaTime);
            }
            else if (IsGrounded)
            {
                if (!allowBunnyHop || !HasBufferedJump)
                {
                    ApplyFriction(ref velocity, groundFriction, deltaTime);
                }
                Accelerate(ref velocity, wishDirection, wishSpeed, groundAcceleration, deltaTime);
            }
            else
            {
                ApplyFriction(ref velocity, airFriction, deltaTime);
                Accelerate(ref velocity, wishDirection, Mathf.Min(wishSpeed, airSpeedCap), airAcceleration, deltaTime);
            }

            if (maxSpeed > 0f && velocity.magnitude > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }

            SetHorizontalVelocity(velocity);
        }

        protected override void PerformJump()
        {
            if (_isSliding)
            {
                StopSlide();
            }

            if (allowBunnyHop && bunnyHopSpeedBonus > 0f)
            {
                Vector3 velocity = HorizontalVelocity;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    SetHorizontalVelocity(velocity + velocity.normalized * bunnyHopSpeedBonus);
                }
            }

            base.PerformJump();
        }

        /// <summary>
        /// A slide is not a crouch to be stood out of. It is entered from a run and so nearly always
        /// with run still held, which would cancel it on the frame it started and leave sliding
        /// unreachable. The slide keeps its own exit conditions — speed, duration, releasing crouch —
        /// and the run only stands the player up once it is over.
        /// </summary>
        protected override bool StandUpToRun()
        {
            return !_isSliding && base.StandUpToRun();
        }

        protected override void OnCrouchPressed()
        {
            if (_isSliding)
            {
                StopSlide();
                TryStand();
                return;
            }

            if (allowSlide && IsGrounded && !IsCrouching && HorizontalSpeed >= slideMinSpeed)
            {
                StartSlide();
                return;
            }

            base.OnCrouchPressed();
        }

        protected override void OnCrouchReleased()
        {
            if (_isSliding)
            {
                StopSlide();
            }

            base.OnCrouchReleased();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.1f)
            {
                _pendingGroundNormal = hit.normal;
            }
        }

        private float GetWishSpeed()
        {
            float wishSpeed = IsRunning ? runSpeed : walkSpeed;

            if (IsCrouching && !_isSliding)
            {
                wishSpeed *= crouchSpeedMultiplier;
            }

            return wishSpeed * GetHeldObjectSpeedMultiplier();
        }

        private void StartSlide()
        {
            Crouch();
            _isSliding = true;
            _slideTimer = 0f;

            Vector3 velocity = HorizontalVelocity;
            Vector3 slideDirection = velocity.sqrMagnitude > 0.01f ? velocity.normalized : transform.forward;
            SetHorizontalVelocity(velocity + slideDirection * slideImpulse);
        }

        private void StopSlide()
        {
            _isSliding = false;
            _slideTimer = 0f;
        }

        private void UpdateSlide(float deltaTime)
        {
            if (!_isSliding) return;

            _slideTimer += deltaTime;

            bool durationExpired = slideMaxDuration > 0f && _slideTimer >= slideMaxDuration;
            if (IsGrounded && !durationExpired && HorizontalSpeed > slideExitSpeed) return;

            StopSlide();

            if (!CrouchHeld)
            {
                TryStand();
            }
        }

        private void ApplySlopeAcceleration(ref Vector3 velocity, float deltaTime)
        {
            if (slideSlopeAcceleration <= 0f || _groundNormal.y >= 0.999f) return;

            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, _groundNormal).normalized;
            float steepness = 1f - _groundNormal.y;

            velocity += downhill * (slideSlopeAcceleration * steepness * deltaTime);
        }

        private void ApplyFriction(ref Vector3 velocity, float friction, float deltaTime)
        {
            if (friction <= 0f) return;

            float speed = velocity.magnitude;
            if (speed < 0.01f)
            {
                velocity = Vector3.zero;
                return;
            }

            float drop = Mathf.Max(speed, stopSpeed) * friction * deltaTime;
            velocity *= Mathf.Max(speed - drop, 0f) / speed;
        }

        private static void Accelerate(ref Vector3 velocity, Vector3 wishDirection, float wishSpeed, float acceleration, float deltaTime)
        {
            float currentSpeed = Vector3.Dot(velocity, wishDirection);
            float addSpeed = wishSpeed - currentSpeed;
            if (addSpeed <= 0f) return;

            float accelerationSpeed = Mathf.Min(acceleration * wishSpeed * deltaTime, addSpeed);
            velocity += wishDirection * accelerationSpeed;
        }
    }
}
