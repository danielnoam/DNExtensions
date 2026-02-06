using System;
using DNExtensions.FirstPersonController.Interactable;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.RangedValues;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


[DisallowMultipleComponent]
[RequireComponent(typeof(FpcManager))]
public class FPCInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 3f;
    [SerializeField] private LayerMask interactionLayer = 0;
    
    [Header("Held Object Settings")]
    [SerializeField] private float autoDropYOffset = 1f;
    [SerializeField, MinMaxRange(0,30)] private RangedFloat throwForceRange = new RangedFloat(5f, 15f);
    [SerializeField, MinMaxRange(1f,4f)] private RangedFloat throwHeldRange = new RangedFloat(1f, 4f);
    
    [Header("References")]
    [SerializeField, AutoGetSelf] private FpcManager manager;
    [SerializeField] private Transform holdPosition;
    [SerializeField] private Transform interactionPosition;
    
    [Header("Debug")]
    [SerializeField] private bool drawInformation;
    [SerializeField, ReadOnly] private PickableObject heldObject;
    
    private IInteractable _closestInteractable;

    private bool _throwInputHeld;
    private float _throwInputHoldTime;
    
    public Transform HoldPosition => holdPosition;

    public PickableObject HeldObject
    {
        get => heldObject;
        set
        {
            if (heldObject == value) return;

            if (heldObject)
            {
                heldObject.Drop();
                heldObject = null;
            }
            
            heldObject = value;
        }
    }

    private void OnValidate()
    {
        if (!manager) manager = GetComponent<FpcManager>();
        if (!interactionPosition) interactionPosition = transform;
    }


    private void OnEnable()
    {
        manager.FpcInput.OnInteractAction += OnInteract;
        manager.FpcInput.OnThrowAction += OnThrow;
        manager.FpcInput.OnDropAction += OnDrop;
    }

    private void OnDisable()
    {
        manager.FpcInput.OnInteractAction -= OnInteract;
        manager.FpcInput.OnThrowAction -= OnThrow;
        manager.FpcInput.OnDropAction -= OnDrop;
    }
    
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var interactorData = new InteractorData(this);
            _closestInteractable?.Interact(interactorData);
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
        if (!heldObject) return;
        
        var force = throwForceRange.Lerp(_throwInputHoldTime / throwHeldRange.maxValue);
        heldObject.Throw(manager.FpcCamera.GetAimDirection(), force);
        heldObject = null;
    }

    private void DropHeldObject()
    {
        heldObject?.Drop();
        heldObject = null;
    }

    private void CheckHeldObjectHeight()
    {
        if (!heldObject) return;
        
        if (heldObject.transform.position.y < (transform.position.y - autoDropYOffset))
        {
            DropHeldObject();
        }
    }

    private void UpdateHeldInputTime()
    {
        if (!heldObject) return;
        
        if (_throwInputHeld && _throwInputHoldTime < throwHeldRange.maxValue)
        {
            _throwInputHoldTime += Time.deltaTime;
        }
    }

    
    private void CheckForInteractable()
    {
        var colliders = Physics.OverlapSphere(interactionPosition.position, interactionRadius, interactionLayer);
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IInteractable interactable))
            {
                if (!interactable.CanInteract()) continue;

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
            _closestInteractable?.HideInteractionTip();
            _closestInteractable?.ShowInteractionTip();
            _closestInteractable = closestInteractable;
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