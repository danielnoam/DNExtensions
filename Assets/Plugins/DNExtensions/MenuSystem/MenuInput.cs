

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DNExtensions.InputSystem;

namespace  DNExtensions.MenuSystem
{
    
    public class MenuInput : InputReaderBase
    {
        private InputActionMap _uiActionMap;
        private InputAction _navigateAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;
        private InputAction _pointAction;
        private InputAction _clickAction;
        private InputAction _scrollWheelAction;
        private InputAction _middleClickAction;
        private InputAction _rightClickAction;
        private InputAction _trackedDevicePositionAction;
        private InputAction _trackedDeviceOrientationAction;


        public event Action<InputAction.CallbackContext> OnNavigateAction;
        public event Action<InputAction.CallbackContext> OnSubmitAction;
        public event Action<InputAction.CallbackContext> OnCancelAction;
        public event Action<InputAction.CallbackContext> OnPointAction;
        public event Action<InputAction.CallbackContext> OnClickAction;
        public event Action<InputAction.CallbackContext> OnScrollWheelAction;
        public event Action<InputAction.CallbackContext> OnMiddleClickAction;
        public event Action<InputAction.CallbackContext> OnRightClickAction;
        public event Action<InputAction.CallbackContext> OnTrackedDevicePositionAction;
        public event Action<InputAction.CallbackContext> OnTrackedDeviceOrientationAction;

        private void Awake()
        {
            
            _uiActionMap = PlayerInput.actions.FindActionMap("UI");

            if (_uiActionMap == null)
            {
                Debug.LogError("UI Action Map not found. Please check the action maps in the Player Input component.");
                return;
            }

            _navigateAction = _uiActionMap.FindAction("Navigate");
            _submitAction = _uiActionMap.FindAction("Submit");
            _cancelAction = _uiActionMap.FindAction("Cancel");
            _pointAction = _uiActionMap.FindAction("Point");
            _clickAction = _uiActionMap.FindAction("Click");
            _scrollWheelAction = _uiActionMap.FindAction("ScrollWheel");
            _middleClickAction = _uiActionMap.FindAction("MiddleClick");
            _rightClickAction = _uiActionMap.FindAction("RightClick");
            _trackedDevicePositionAction = _uiActionMap.FindAction("TrackedDevicePosition");
            _trackedDeviceOrientationAction = _uiActionMap.FindAction("TrackedDeviceOrientation");
        }


        private void OnEnable()
        {
            SubscribeToAction(_navigateAction, OnNavigate);
            SubscribeToAction(_submitAction, OnSubmit);
            SubscribeToAction(_cancelAction, OnCancel);
            SubscribeToAction(_pointAction, OnPoint);
            SubscribeToAction(_clickAction, OnClick);
            SubscribeToAction(_scrollWheelAction, OnScrollWheel);
            SubscribeToAction(_middleClickAction, OnMiddleClick);
            SubscribeToAction(_rightClickAction, OnRightClick);
            SubscribeToAction(_trackedDevicePositionAction, OnTrackedDevicePosition);
            SubscribeToAction(_trackedDeviceOrientationAction, OnTrackedDeviceOrientation);

        }

        private void OnDisable()
        {
            UnsubscribeFromAction(_navigateAction, OnNavigate);
            UnsubscribeFromAction(_submitAction, OnSubmit);
            UnsubscribeFromAction(_cancelAction, OnCancel);
            UnsubscribeFromAction(_pointAction, OnPoint);
            UnsubscribeFromAction(_clickAction, OnClick);
            UnsubscribeFromAction(_scrollWheelAction, OnScrollWheel);
            UnsubscribeFromAction(_middleClickAction, OnMiddleClick);
            UnsubscribeFromAction(_rightClickAction, OnRightClick);
            UnsubscribeFromAction(_trackedDevicePositionAction, OnTrackedDevicePosition);
            UnsubscribeFromAction(_trackedDeviceOrientationAction, OnTrackedDeviceOrientation);

        }



        #region Input Events --------------------------------------------------------------------------------------

        private void OnNavigate(InputAction.CallbackContext context)
        {
            OnNavigateAction?.Invoke(context);
        }


        private void OnSubmit(InputAction.CallbackContext context)
        {
            OnSubmitAction?.Invoke(context);
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            OnCancelAction?.Invoke(context);
        }

        private void OnPoint(InputAction.CallbackContext context)
        {
            OnPointAction?.Invoke(context);
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            OnClickAction?.Invoke(context);
        }

        private void OnScrollWheel(InputAction.CallbackContext context)
        {
            OnScrollWheelAction?.Invoke(context);
        }

        private void OnMiddleClick(InputAction.CallbackContext context)
        {
            OnMiddleClickAction?.Invoke(context);
        }

        private void OnRightClick(InputAction.CallbackContext context)
        {
            OnRightClickAction?.Invoke(context);
        }

        private void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
            OnTrackedDevicePositionAction?.Invoke(context);
        }

        private void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            OnTrackedDeviceOrientationAction?.Invoke(context);
        }


        #endregion Input Events --------------------------------------------------------------------------------------


    }
}