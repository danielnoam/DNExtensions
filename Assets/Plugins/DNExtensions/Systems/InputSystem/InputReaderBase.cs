using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.Systems.InputSystem
{
    /// <summary>
    /// Base class for handling Unity Input System interactions with cursor management capabilities.
    /// Provides foundation for input handling classes with built-in cursor visibility controls.
    /// </summary>
    public class InputReaderBase : MonoBehaviour
    {
        [SerializeField] protected InputManager inputManager;

        public bool IsGamepad => InputManager.IsGamepad;
        public bool IsKeyboardMouse => InputManager.IsKeyboardMouse;
        public bool IsTouch => InputManager.IsTouch;
        public bool IsMobile => InputManager.IsMobile;
        public InputDeviceType CurrentDevice => InputManager.CurrentDevice;
        protected PlayerInput PlayerInput => inputManager?.PlayerInput ?? InputManager.Instance?.PlayerInput;

        protected virtual void OnValidate()
        {
            if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
        }
        
        /// <summary>
        /// Finds a specific InputAction within an InputActionMap and assigns it to a reference.
        /// </summary>
        /// <param name="actionMap"></param> The InputActionMap to search within.
        /// <param name="actionName"></param> The name of the InputAction to find.
        /// <param name="actionReference"></param> The reference to assign the found InputAction to.
        protected void FindAction(InputActionMap actionMap, string actionName, ref InputAction actionReference)
        {
            if (actionMap == null)
            {
                Debug.LogError("Action Map not found.");
                return;
            }
            
            actionReference = actionMap.FindAction(actionName);
            if (actionReference == null) Debug.LogError($"{actionName} action not found in Player Action Map.");
        }

        /// <summary>
        /// Subscribes a callback method to all phases of an InputAction (started, performed, canceled).
        /// </summary>
        /// <param name="action">The InputAction to subscribe to. If null, no subscription occurs.</param>
        /// <param name="callback">The callback method to invoke for all action phases.</param>
        protected void SubscribeToAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                Debug.LogError("No action was found!");
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
        protected void UnsubscribeFromAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null)
            {
                Debug.LogError("No action was found!");
                return;
            }

            action.performed -= callback;
            action.started -= callback;
            action.canceled -= callback;
        }
    }
}