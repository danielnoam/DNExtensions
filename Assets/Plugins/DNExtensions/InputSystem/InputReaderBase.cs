using System;
using DNExtensions.Button;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    /// <summary>
    /// Base class for handling Unity Input System interactions with cursor management capabilities.
    /// Provides foundation for input handling classes with built-in cursor visibility controls.
    /// </summary>
    public class InputReaderBase : MonoBehaviour
    {
        [SerializeField] protected PlayerInput playerInput;
        [SerializeField] private bool autoHideCursorOnAwake = true;

        
        private void OnValidate()
        {
            if (!playerInput) 
            {
                Debug.Log("No Player Input was set!");
            }

            if (playerInput && playerInput.notificationBehavior != PlayerNotifications.InvokeCSharpEvents)
            {
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                Debug.Log("Set Player Input notification to c# events");
            }
        }


        protected virtual void Awake()
        {
            if (autoHideCursorOnAwake) SetCursorVisibility(false);
        }

        /// <summary>
        /// Subscribes a callback method to all phases of an InputAction (started, performed, canceled).
        /// </summary>
        /// <param name="action">The InputAction to subscribe to. If null, no subscription occurs.</param>
        /// <param name="callback">The callback method to invoke for all action phases.</param>
        protected void SubscribeToAction(InputAction action, Action<InputAction.CallbackContext> callback)
        {
            if (action == null) return;

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
            if (action == null) return;

            action.performed -= callback;
            action.started -= callback;
            action.canceled -= callback;
        }

        /// <summary>
        /// Sets the cursor visibility and lock state.
        /// </summary>
        /// <param name="isVisible">True to show the cursor, false to hide it.</param>
        protected void SetCursorVisibility(bool isVisible)
        {
            if (!isVisible)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// Toggles cursor visibility between visible and hidden states.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        protected void ToggleCursorVisibility()
        {
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}