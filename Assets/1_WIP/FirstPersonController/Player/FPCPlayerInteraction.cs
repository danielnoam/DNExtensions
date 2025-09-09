using System;
using DNExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(FpcPlayerMovement))]
[RequireComponent(typeof(FPCPlayerCamera))]
public class FPCPlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private LayerMask interactionLayer;
    
    [Header("Held Object Settings")]
    [SerializeField] private float autoDropYOffset = 1f;
    [SerializeField, MinMaxRange(0,30)] private RangedFloat throwForceRange = new RangedFloat(5f, 15f);
    [SerializeField, MinMaxRange(1f,4f)] private RangedFloat throwHeldRange = new RangedFloat(1f, 4f);
    
    [Header("References")]
    [SerializeField] private FPCPlayerCamera fpcPlayerCamera;
    [SerializeField] private FPCPlayerInput fpcPlayerInput;
    [SerializeField] private FpcPlayerMovement fpcPlayerMovement;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform holdPosition;
    [SerializeField] private Transform interactionPosition;
    
    
    private Interactable _closestInteractable;
    private PickableObject _heldObject;
    private bool _throwInputHeld;
    private float _throwInputHoldTime;
    
    
    public Transform HoldPosition => holdPosition;

    public PickableObject HeldObject
    {
        get => _heldObject;
        set
        {
            if (_heldObject == value) return;

            if (_heldObject)
            {
                _heldObject.Drop();
                _heldObject = null;
            }
            
            _heldObject = value;
        }
    }

    private void OnValidate()
    {
        if (!fpcPlayerCamera) fpcPlayerCamera = GetComponent<FPCPlayerCamera>();
        if (!fpcPlayerInput) fpcPlayerInput = GetComponent<FPCPlayerInput>();
        if (!fpcPlayerMovement) fpcPlayerMovement = GetComponent<FpcPlayerMovement>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!interactionPosition) interactionPosition = transform;
    }


    private void OnEnable()
    {
        fpcPlayerInput.OnInteractAction += OnInteract;
        fpcPlayerInput.OnThrowAction += OnThrow;
        fpcPlayerInput.OnDropAction += OnDrop;
    }

    private void OnDisable()
    {
        fpcPlayerInput.OnInteractAction -= OnInteract;
        fpcPlayerInput.OnThrowAction -= OnThrow;
        fpcPlayerInput.OnDropAction -= OnDrop;
    }
    
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (_closestInteractable)
            {
                _closestInteractable.Interact(this);
            }
        }
    }
    
    private void OnThrow(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _throwInputHeld = true;
        }
        else if (context.canceled)
        {
            _throwInputHeld = false;
            ThrowHeldObject();
        }


        _throwInputHoldTime = 0f;
    }
    
    private void OnDrop(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DropHeldObject();
        }
    }

    private void Update()
    {
        UpdateHeldInputTime();
    }

    private void FixedUpdate()
    {
        CheckForInteractable();
        CheckHeldObjectHeight();
    }

    private void ThrowHeldObject()
    {
        if (!_heldObject) return;
        
        var force = throwForceRange.Lerp(_throwInputHoldTime / throwHeldRange.maxValue);
        _heldObject.Throw(fpcPlayerCamera.GetAimDirection(), force);
        _heldObject = null;
    }

    private void DropHeldObject()
    {
        _heldObject?.Drop();
        _heldObject = null;
    }

    private void CheckHeldObjectHeight()
    {
        if (!_heldObject) return;
        
        if (_heldObject.transform.position.y < (transform.position.y - autoDropYOffset))
        {
            DropHeldObject();
        }
    }

    private void UpdateHeldInputTime()
    {
        if (!_heldObject) return;
        
        if (_throwInputHeld && _throwInputHoldTime < throwHeldRange.maxValue)
        {
            _throwInputHoldTime += Time.deltaTime;
        }
    }

    
    private void CheckForInteractable()
    {
        var colliders = Physics.OverlapSphere(
            interactionPosition.position, 
            interactionRadius, 
            interactionLayer
        );

        Interactable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out Interactable interactable))
            {
                if (!interactable.CanInteract) continue;

                float distance = Vector3.Distance(interactionPosition.position, col.transform.position);
            
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != _closestInteractable)
        {
            if (_closestInteractable) _closestInteractable.UnHighlight();
        
            _closestInteractable = closestInteractable;
    
            if (_closestInteractable) _closestInteractable.Highlight();
        }
    }
    
#if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        if (interactionPosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(interactionPosition.position, interactionRadius);
        }
        
        if (holdPosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(holdPosition.position, 0.3f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position.RemoveY(autoDropYOffset), 0.1f);
        Handles.Label(transform.position.RemoveY(autoDropYOffset), "Held object auto drop point");
    }
#endif
    

}