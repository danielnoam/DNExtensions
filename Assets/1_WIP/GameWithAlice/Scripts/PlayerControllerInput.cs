using System;
using UnityEngine;
using UnityEngine.InputSystem;

    public class PlayerControllerInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput playerInput;


        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        private InputAction _useAction;
        
        
        public event Action<InputAction.CallbackContext> OnMoveAction;
        public event Action<InputAction.CallbackContext> OnJumpAction;
        public event Action<InputAction.CallbackContext> OnInteractAction;
        public event Action<InputAction.CallbackContext> OnUseAction;
        


        private void Awake()
        {

            _playerActionMap = playerInput.actions.FindActionMap("Player");

            if (_playerActionMap == null)
            {
                Debug.LogError("Player Action Map not found. Please check the action maps in the Player Input component.");
                return;
            }

            _moveAction = _playerActionMap.FindAction("Move");
            _jumpAction = _playerActionMap.FindAction("Jump");
            _interactAction = _playerActionMap.FindAction("Interact");
            _useAction = _playerActionMap.FindAction("Use");
            
            
            if (_moveAction == null) Debug.LogError("Move action not found in Player Action Map.");
            if (_jumpAction == null) Debug.LogError("Jump action not found in Player Action Map.");
            if (_interactAction == null) Debug.LogError("Interact action not found in Player Action Map.");
            if (_useAction == null) Debug.LogError("Use action not found in Player Action Map.");
            
            _playerActionMap.Enable();
        }


        private void OnEnable()
        {
            SubscribeToAction(_moveAction, OnMove);
            SubscribeToAction(_jumpAction, OnJump);
            SubscribeToAction(_interactAction, OnInteract);
            SubscribeToAction(_useAction, OnUse);
        }
        

        private void OnDisable()
        {
            UnsubscribeFromAction(_moveAction, OnMove);
            UnsubscribeFromAction(_jumpAction, OnJump);
            UnsubscribeFromAction(_interactAction, OnInteract);
            UnsubscribeFromAction(_useAction, OnUse);
        }

        
        private void OnMove(InputAction.CallbackContext context)
        {
            OnMoveAction?.Invoke(context);
        }
        
        private void OnInteract(InputAction.CallbackContext context)
        {
            OnInteractAction?.Invoke(context);
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            OnJumpAction?.Invoke(context);
        }
        
        private void OnUse(InputAction.CallbackContext context)
        {
            OnUseAction?.Invoke(context);
        }
        
        
        /// <summary>
        /// Subscribes a callback method to all phases of an InputAction (started, performed, canceled).
        /// </summary>
        /// <param name="action">The InputAction to subscribe to. If null, no subscription occurs.</param>
        /// <param name="callback">The callback method to invoke for all action phases.</param>
        private void SubscribeToAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                Debug.LogError($"No action was found!");
                return;
            }

            action.performed += callback;
            action.started += callback;
            action.canceled += callback;
        }

        /// <summary>
        /// Unsubscribes a callback method from all phases of an InputAction (started, performed, canceled).
        /// </summary>
        /// <param name="action">The InputAction to unsubscribe from. If null, no unsubscription occurs.</param>
        /// <param name="callback">The callback method to remove from all action phases.</param>
        private void UnsubscribeFromAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                Debug.LogError($"No action was found!");
                return;
            }

            action.performed -= callback;
            action.started -= callback;
            action.canceled -= callback;
        }
        
    }
