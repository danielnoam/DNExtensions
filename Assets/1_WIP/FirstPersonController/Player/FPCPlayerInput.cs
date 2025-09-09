using System;
using DNExtensions;
using DNExtensions.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FPCPlayerInput : InputReaderBase
{
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _interactAction;
        private InputAction _throwAction;
        private InputAction _dropAction;
        private InputAction _toggleMenu;
        
        
        public event Action<InputAction.CallbackContext> OnMoveAction;
        public event Action<InputAction.CallbackContext> OnLookAction;
        public event Action<InputAction.CallbackContext> OnJumpAction;
        public event Action<InputAction.CallbackContext> OnRunAction;
        public event Action<InputAction.CallbackContext> OnInteractAction;
        public event Action<InputAction.CallbackContext> OnThrowAction;
        public event Action<InputAction.CallbackContext> OnDropAction;
        public event Action<InputAction.CallbackContext> OnToggleMenuAction;


        protected override void Awake()
        {
            base.Awake();


            _playerActionMap = playerInput.actions.FindActionMap("Player");

            if (_playerActionMap == null)
            {
                Debug.LogError("Player Action Map not found. Please check the action maps in the Player Input component.");
                return;
            }

            _moveAction = _playerActionMap.FindAction("Move");
            _lookAction = _playerActionMap.FindAction("Look");
            _jumpAction = _playerActionMap.FindAction("Jump");
            _runAction = _playerActionMap.FindAction("Run");
            _interactAction = _playerActionMap.FindAction("Interact");
            _throwAction = _playerActionMap.FindAction("Throw");
            _dropAction = _playerActionMap.FindAction("Drop");
            _toggleMenu = _playerActionMap.FindAction("ToggleMenu");
            
            if (_moveAction == null) Debug.LogError("Move action not found in Player Action Map.");
            if (_lookAction == null) Debug.LogError("Look action not found in Player Action Map.");
            if (_jumpAction == null) Debug.LogError("Jump action not found in Player Action Map.");
            if (_runAction == null) Debug.LogError("Run action not found in Player Action Map.");
            if (_interactAction == null) Debug.LogError("Interact action not found in Player Action Map.");
            if (_throwAction == null) Debug.LogError("Throw action not found in Player Action Map.");
            if (_dropAction == null) Debug.LogError("Drop action not found in Player Action Map.");
            if (_toggleMenu == null) Debug.LogError("ToggleMenu action not found in Player Action Map.");
            
            _playerActionMap.Enable();

        }


        private void OnEnable()
        {
            SubscribeToAction(_moveAction, OnMove);
            SubscribeToAction(_lookAction, OnLook);
            SubscribeToAction(_jumpAction, OnJump);
            SubscribeToAction(_runAction, OnRun);
            SubscribeToAction(_interactAction, OnInteract);
            SubscribeToAction(_throwAction, OnThrow);
            SubscribeToAction(_dropAction, OnDrop);
            SubscribeToAction(_toggleMenu, OnToggleMenu);


        }

        private void OnDisable()
        {
            UnsubscribeFromAction(_moveAction, OnMove);
            UnsubscribeFromAction(_lookAction, OnLook);
            UnsubscribeFromAction(_jumpAction, OnJump);
            UnsubscribeFromAction(_runAction, OnRun);
            UnsubscribeFromAction(_interactAction, OnInteract);
            UnsubscribeFromAction(_throwAction, OnThrow);
            UnsubscribeFromAction(_dropAction, OnDrop);
            UnsubscribeFromAction(_toggleMenu, OnToggleMenu);
        }

        

        private void OnMove(InputAction.CallbackContext context)
        {
            Debug.Log("Move action received");
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
        
        private void OnRun(InputAction.CallbackContext context)
        {
           

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
        
        private void OnDrop(InputAction.CallbackContext context)
        {
            OnDropAction?.Invoke(context);
        }
        
        
        private void OnToggleMenu(InputAction.CallbackContext context)
        {
            OnToggleMenuAction?.Invoke(context);
        }
        

   
}