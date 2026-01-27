using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.RangedValues;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


[DisallowMultipleComponent]
[RequireComponent(typeof(FPCManager))]
public class FPCInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private LayerMask interactionLayer;
    
    [Header("Held Object Settings")]
    [SerializeField] private float autoDropYOffset = 1f;
    [SerializeField, MinMaxRange(0,30)] private RangedFloat throwForceRange = new RangedFloat(5f, 15f);
    [SerializeField, MinMaxRange(1f,4f)] private RangedFloat throwHeldRange = new RangedFloat(1f, 4f);
    
    [Header("References")]
    [SerializeField] private FPCManager manager;
    [SerializeField] private Transform holdPosition;
    [SerializeField] private Transform interactionPosition;
    
    
    [Header("Debug")]
    [SerializeField] private bool drawInformation;
    
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
        if (!manager) manager = GetComponent<FPCManager>();
        if (!interactionPosition) interactionPosition = transform;
    }


    private void OnEnable()
    {
        manager.FPCInput.OnInteractAction += OnInteract;
        manager.FPCInput.OnThrowAction += OnThrow;
        manager.FPCInput.OnDropAction += OnDrop;
    }

    private void OnDisable()
    {
        manager.FPCInput.OnInteractAction -= OnInteract;
        manager.FPCInput.OnThrowAction -= OnThrow;
        manager.FPCInput.OnDropAction -= OnDrop;
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
        _heldObject.Throw(manager.FPCCamera.GetAimDirection(), force);
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
    


    private void OnDrawGizmos()
    {
        if (!drawInformation) return;
        
#if UNITY_EDITOR

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
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position.RemoveY(autoDropYOffset), 0.1f);
        Handles.Label(transform.position.RemoveY(autoDropYOffset), "Auto drop distance");
        
#endif
    }

    

}