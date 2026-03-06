using DNExtensions.Systems.FirstPersonController.Interactable;
using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Handles player interactions with interactable objects, including picking up, holding, throwing, and dropping objects.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FpcManager))]
    [AddComponentMenu("")]
    public class FPCInteraction : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 3f;
        [SerializeField] private LayerMask interactionLayer = 0;
        
        [Header("Held Object")]
        [SerializeField] private float minTimeForThrow = 0.1f;
        [SerializeField] private float autoDropYOffset = 1f;
        [SerializeField, MinMaxRange(0,30)] private RangedFloat throwForceRange = new RangedFloat(5f, 15f);
        [SerializeField, MinMaxRange(1f,4f)] private RangedFloat throwHeldRange = new RangedFloat(1f, 4f);
        
        [Header("References")]
        [SerializeField, AutoGetSelf, HideInInspector] private FpcManager manager;
        [SerializeField] private Transform holdPosition;
        [SerializeField] private Transform interactionPosition;

        private PickableObject _heldObject;
        private IInteractable _closestInteractable;
        private bool _throwInputHeld;
        private float _throwInputHoldTime;
        
        public Transform HoldPosition => holdPosition;

        /// <summary>
        /// Gets or sets the currently held pickable object. Setting to null or a different object will drop the current object.
        /// </summary>
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
            AutoGetSystem.Process(this);
            if (!interactionPosition) interactionPosition = transform;
        }

        private void OnEnable()
        {
            manager.FpcInput.OnInteractAction += OnInteract;
            manager.FpcInput.OnThrowAction += OnThrow;
        }

        private void OnDisable()
        {
            manager.FpcInput.OnInteractAction -= OnInteract;
            manager.FpcInput.OnThrowAction -= OnThrow;
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
        
        private void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                var interactorData = new InteractorData()
                {
                  FpcInteraction = this
                };
                _closestInteractable?.Interact(interactorData);
            }
        }
        
        private void OnThrow(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _throwInputHeld = true;
                _throwInputHoldTime = 0f;
            }
            else if (context.canceled)
            {
                _throwInputHeld = false;
                
                if (_throwInputHoldTime < minTimeForThrow)
                {
                    DropHeldObject();
                }
                else
                {
                    ThrowHeldObject();
                }
                
                _throwInputHoldTime = 0f;
            }
        }



        private void ThrowHeldObject()
        {
            if (!_heldObject) return;
            
            var force = throwForceRange.Lerp(_throwInputHoldTime / throwHeldRange.maxValue);
            _heldObject.Throw(manager.FpcCamera.GetAimDirection(), force);
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
                _closestInteractable = closestInteractable;
                _closestInteractable?.ShowInteractionTip();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
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
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position.RemoveY(autoDropYOffset), 0.1f);
            Handles.Label(transform.position.RemoveY(autoDropYOffset), "Auto drop distance");
        }
#endif
    }
}