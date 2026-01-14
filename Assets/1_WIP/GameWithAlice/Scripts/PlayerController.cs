using System;
using DNExtensions;
using DNExtensions.SerializedInterface;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerControllerInput))]
[SelectionBase]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float gravity = 1f;
    [SerializeField] private float maxFallSpeed = 25f;
    [SerializeField] private float jumpForce = 15f;
    [Tooltip("Time window after pressing jump to still perform a jump when landing.")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [Tooltip("Time window after leaving ground to still perform a jump.")]
    [SerializeField] private float coyoteTime = 0.2f;
    
    [Header("Collision Settings")]
    [SerializeField] private float groundCheckRadius = 0.29f;
    [SerializeField] private Vector3 groundCheckOffset = Vector3.down;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Interaction Settings")]
    [SerializeField] private bool canInteractWhileAirborne = true;
    [SerializeField] private float interactCheckRange = 3f;
    [SerializeField] private Vector3 interactCheckOffset = Vector3.zero;
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerControllerInput input;


    [Separator]
    [SerializeField, ReadOnly] private Vector2 moveInput;
    [SerializeField, ReadOnly] private Vector3 velocity;
    [SerializeField, ReadOnly] private bool isGrounded;
    [SerializeField, ReadOnly] private bool jumpInput;
    [SerializeField, ReadOnly] private float jumpBufferTimer;
    [SerializeField, ReadOnly] private float coyoteTimer;
    [SerializeField, ReadOnly] private InterfaceReference<IInteractable> closetInteractable;
    [SerializeField, ReadOnly] private MovingPlatform currentPlatform;
    private Vector3 platformVelocity;
    

    private bool CanInteract => canInteractWhileAirborne || isGrounded;
    

    private void OnEnable()
    {
        input.OnMoveAction += OnMoveAction;
        input.OnJumpAction += OnJumpAction;
        input.OnInteractAction += OnInteractAction;
    }
    
    private void OnDisable()
    {
        input.OnMoveAction -= OnMoveAction;
        input.OnJumpAction -= OnJumpAction;
        input.OnInteractAction -= OnInteractAction;
    }
    
    private void OnInteractAction(InputAction.CallbackContext context)
    {
        if (CanInteract && context.performed && closetInteractable.Value != null)
        {
            closetInteractable.Value.Interact();
        }
    }

    private void OnJumpAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpInput = true;
            jumpBufferTimer = jumpBufferTime;
        }
        else if (context.canceled)
        {
            jumpInput = false;
        }
    }

    private void OnMoveAction(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else if (coyoteTimer > 0f)
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        CheckGround();
        CheckForInteractable();
        CheckForPlatform();
        HandleGravity();
        HandleJump();
        HandleMovement();   
    }

    private void HandleMovement()
    {
        if (!controller || !controller.enabled) return;
        
        velocity.x = moveInput.x * moveSpeed;
        velocity.z = moveInput.y * moveSpeed;
    
        Vector3 finalVelocity = velocity + platformVelocity;
        controller.Move(finalVelocity * Time.fixedDeltaTime);
    }

    private void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y -= gravity;
            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
            }
        }
    }

    private void HandleJump()
    {
        if ((jumpInput || jumpBufferTimer > 0) && (isGrounded || coyoteTimer > 0))
        {
            velocity.y = jumpForce;
            jumpInput = false;
            jumpBufferTimer = 0;
            coyoteTimer = 0;
        }
    }
    
    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(transform.position + groundCheckOffset, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void CheckForPlatform()
    {
        if (!isGrounded)
        {
            currentPlatform = null;
            platformVelocity = Vector3.zero;
            return;
        }

        var colliders = Physics.OverlapSphere(transform.position + groundCheckOffset, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out MovingPlatform platform))
            {
                currentPlatform = platform;
                platformVelocity = platform.Velocity;
                return;
            }
        }

        currentPlatform = null;
        platformVelocity = Vector3.zero;
    }

    private void CheckForInteractable()
    {
        var colliders = Physics.OverlapSphere(transform.position + interactCheckOffset, interactCheckRange, interactableLayer);
        var closestDistance = float.MaxValue;
        IInteractable closest = null;

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IInteractable interactable) && interactable.CanInteract())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }
        }
        
        closetInteractable.Value = closest;
    }
    
    public void ForceJump(float force)
    {
        if (!controller || !controller.enabled) return;
        
        velocity.y = force;
        jumpInput = false;
    }

    
    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + interactCheckOffset, interactCheckRange);
    }
}