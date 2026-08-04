using System;
using DNExtensions.Systems.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Systems.FirstPersonController
{
    /// <summary>
    /// Handles input actions for the first-person controller including movement, look, jump, crouch, run, and interactions.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class FPCInput : InputReaderBase
    {
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _crouchAction;
        private InputAction _runAction;
        private InputAction _interactAction;
        private InputAction _throwAction;
        private InputAction _toggleMenu;

        public event Action<InputAction.CallbackContext> OnMoveAction;
        public event Action<InputAction.CallbackContext> OnLookAction;
        public event Action<InputAction.CallbackContext> OnJumpAction;
        public event Action<InputAction.CallbackContext> OnRunAction;
        public event Action<InputAction.CallbackContext> OnCrouchAction;
        public event Action<InputAction.CallbackContext> OnInteractAction;
        public event Action<InputAction.CallbackContext> OnThrowAction;
        public event Action<InputAction.CallbackContext> OnToggleMenuAction;

        [SerializeField] private bool toggleRun;
        [SerializeField] private bool toggleCrouch = true;
        
        public Vector2 MoveInput { get; private set; }
        public bool RunInput { get; private set; }
        public bool ToggleCrouch => toggleCrouch;

        private void Awake()
        {
            _playerActionMap = PlayerInput.actions.FindActionMap("Player");

            if (_playerActionMap == null)
            {
                Debug.LogError("Player Action Map not found. Please check the action maps in the Player Input component.");
                return;
            }

            FindAction(_playerActionMap,"Move", ref _moveAction);
            FindAction(_playerActionMap, "Look", ref _lookAction);
            FindAction(_playerActionMap, "Run", ref _runAction);
            FindAction(_playerActionMap, "Jump", ref _jumpAction);
            FindAction(_playerActionMap, "Crouch", ref _crouchAction);
            FindAction(_playerActionMap, "Interact", ref _interactAction);
            FindAction(_playerActionMap, "Throw", ref _throwAction);
            FindAction(_playerActionMap, "ToggleMenu", ref _toggleMenu);
            
            _playerActionMap.Enable();
        }
        

        private void OnEnable()
        {
            SubscribeToAction(_moveAction, OnMove);
            SubscribeToAction(_lookAction, OnLook);
            SubscribeToAction(_jumpAction, OnJump);
            SubscribeToAction(_crouchAction, OnCrouch);
            SubscribeToAction(_runAction, OnRun);
            SubscribeToAction(_interactAction, OnInteract);
            SubscribeToAction(_throwAction, OnThrow);
            SubscribeToAction(_toggleMenu, OnToggleMenu);
        }

        private void OnDisable()
        {
            UnsubscribeFromAction(_moveAction, OnMove);
            UnsubscribeFromAction(_lookAction, OnLook);
            UnsubscribeFromAction(_jumpAction, OnJump);
            UnsubscribeFromAction(_crouchAction, OnCrouch);
            UnsubscribeFromAction(_runAction, OnRun);
            UnsubscribeFromAction(_interactAction, OnInteract);
            UnsubscribeFromAction(_throwAction, OnThrow);
            UnsubscribeFromAction(_toggleMenu, OnToggleMenu);
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            OnMoveAction?.Invoke(context);
        }
        
        private void OnLook(InputAction.CallbackContext context)
        {
            OnLookAction?.Invoke(context);
        }
        
        private void OnJump(InputAction.CallbackContext context)
        {
            OnJumpAction?.Invoke(context);
        }
        
        private void OnCrouch(InputAction.CallbackContext context)
        {
            if (toggleCrouch)
            {
                if (context.phase == InputActionPhase.Started)
                {
                    OnCrouchAction?.Invoke(context);
                }
            }
            else
            {
                OnCrouchAction?.Invoke(context);
            }
        }

        private void OnRun(InputAction.CallbackContext context)
        {
            RunInput = toggleRun ? (context.phase == InputActionPhase.Started ? !RunInput : RunInput) : context.ReadValueAsButton();
            OnRunAction?.Invoke(context);
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            OnInteractAction?.Invoke(context);
        }
        
        private void OnThrow(InputAction.CallbackContext context)
        {
            OnThrowAction?.Invoke(context);
        }
        

        private void OnToggleMenu(InputAction.CallbackContext context)
        {
            OnToggleMenuAction?.Invoke(context);
        }
    }
}