using System;
using DNExtensions.Utilities.Button;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        
        
        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private bool hideCursor = true;
        
        [Header("Controls Sprite Assets")]
        [Tooltip("Sprite assets has to be in /Resources/Sprite Assets/")]
        [SerializeField] private TMP_SpriteAsset keyboardMouseSpriteAsset;
        [Tooltip("Sprite assets has to be in /Resources/Sprite Assets/")]
        [SerializeField] private TMP_SpriteAsset gamepadSpriteAsset;


        public static InputDeviceType CurrentDevice { get; private set; }
        public static bool IsMobile { get; private set; }
        
        public static bool IsGamepad => CurrentDevice == InputDeviceType.Gamepad;
        public static bool IsKeyboardMouse => CurrentDevice == InputDeviceType.KeyboardMouse;
        public static bool IsTouch => CurrentDevice == InputDeviceType.Touch;
        public static event Action<PlayerInput> OnDeviceRegained;
        public static event Action<PlayerInput> OnDeviceLost;
        public static event Action<PlayerInput> OnControlsChanged;

        
        public PlayerInput PlayerInput => playerInput;




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

        private void Awake()
        {
            if ((!Instance || Instance == this) && playerInput)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            IsMobile = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
            CurrentDevice = IsMobile ? InputDeviceType.Touch : InputDeviceType.KeyboardMouse;

            SetCursorVisibility(!hideCursor);
        }

        private void OnEnable()
        {
            if (!playerInput) return;

            playerInput.onDeviceRegained += OnDeviceRegainedHandler;
            playerInput.onDeviceLost += OnDeviceLostHandler;
            playerInput.onControlsChanged += OnControlsChangedHandler;
        }

        private void OnDisable()
        {
            if (!playerInput) return;

            playerInput.onDeviceRegained -= OnDeviceRegainedHandler;
            playerInput.onDeviceLost -= OnDeviceLostHandler;
            playerInput.onControlsChanged -= OnControlsChangedHandler;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDeviceRegainedHandler(PlayerInput input)
        {
            UpdateActiveDevice(input);
            OnDeviceRegained?.Invoke(input);
        }

        private void OnDeviceLostHandler(PlayerInput input)
        {
            UpdateActiveDevice(input);
            OnDeviceLost?.Invoke(input);
        }

        private void OnControlsChangedHandler(PlayerInput input)
        {
            UpdateActiveDevice(input);
            OnControlsChanged?.Invoke(input);
        }

        private void UpdateActiveDevice(PlayerInput input)
        {
            if (IsMobile)
            {
                if (input.currentControlScheme == "Gamepad")
                {
                    CurrentDevice = InputDeviceType.Gamepad;
                }
                else
                {
                    CurrentDevice = InputDeviceType.Touch;
                }
            }
            else
            {
                if (input.currentControlScheme == "Gamepad")
                {
                    CurrentDevice = InputDeviceType.Gamepad;
                }
                else
                {
                    CurrentDevice = InputDeviceType.KeyboardMouse;
                }
            }
        }

        /// <summary>
        /// Sets the cursor visibility and lock state.
        /// </summary>
        /// <param name="state">True to show the cursor, false to hide it.</param>
        public void SetCursorVisibility(bool state)
        {
            if (state)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// Toggles cursor visibility between visible and hidden states.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void ToggleCursorVisibility()
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

        /// <summary>
        /// Get binding for a specific InputAction
        /// </summary>
        public static string GetActionBinding(InputAction action, bool asSprite = true, string compositePartFilter = "")
        {
            if (!Instance?.playerInput || action == null) return action?.name ?? "Unknown";

            TMP_SpriteAsset spriteAsset = asSprite ? IsGamepad
                ? Instance.gamepadSpriteAsset
                : Instance.keyboardMouseSpriteAsset : null;

            return InputBindingFormatter.GetActionBinding(action, asSprite, Instance.playerInput, spriteAsset, compositePartFilter);
        }

        /// <summary>
        /// Get bindings for multiple InputActions
        /// </summary>
        public static string GetActionBindings(InputAction[] actions, string separator = " | ", bool asSprites = true, string compositePartFilter = "")
        {
            if (actions == null || actions.Length == 0) return "";

            string[] bindings = new string[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                bindings[i] = GetActionBinding(actions[i], asSprites, compositePartFilter);
            }

            return string.Join(separator, bindings);
        }
    }
}