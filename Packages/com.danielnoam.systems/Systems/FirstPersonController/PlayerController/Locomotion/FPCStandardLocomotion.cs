using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Locomotion with instant acceleration. Horizontal velocity always matches the input direction
    /// at a fixed speed, both on the ground and in the air.
    /// </summary>
    [AddComponentMenu("")]
    public class FPCStandardLocomotion : FPCLocomotionBase
    {
        [Header("Speed")]
        [SerializeField] private float walkSpeed = 8f;
        [SerializeField] private float runSpeed = 12f;

        protected override void HandleMovement(float deltaTime)
        {
            Vector3 wishDirection = GetWishDirection();
            float targetSpeed = IsRunning && (allowStartRunInAir || IsGrounded) ? runSpeed : walkSpeed;

            if (IsCrouching)
            {
                targetSpeed *= crouchSpeedMultiplier;
            }

            targetSpeed *= GetHeldObjectSpeedMultiplier();

            SetHorizontalVelocity(wishDirection * targetSpeed);
        }
    }
}
