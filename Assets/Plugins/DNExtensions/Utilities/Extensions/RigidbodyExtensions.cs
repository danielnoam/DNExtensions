using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class RigidbodyExtensions
    {
        /// <summary>
        /// Changes the direction of the Rigidbody's velocity while maintaining its speed
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to change direction</param>
        /// <param name="direction">The new direction for the Rigidbody</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody ChangeDirection(this Rigidbody rigidbody, Vector3 direction)
        {
            if (!rigidbody) return null;
            if (direction.sqrMagnitude == 0f) return rigidbody;
            
            direction.Normalize();
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = direction * rigidbody.linearVelocity.magnitude;
#else
            rigidbody.velocity = direction * rigidbody.velocity.magnitude;
#endif
            return rigidbody;
        }

        /// <summary>
        /// Stops the Rigidbody by setting its linear and angular velocities to zero
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to stop</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody Stop(this Rigidbody rigidbody)
        {
            if (!rigidbody) return null;
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
#else
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
#endif
            return rigidbody;
        }

        /// <summary>
        /// Sets the linear velocity of the Rigidbody
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to modify</param>
        /// <param name="velocity">The new velocity</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody SetVelocity(this Rigidbody rigidbody, Vector3 velocity)
        {
            if (!rigidbody) return null;
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = velocity;
#else
            rigidbody.velocity = velocity;
#endif
            return rigidbody;
        }

        /// <summary>
        /// Adds velocity to the Rigidbody
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to modify</param>
        /// <param name="velocity">The velocity to add</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody AddVelocity(this Rigidbody rigidbody, Vector3 velocity)
        {
            if (!rigidbody) return null;
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity += velocity;
#else
            rigidbody.velocity += velocity;
#endif
            return rigidbody;
        }

        /// <summary>
        /// Gets the current linear velocity (Unity 6 compatible)
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to query</param>
        /// <returns>The current linear velocity</returns>
        public static Vector3 GetVelocity(this Rigidbody rigidbody)
        {
            if (!rigidbody) return Vector3.zero;
            
#if UNITY_6000_0_OR_NEWER
            return rigidbody.linearVelocity;
#else
            return rigidbody.velocity;
#endif
        }

        /// <summary>
        /// Clamps the Rigidbody's velocity to a maximum magnitude
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to clamp</param>
        /// <param name="maxSpeed">The maximum speed allowed</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody ClampVelocity(this Rigidbody rigidbody, float maxSpeed)
        {
            if (!rigidbody) return null;
            
#if UNITY_6000_0_OR_NEWER
            if (rigidbody.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * maxSpeed;
            }
#else
            if (rigidbody.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                rigidbody.velocity = rigidbody.velocity.normalized * maxSpeed;
            }
#endif
            return rigidbody;
        }

        /// <summary>
        /// Sets only the horizontal velocity (X and Z), preserving vertical (Y)
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to modify</param>
        /// <param name="horizontalVelocity">The new horizontal velocity (X and Z)</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody SetHorizontalVelocity(this Rigidbody rigidbody, Vector2 horizontalVelocity)
        {
            if (!rigidbody) return null;
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = new Vector3(horizontalVelocity.x, rigidbody.linearVelocity.y, horizontalVelocity.y);
#else
            rigidbody.velocity = new Vector3(horizontalVelocity.x, rigidbody.velocity.y, horizontalVelocity.y);
#endif
            return rigidbody;
        }

        /// <summary>
        /// Sets only the vertical velocity (Y), preserving horizontal (X and Z)
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to modify</param>
        /// <param name="verticalVelocity">The new vertical velocity</param>
        /// <returns>The modified Rigidbody for method chaining</returns>
        public static Rigidbody SetVerticalVelocity(this Rigidbody rigidbody, float verticalVelocity)
        {
            if (!rigidbody) return null;
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, verticalVelocity, rigidbody.linearVelocity.z);
#else
            rigidbody.velocity = new Vector3(rigidbody.velocity.x, verticalVelocity, rigidbody.velocity.z);
#endif
            return rigidbody;
        }
    }
}