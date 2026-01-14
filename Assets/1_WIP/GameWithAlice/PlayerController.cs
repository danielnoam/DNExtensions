
using DNExtensions;
using DNExtensions.SerializedInterface;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerControllerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxFallSpeed = 50f;
    
    [Header("Collision Settings")]
    [SerializeField] private float groundCheckRadius = 0.2f;
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
    [SerializeField, ReadOnly] private InterfaceReference<IInteractable> closetInteractable;
    
    
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
        if (CanInteract && context.performed && closetInteractable.IsValid)
        {
            closetInteractable.Value.Interact();
        }
    }

    private void OnJumpAction(InputAction.CallbackContext context)
    {
        if (context.performed) jumpInput = true;
        else if (context.canceled) jumpInput = false;
    }

    private void OnMoveAction(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    

    private void FixedUpdate()
    {
        CheckGround();
        CheckForInteractable();
        HandleGravity();
        HandleJump();
        HandleMovement();   
    }

    private void HandleMovement()
    {
        if ( !controller || !controller.enabled) return;
        
        velocity.x = moveInput.x * moveSpeed;
        velocity.z = moveInput.y * moveSpeed;
    
        controller.Move(velocity * Time.fixedDeltaTime);
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
        if (jumpInput && isGrounded)
        {
            velocity.y = jumpForce;
            jumpInput = false;
        }
    }
    
    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(transform.position + groundCheckOffset, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void CheckForInteractable()
    {
        var colliders = Physics.OverlapSphere(transform.position + interactCheckOffset, interactCheckRange, interactableLayer);
        float closestDistance = float.MaxValue;
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
        if ( !controller || !controller.enabled) return;
        
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
