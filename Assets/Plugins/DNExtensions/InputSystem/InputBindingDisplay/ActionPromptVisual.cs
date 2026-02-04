using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace DNExtensions.InputSystem
{
    /// <summary>
    /// Visual styling component for action prompts.
    /// Handles color and scale changes based on input action pressed state.
    /// Requires ActionBindingDisplay on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(ActionBindingDisplay))]
    public class ActionPromptVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color pressedColor = Color.gray;
        [SerializeField] private Vector3 pressedScale = Vector3.one * 0.9f;
        [Tooltip("Override which action to watch for pressed state. Leave empty to use all actions from binding display.")]
        [SerializeField] private InputActionReference actionOverride;

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
            if (actionOverride?.action != null)
            {
                return new [] { actionOverride.action };
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