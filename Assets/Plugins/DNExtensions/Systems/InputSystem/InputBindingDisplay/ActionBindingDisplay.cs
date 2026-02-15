using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DNExtensions.InputSystem
{
    public class ActionBindingDisplay : InputBindingDisplay
    {
        [SerializeField] private string[] actionNames;
        [SerializeField] private string separator = " | ";
        [Tooltip("Filter to show only specific composite parts (e.g., 'positive', 'negative', 'up', 'down', 'left', 'right'). Leave empty to show all bindings.")]
        [SerializeField] private string compositePartFilter = "";

        protected override string GetDisplayText()
        {
            if (actionNames == null || actionNames.Length == 0)
            {
                return null;
            }

            var playerInput = InputManager.Instance?.PlayerInput;
            if (playerInput == null)
            {
                Debug.LogWarning("[ActionBindingDisplay] PlayerInput not found!", this);
                return null;
            }
            
            List<InputAction> validActions = new List<InputAction>();

            foreach (var actionName in actionNames)
            {
                if (string.IsNullOrEmpty(actionName)) continue;
                
                var action = playerInput.actions.FindAction(actionName);
                if (action != null)
                {
                    validActions.Add(action);
                }
                else
                {
                    Debug.LogWarning($"[ActionBindingDisplay] Action '{actionName}' not found!", this);
                }
            }
            
            if (validActions.Count == 0)
            {
                Debug.LogWarning("[ActionBindingDisplay] No valid actions found!", this);
                return null;
            }
            
            return InputManager.GetActionBindings(validActions.ToArray(), separator, useSprites, compositePartFilter);
        }


        public InputAction[] GetAllActions()
        {
            if (actionNames == null || actionNames.Length == 0)
            {
                return Array.Empty<InputAction>();
            }

            var playerInput = InputManager.Instance?.PlayerInput;
            if (playerInput == null)
            {
                return Array.Empty<InputAction>();
            }

            List<InputAction> validActions = new List<InputAction>();
            foreach (var actionName in actionNames)
            {
                if (string.IsNullOrEmpty(actionName)) continue;
                
                var action = playerInput.actions.FindAction(actionName);
                if (action != null)
                {
                    validActions.Add(action);
                }
            }

            return validActions.ToArray();
        }
    }
}