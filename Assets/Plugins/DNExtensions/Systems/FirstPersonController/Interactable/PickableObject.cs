using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController.Interactable
{
    /// <summary>
    /// Represents an object that can be picked up, held, dropped, and thrown by the player.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(InteractableBase))]
    [RequireComponent(typeof(AudioSource))]
    public class PickableObject : InteractableBase
    {
        [Header("Pickable Object Settings")]
        [Tooltip( "Affects the players movement speed when this object is held, 1 has no effect.")]
        [SerializeField, Min(1)] private float objectWeight = 1f;
        [SerializeField] private float heldFollowForce = 15f;
        [SerializeField, AutoGetSelf] private Rigidbody rigidBody;
        [SerializeField, AutoGetSelf] private InteractableBase interactable;
        [SerializeField, AutoGetSelf] private AudioSource audioSource;
        [SerializeField] private SOAudioEvent collisionSfx;

        private bool _isBeingHeld;
        private Transform _holdPosition;
        public float ObjectWeight => objectWeight;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 0.3f)
            {
                collisionSfx?.Play(audioSource);
            }
        }

        private void FixedUpdate()
        {
            FollowHoldPosition();
        }
        
        public override void Interact(InteractorData interactorData)
        {
            if (!CanInteract() || _isBeingHeld) return;

            PickUp(interactorData);
        }

        private void FollowHoldPosition()
        {
            if (!_isBeingHeld || !_holdPosition) return;

            var direction = _holdPosition.position - rigidBody.position;
            rigidBody.linearVelocity = direction * (heldFollowForce * Time.fixedDeltaTime);

            if (rigidBody.rotation != Quaternion.Euler(Vector3.zero))
            {
                Quaternion targetRotation = Quaternion.Euler(Vector3.zero);
                Quaternion rotationDifference = targetRotation * Quaternion.Inverse(rigidBody.rotation);
                rotationDifference.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;
                float angularSpeed = 5;

                Vector3 desiredAngularVelocity = axis * (angle * Mathf.Deg2Rad * angularSpeed);
                rigidBody.angularVelocity = desiredAngularVelocity;
            }
            else if (rigidBody.angularVelocity != Vector3.zero)
            {
                rigidBody.angularVelocity = Vector3.Lerp(rigidBody.angularVelocity, Vector3.zero, 1f * Time.fixedDeltaTime);
            }
        }

        private void PickUp(InteractorData interactorData)
        {
            if (!rigidBody || _isBeingHeld) return;

            canInteract = false;
            rigidBody.useGravity = true;
            _isBeingHeld = true;
            _holdPosition = interactorData.FpcInteraction.HoldPosition;
            interactorData.FpcInteraction.HeldObject = this;
        }

        /// <summary>
        /// Drops the held object, allowing it to be picked up again.
        /// </summary>
        public void Drop()
        {
            if (!rigidBody || !_isBeingHeld) return;
            
            canInteract = true;
            rigidBody.useGravity = true;
            _isBeingHeld = false;
            _holdPosition = null;
        }

        /// <summary>
        /// Throws the held object in the specified direction with the given force.
        /// </summary>
        public void Throw(Vector3 direction, float force)
        {
            if (!rigidBody) return;

            canInteract = true;
            rigidBody.useGravity = true;
            _isBeingHeld = false;
            _holdPosition = null;
            rigidBody.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}