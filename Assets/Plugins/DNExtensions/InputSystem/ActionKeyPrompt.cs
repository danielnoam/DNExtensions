
using System;
using DNExtensions.Button;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    
    public class ActionKeyPrompt : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private bool useSprites = true;
        [SerializeField] private string separator = " | ";
        [SerializeField] private InputActionReference[] inputActionReferences = Array.Empty<InputActionReference>();

        [Header("Pressed Effects")] [SerializeField]
        private Color pressedColor = Color.gray;

        [SerializeField] private Vector3 pressedScale = Vector3.one * 0.9f;

        [Header("Reference")] [SerializeField] private TextMeshProUGUI prompt;
        [SerializeField] private InputManager inputManager;

        private Color _originalColor;
        private Vector3 _originalScale;
        private bool _isPressed;

        private void Awake()
        {
            if (!inputManager) inputManager = FindFirstObjectByType<InputManager>();
        }

        private void Start()
        {
            if (prompt)
            {
                _originalColor = prompt.color;
                _originalScale = prompt.transform.localScale;
            }

            UpdateDisplay();
        }

        private void OnEnable()
        {
            if (inputManager)
            {
                inputManager.OnControlsChangedEvent += OnInputChanged;
            }


            foreach (var actionReference in inputActionReferences)
            {
                if (actionReference?.action != null)
                {
                    actionReference.action.started += OnActionStarted;
                    actionReference.action.canceled += OnActionCanceled;
                }
            }
        }

        private void OnDisable()
        {
            if (inputManager)
            {
                inputManager.OnControlsChangedEvent -= OnInputChanged;
            }


            foreach (var actionReference in inputActionReferences)
            {
                if (actionReference?.action != null)
                {
                    actionReference.action.started -= OnActionStarted;
                    actionReference.action.canceled -= OnActionCanceled;
                }
            }
        }

        private void OnInputChanged(PlayerInput input) => UpdateDisplay();

        private void OnActionStarted(InputAction.CallbackContext context)
        {

            SetFontColor(pressedColor);
            SetScale(pressedScale);
            _isPressed = true;
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {

            ResetFontColor();
            ResetScale();
            _isPressed = false;
        }




        [Button("Update Display")]
        public void UpdateDisplay()
        {
            if (!prompt || inputActionReferences == null || inputActionReferences.Length == 0) return;

            InputAction[] actions = new InputAction[inputActionReferences.Length];
            for (int i = 0; i < inputActionReferences.Length; i++)
            {
                actions[i] = inputActionReferences[i]?.action;
            }

            prompt.text = InputManager.GetActionBindings(actions, separator, useSprites);

            if (_isPressed)
            {
                SetFontColor(pressedColor);
                SetScale(pressedScale);
            }
            else
            {
                ResetFontColor();
                ResetScale();
            }
        }

        private void SetFontColor(Color color)
        {
            if (prompt)
            {
                prompt.color = color;
            }
        }

        private void ResetFontColor()
        {
            if (prompt)
            {
                prompt.color = _originalColor;
            }
        }

        private void SetScale(Vector3 scale)
        {
            if (prompt)
            {
                prompt.transform.localScale = scale;
            }
        }

        private void ResetScale()
        {
            if (prompt)
            {
                prompt.transform.localScale = _originalScale;
            }
        }

    }
}