using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    [Serializable]
    public struct TokenActionPair
    {
        [Tooltip("The token to find in the text (e.g., '{jump}' or 'JUMP')")]
        public string token;
        
        [Tooltip("The action to display when this token is found")]
        public string actionName;
    }

    public class TokenBindingDisplay : InputBindingDisplay
    {
        [SerializeField] private TokenActionPair[] tokenBindings = Array.Empty<TokenActionPair>();

        private string templateText;
        
        private void Awake()
        {
            
            if (textComponent)
            {
                templateText = textComponent.text;
            }
        }
        

        protected override string GetDisplayText()
        {
            if (string.IsNullOrEmpty(templateText))
            {
                return null;
            }

            var playerInput = InputManager.Instance?.PlayerInput;
            if (!playerInput)
            {
                Debug.LogWarning("PlayerInput not found!", this);
                return null;
            }
            
            string result = templateText;
            
            foreach (var binding in tokenBindings)
            {
                if (string.IsNullOrEmpty(binding.token) || string.IsNullOrEmpty(binding.actionName))
                {
                    continue;
                }

                var action = playerInput.actions.FindAction(binding.actionName);
                if (action == null)
                {
                    Debug.LogWarning($"Action '{binding.actionName}' not found!", this);
                    continue;
                }

                string replacement = InputManager.GetActionBinding(action, useSprites);
                if (!string.IsNullOrEmpty(replacement))
                {
                    result = result.Replace(binding.token, replacement);
                }
            }

            return result;
        }
    }
}