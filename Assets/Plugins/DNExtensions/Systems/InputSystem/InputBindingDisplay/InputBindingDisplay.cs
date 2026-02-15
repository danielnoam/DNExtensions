using DNExtensions.Utilities.Button;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    public abstract class InputBindingDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected TextMeshProUGUI textComponent;

        [Header("Settings")]
        [SerializeField] protected bool useSprites;

        protected virtual void OnValidate()
        {
            if (!textComponent)
            {
                textComponent = GetComponent<TextMeshProUGUI>();
            }
        }
        

        protected virtual void Start()
        {
            UpdateDisplay();
        }

        protected virtual void OnEnable()
        {
            InputManager.OnControlsChanged += OnDeviceChanged;
        }

        protected virtual void OnDisable()
        {
            InputManager.OnControlsChanged  -= OnDeviceChanged;
        }

        private void OnDeviceChanged(PlayerInput input)
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Updates the text component with the current binding display.
        /// </summary>
        [Button("Update Display", ButtonPlayMode.OnlyWhenPlaying)]
        public void UpdateDisplay()
        {
            if (!textComponent)
            {
                Debug.LogError("TextMeshProUGUI component not assigned!", this);
                return;
            }
            

            string result = GetDisplayText();

            if (string.IsNullOrEmpty(result))
            {
                result = "[No Binding]";
            }

            textComponent.text = result;
        }

        /// <summary>
        /// Subclasses override this to generate their specific display text.
        /// </summary>
        /// <returns>The formatted binding text, or null if bindings cannot be determined.</returns>
        protected abstract string GetDisplayText();
    }
}