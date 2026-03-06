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
    public class PickableObject : MonoBehaviour, IInteractable
    {
        [Header("Pickable Object Settings")]
        [Tooltip( "Affects the players movement speed when this object is held, 1 has no effect.")]
        [SerializeField, Min(1)] private float objectWeight = 1f;
        [SerializeField] private float heldFollowForce = 15f;
        [SerializeField, AutoGetSelf, HideInInspector] private Rigidbody rigidBody;
        [SerializeField, AutoGetSelf, HideInInspector] private InteractableTip tip;

        private bool _isBeingHeld;
        private Transform _holdPosition;
        
        public float ObjectWeight => objectWeight;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void FixedUpdate()
        {
            FollowHoldPosition();
        }
        
        public void Interact(InteractorData interactorData)
        {
            if (!CanInteract()) return;

            PickUp(interactorData);
        }

        public bool CanInteract()
        {
            return !_isBeingHeld;
        }

        public void ShowInteractionTip()
        {
            tip?.ToggleTooltip(true);
        }

        public void HideInteractionTip()
        {
            tip?.ToggleTooltip(false);
        }

        private void FollowHoldPosition()
        {
            if (!_isBeingHeld || !_holdPosition) return;

            var targetPosition = Vector3.Lerp(rigidBody.position, _holdPosition.position, heldFollowForce * Time.fixedDeltaTime);
            rigidBody.MovePosition(targetPosition);
            rigidBody.angularVelocity = Vector3.zero;
        }

        private void PickUp(InteractorData interactorData)
        {
            if (!rigidBody || _isBeingHeld) return;
            
            rigidBody.useGravity = false;
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
            
            rigidBody.useGravity = true;
            _isBeingHeld = false;
            _holdPosition = null;
            rigidBody.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}