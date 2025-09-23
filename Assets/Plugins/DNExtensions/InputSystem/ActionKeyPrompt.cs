using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using DNExtensions.Button;

public class ActionKeyPrompt : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private InputActionReference[] inputActionReferences = Array.Empty<InputActionReference>();
    
    
    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI prompt;
    [SerializeField] private PlayerInput playerInput;


    private void OnValidate()
    {
        if (!playerInput) playerInput = FindFirstObjectByType<PlayerInput>();
    }

    private void Awake()
    {
        if (!playerInput) playerInput = FindFirstObjectByType<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput)
        {
            playerInput.onControlsChanged += SetTextBasedOnAction;
        }
    }

    private void OnDisable()
    {
        if (playerInput)
        {
            playerInput.onControlsChanged -= SetTextBasedOnAction;
        }
    }

    [Button]
    private void SetTextBasedOnAction(PlayerInput input)
    {
        if (inputActionReferences == null || inputActionReferences.Length < 1 || !prompt) return;
        
        var currentDeviceIsGamepad = playerInput && playerInput.currentControlScheme == "Gamepad";

        prompt.text = "";

        for (var index = 0; index < inputActionReferences.Length; index++)
        {
            var inputActionReference = inputActionReferences[index];
            if (!inputActionReference) continue;

            if (inputActionReferences.Length > 1 && index < inputActionReferences.Length - 1)
            {
                prompt.text += $"{inputActionReference.action.GetBindingDisplayString(0, currentDeviceIsGamepad ? "Gamepad" : "Keyboard&Mouse")} | ";
            }
            else
            {
                prompt.text += $"{inputActionReference.action.GetBindingDisplayString(0, currentDeviceIsGamepad ? "Gamepad" : "Keyboard&Mouse")}";
            }

        }
    }
    
    
}
