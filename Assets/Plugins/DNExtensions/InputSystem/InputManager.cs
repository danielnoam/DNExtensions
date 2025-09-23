using System;
using DNExtensions.Button;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private PlayerInput playerInput;

        // Sprite assets has to be in /Resources/Sprite Assets/ folder
        [Header("Controls Sprite Assets")] [SerializeField]
        private TMP_SpriteAsset keyboardMouseSpriteAsset;

        [SerializeField] private TMP_SpriteAsset gamepadSpriteAsset;

        [Header("Cursor Settings")] [SerializeField]
        private bool hideCursor = true;


        private bool _isCurrentDeviceGamepad;

        public bool IsCurrentDeviceGamepad => _isCurrentDeviceGamepad;
        public PlayerInput PlayerInput => playerInput;

        public event Action<PlayerInput> OnDeviceRegainedEvent;
        public event Action<PlayerInput> OnDeviceLostEvent;
        public event Action<PlayerInput> OnControlsChangedEvent;


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
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (!playerInput) return;

            SetCursorVisibility(!hideCursor);
        }

        private void OnEnable()
        {
            if (!playerInput) return;

            playerInput.onDeviceRegained += OnDeviceRegained;
            playerInput.onDeviceLost += OnDeviceLost;
            playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDisable()
        {
            if (!playerInput) return;

            playerInput.onDeviceRegained -= OnDeviceRegained;
            playerInput.onDeviceLost -= OnDeviceLost;
            playerInput.onControlsChanged -= OnControlsChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDeviceRegained(PlayerInput input)
        {
            SetActiveControlScheme(input);
            OnDeviceRegainedEvent?.Invoke(input);
        }

        private void OnDeviceLost(PlayerInput input)
        {
            SetActiveControlScheme(input);
            OnDeviceLostEvent?.Invoke(input);
        }

        private void OnControlsChanged(PlayerInput input)
        {
            SetActiveControlScheme(input);
            OnControlsChangedEvent?.Invoke(input);
        }

        private void SetActiveControlScheme(PlayerInput input)
        {
            _isCurrentDeviceGamepad = input.currentControlScheme == "Gamepad";
        }


        /// <summary>
        /// Sets the cursor visibility and lock state.
        /// </summary>
        /// <param name="isVisible">True to show the cursor, false to hide it.</param>
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

        public static string ReplaceActionBindingsWithSprites(string text)
        {
            if (!Instance) return text;

            TMP_SpriteAsset spriteAsset = Instance._isCurrentDeviceGamepad
                ? Instance.gamepadSpriteAsset
                : Instance.keyboardMouseSpriteAsset;

            return InputManagerBindingFormatter.ReplaceActionBindings(text, true, Instance.playerInput, spriteAsset);
        }

        public static string ReplaceActionBindingsWithText(string text)
        {
            return InputManagerBindingFormatter.ReplaceActionBindings(text, false, Instance.playerInput);
        }
    }
}