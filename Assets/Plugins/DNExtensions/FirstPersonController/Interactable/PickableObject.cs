using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AudioEvent;
using UnityEngine;

[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(AudioSource))]
public class PickableObject : MonoBehaviour
{

    [Header("Pickable Object Settings")]
    [Tooltip( "Affects the players movement speed when this object is held, 1 has no effect.")]
    [SerializeField, Min(1)] private float objectWeight = 1f;
    [SerializeField] private float heldFollowForce = 15f;
    [SerializeField] protected Rigidbody rigidBody;
    [SerializeField] protected Interactable interactable;
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] private SOAudioEvent collisionSfx;

    private bool _isBeingHeld;
    private Transform _holdPosition;
    
    public float ObjectWeight => objectWeight;
    
    private void OnValidate()
    {
        if (!rigidBody) rigidBody = this.GetOrAddComponent<Rigidbody>();
        if (!interactable) interactable = this.GetOrAddComponent<Interactable>();
        if (!audioSource) audioSource = this.GetOrAddComponent<AudioSource>();
    }

    protected virtual void OnEnable()
    {
        interactable.OnInteract += OnInteract;
        
    }
    
    protected virtual void OnDisable()
    {
        interactable.OnInteract -= OnInteract;
    }   
    
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 0.3f)
        {
            collisionSfx?.Play(audioSource);
        }
    }
    
    private void OnInteract(FPCInteraction interactor)
    {
        if (!rigidBody || !interactor) return;

        if (!_isBeingHeld)
        {
            PickUp(interactor);
        }

    }
    

    private void FixedUpdate()
    {
        FollowHoldPosition();
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
    
    
    private void PickUp(FPCInteraction interactor)
    {
        if (!rigidBody || _isBeingHeld) return;

        interactable?.SetCanInteract(false);
        rigidBody.useGravity = true;
        _isBeingHeld = true;
        _holdPosition = interactor.HoldPosition;
        interactor.HeldObject = this;
    }

    public void Drop()
    {
        if (!rigidBody || !_isBeingHeld) return;
        interactable?.SetCanInteract(true);
        rigidBody.useGravity = true;
        _isBeingHeld = false;
        _holdPosition = null;
    }

    public void Throw(Vector3 direction, float force)
    {
        if (!rigidBody) return;

        interactable?.SetCanInteract(true);
        rigidBody.useGravity = true;
        _isBeingHeld = false;
        _holdPosition = null;
        rigidBody.AddForce(direction * force, ForceMode.Impulse);
    }
    


    

}
