using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace DNExtensions.InputSystem
{
    [RequireComponent(typeof(ActionBindingDisplay))]
    public class ActionPromptVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color pressedColor = Color.gray;
        [SerializeField] private Vector3 pressedScale = Vector3.one * 0.9f;
        [Tooltip("Override which action to watch for pressed state. Leave empty to use all actions from binding display.")]
        [SerializeField] private string actionOverride;

        private ActionBindingDisplay _bindingDisplay;
        private TextMeshProUGUI _textComponent;
        private InputAction[] _watchedActions;
        private Color _originalColor;
        private Vector3 _originalScale;

        private void Awake()
        {
            _bindingDisplay = GetComponent<ActionBindingDisplay>();
            _textComponent = GetComponent<TextMeshProUGUI>();
            _originalColor = _textComponent.color;
            _originalScale = _textComponent.transform.localScale;
            _watchedActions = GetWatchedActions();
        }
        

        private void OnEnable()
        {
            if (_watchedActions == null) return;
            foreach (var action in _watchedActions)
            {
                if (action != null)
                {
                    action.started += OnActionStarted;
                    action.canceled += OnActionCanceled;
                }
            }
        }

        private void OnDisable()
        {
            if (_watchedActions == null) return;
            foreach (var action in _watchedActions)
            {
                if (action != null)
                {
                    action.started -= OnActionStarted;
                    action.canceled -= OnActionCanceled;
                }
            }
        }

        private InputAction[] GetWatchedActions()
        {
            var playerInput = InputManager.Instance?.PlayerInput;
            if (!playerInput)
            {
                return Array.Empty<InputAction>();
            }

            if (!string.IsNullOrEmpty(actionOverride))
            {
                var action = playerInput.actions.FindAction(actionOverride);
                if (action != null)
                {
                    return new [] { action };
                }
                
                Debug.LogWarning($"Override action '{actionOverride}' not found!", this);
                return Array.Empty<InputAction>();
            }
            
            return _bindingDisplay.GetAllActions();
        }

        private void OnActionStarted(InputAction.CallbackContext context)
        {
            ApplyPressedState();
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            ApplyNormalState();
        }

        private void ApplyPressedState()
        {
            if (_textComponent)
            {
                _textComponent.color = pressedColor;
                _textComponent.transform.localScale = pressedScale;
            }
        }

        private void ApplyNormalState()
        {
            if (_textComponent)
            {
                _textComponent.color = _originalColor;
                _textComponent.transform.localScale = _originalScale;
            }
        }
    }
}