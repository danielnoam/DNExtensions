using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    /// <summary>
    /// Displays input bindings for an array of InputActions.
    /// Always generates display from the actions array, ignoring any existing text.
    /// </summary>
    public class ActionBindingDisplay : InputBindingDisplay
    {
        [SerializeField] private InputActionReference[] actions = Array.Empty<InputActionReference>();
        [SerializeField] private string separator = " | ";

        protected override string GetDisplayText()
        {
            if (actions == null || actions.Length == 0)
            {
                return null;
            }
            
            List<InputAction> validActions = new List<InputAction>();

            foreach (var actionRef in actions)
            {
                if (actionRef?.action != null)
                {
                    validActions.Add(actionRef.action);
                }
            }
            
            if (validActions.Count == 0)
            {
                Debug.LogWarning("No valid actions found!", this);
                return null;
            }
            
            return InputManager.GetActionBindings(validActions.ToArray(), separator, useSprites);
        }
        
        
        /// <summary>
        /// Gets all actions for ActionPromptVisual.
        /// </summary>
        public InputAction[] GetAllActions()
        {
            if (actions == null || actions.Length == 0)
            {
                return Array.Empty<InputAction>();
            }

            List<InputAction> validActions = new List<InputAction>();
            foreach (var actionRef in actions)
            {
                if (actionRef?.action != null)
                {
                    validActions.Add(actionRef.action);
                }
            }

            return validActions.ToArray();
        }
    }
}