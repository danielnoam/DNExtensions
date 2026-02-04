using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    /// <summary>
    /// Maps custom tokens in text to input actions and displays their bindings.
    /// Captures the original text as a template on Start and replaces specified tokens on each update.
    /// </summary>
    [Serializable]
    public struct TokenActionPair
    {
        [Tooltip("The token to find in the text (e.g., '{jump}' or 'JUMP')")]
        public string token;
        
        [Tooltip("The action to display when this token is found")]
        public InputActionReference action;
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
            
            string result = templateText;
            
            foreach (var binding in tokenBindings)
            {
                if (string.IsNullOrEmpty(binding.token) || binding.action?.action == null)
                {
                    continue;
                }
                string replacement = InputManager.GetActionBinding(
                    binding.action.action, 
                    useSprites
                );
                result = result.Replace(binding.token, replacement);
            }

            return result;
        }
    }
}