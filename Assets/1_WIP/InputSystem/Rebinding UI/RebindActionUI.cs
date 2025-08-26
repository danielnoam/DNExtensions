// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using InputBindingSystem;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.Events;
// using VInspector;
// using TMPro;
// using UnityEngine.UI;
// using PrimeTween;
//
//
// public class RebindActionUI : MonoBehaviour, IInputActionReferenceProvider
// {
//
//     [Header("Binding Configuration")] 
//     [SerializeField] private bool notEditable;
//     [SerializeField] private InputActionReference action;
//     [SerializeField] [BindingDropdown] private string m_BindingId;
//     [SerializeField] private InputBinding.DisplayStringOptions m_DisplayStringOptions;
//
//     [Header("UI Elements")]
//     [SerializeField] private bool showIconInsteadOfText;
//     [SerializeField] private bool overrideActionLabel;
//     [ShowIf("overrideActionLabel")]
//     [SerializeField] private string actionLabelString;
//     [EndIf]
//     [SerializeField] private Image actionBindingIcon;
//     [SerializeField] private TextMeshProUGUI m_ActionLabel;
//     [SerializeField] private TextMeshProUGUI m_BindingText;
//     [SerializeField] private Button actionButton;
//     [SerializeField] private Button resetButton;
//     
//     [Header("Overlay Elements")]
//     [SerializeField] private GameObject rebindOverlay;
//     [SerializeField] private TextMeshProUGUI rebindText;
//     [SerializeField] private TextMeshProUGUI errorText;
//     [SerializeField] private float errorDisplayTime = 1f;
//     [SerializeField] private bool overrideErrorLabel;
//     [ShowIf("m_OverrideErrorLabel")]
//     [SerializeField] private string errorLabelString;
//     [EndIf] 
//     [SerializeField] private bool shakeErrorLabel;
//     [SerializeField] private float shakeErrorLabelDuration = 0.7f;
//     [SerializeField] private Vector3 shakeErrorLabelAxis = new Vector3(3f, 3f, 0f);
//     [SerializeField] private float shakeErrorLabelFrequency = 10f;
//
//     [Header("Events")]
//     [SerializeField] private UpdateBindingUIEvent updateBindingUIEvent;
//     [SerializeField] private InteractiveRebindEvent rebindStartEvent;
//     [SerializeField] private InteractiveRebindEvent rebindStopEvent;
//
//
//     private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
//     private static List<RebindActionUI> rebindActionUIs;
//     private bool _isRebinding;
//     private Coroutine _errorCoroutine;
//     private readonly List<InputActionMap> _disabledActionMaps = new List<InputActionMap>();
//
//     private void Awake()
//     {
//         if (resetButton) { resetButton.gameObject.SetActive(!notEditable); }
//         if (actionButton) { actionButton.interactable = !notEditable; }
//     }
//
//     private void OnEnable()
//     {
//         rebindActionUIs ??= new List<RebindActionUI>();
//         rebindActionUIs.Add(this);
//         if (rebindActionUIs.Count == 1)
//         {
//             InputSystem.onActionChange += OnActionChange;
//         }
//         
//         UpdateBindingDisplay();
//     }
//
//     private void OnDisable()
//     {
//         if (_rebindOperation != null)
//         {
//             _rebindOperation.Dispose();
//             _rebindOperation = null;
//         }
//
//         if (_errorCoroutine != null)
//         {
//             StopCoroutine(_errorCoroutine);
//             _errorCoroutine = null;
//         }
//
//         rebindActionUIs.Remove(this);
//         if (rebindActionUIs.Count == 0)
//         {
//             rebindActionUIs = null;
//             InputSystem.onActionChange -= OnActionChange;
//         }
//     }
//     
//     #if UNITY_EDITOR
//     private void OnValidate()
//     {
//         if (resetButton) { resetButton.gameObject.SetActive(!notEditable); }
//         if (actionButton) { actionButton.interactable = !notEditable; }
//         
//         UpdateActionLabel();
//         UpdateBindingDisplay();
//     }
//     #endif
//
//
//     #region Public Properties
//     public InputActionReference actionReference => action;
//
//     public string bindingId
//     {
//         get => m_BindingId;
//         set
//         {
//             m_BindingId = value;
//             UpdateBindingDisplay();
//         }
//     }
//
//     public InputBinding.DisplayStringOptions displayStringOptions
//     {
//         get => m_DisplayStringOptions;
//         set
//         {
//             m_DisplayStringOptions = value;
//             UpdateBindingDisplay();
//         }
//     }
//
//     public TextMeshProUGUI actionLabel
//     {
//         get => m_ActionLabel;
//         set
//         {
//             m_ActionLabel = value;
//             UpdateActionLabel();
//         }
//     }
//
//     public TextMeshProUGUI bindingText
//     {
//         get => m_BindingText;
//         set
//         {
//             m_BindingText = value;
//             UpdateBindingDisplay();
//         }
//     }
//
//     public InputActionRebindingExtensions.RebindingOperation ongoingRebind => _rebindOperation;
//     #endregion
//
//     
//     #region Public Methods
//     
//     
//     public void CancelRebind()
//     {
//         if (_rebindOperation != null)
//         {
//             _rebindOperation.Cancel();
//         }
//     }
//     
//     
//     public void StartInteractiveRebind()
//     {
//         if (!ResolveActionAndBinding(out var inputAction, out var bindingIndex))
//             return;
//
//         if (inputAction.bindings[bindingIndex].isComposite)
//         {
//             var firstPartIndex = bindingIndex + 1;
//             if (firstPartIndex < inputAction.bindings.Count && inputAction.bindings[firstPartIndex].isPartOfComposite)
//                 PerformInteractiveRebind(inputAction, firstPartIndex, true);
//         }
//         else
//         {
//             PerformInteractiveRebind(inputAction, bindingIndex);
//         }
//     }
//
//     public void ResetToDefault()
//     {
//         if (!ResolveActionAndBinding(out var inputAction, out var bindingIndex))
//             return;
//
//         if (inputAction.bindings[bindingIndex].isComposite)
//         {
//             for (var i = bindingIndex + 1; i < inputAction.bindings.Count && inputAction.bindings[i].isPartOfComposite; ++i)
//                 inputAction.RemoveBindingOverride(i);
//         }
//         else
//         {
//             inputAction.RemoveBindingOverride(bindingIndex);
//         }
//
//         UpdateBindingDisplay();
//     }
//
//     private bool ResolveActionAndBinding(out InputAction inputAction, out int bindingIndex)
//     {
//         bindingIndex = -1;
//
//         inputAction = this.action?.action;
//         if (inputAction == null)
//             return false;
//
//         if (string.IsNullOrEmpty(m_BindingId))
//             return false;
//
//         var guid = new Guid(m_BindingId);
//         bindingIndex = inputAction.bindings.IndexOf(x => x.id == guid);
//         
//         if (bindingIndex == -1)
//         {
//             Debug.LogError($"Cannot find binding with ID '{guid}' on '{inputAction}'", this);
//             return false;
//         }
//
//         return true;
//     }
//
//     private void UpdateBindingDisplay()
//     {
//         var displayString = string.Empty;
//         var deviceLayoutName = default(string);
//         var controlPath = default(string);
//
//         var inputAction = this.action?.action;
//         if (inputAction == null) return;
//
//         var bindingIndex = inputAction.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
//         if (bindingIndex == -1) return;
//
//         // Check if this is a composite binding
//         var isComposite = inputAction.bindings[bindingIndex].isComposite;
//         var isPartOfComposite = inputAction.bindings[bindingIndex].isPartOfComposite;
//
//         if (isComposite)
//         {
//             // For composites, we want to show all the component parts
//             var firstPartIndex = bindingIndex + 1;
//             var compositePartStrings = new List<string>();
//             var compositeIcons = new List<Sprite>();
//
//             // Collect all parts of the composite
//             for (int i = firstPartIndex; i < inputAction.bindings.Count && inputAction.bindings[i].isPartOfComposite; i++)
//             {
//                 var partString = inputAction.GetBindingDisplayString(i, out var partDeviceLayout, out var partControlPath, m_DisplayStringOptions);
//                 compositePartStrings.Add(partString);
//
//                 if (showIconInsteadOfText && inputReader)
//                 {
//                     Sprite partIcon = null;
//                     if (partDeviceLayout?.Contains("Gamepad") == true)
//                     {
//                         string controlName = partControlPath?.Split('/').LastOrDefault();
//                         partIcon = inputReader.gamepadIcons.GetSprite(controlName);
//                     }
//                     else if (partDeviceLayout?.Contains("Keyboard") == true || partDeviceLayout?.Contains("Mouse") == true)
//                     {
//                         partIcon = inputReader.keyboardIcons.GetSprite(partControlPath);
//                     }
//                     compositeIcons.Add(partIcon);
//                 }
//             }
//
//             // If we're showing icons and all parts have icons available, show them
//             bool canShowIcons = showIconInsteadOfText && 
//                                compositeIcons.Count > 0 && 
//                                compositeIcons.All(icon => icon != null);
//
//             if (canShowIcons)
//             {
//                 if (actionBindingIcon)
//                 {
//                     
//                     // For now, just show the first icon
//                     actionBindingIcon.sprite = compositeIcons[0];
//                     actionBindingIcon.enabled = true;
//                     actionBindingIcon.gameObject.SetActive(true);
//                 }
//                 if (m_BindingText)
//                     m_BindingText.gameObject.SetActive(false);
//             }
//             else
//             {
//                 // Fall back to text for composite
//                 displayString = string.Join(", ", compositePartStrings);
//                 if (m_BindingText)
//                 {
//                     m_BindingText.gameObject.SetActive(true);
//                     m_BindingText.text = displayString;
//                 }
//                 if (actionBindingIcon)
//                 {
//                     actionBindingIcon.enabled = false;
//                     actionBindingIcon.gameObject.SetActive(false);
//                 }
//             }
//         }
//         else if (!isPartOfComposite)
//         {
//             // Handle single binding normally
//             displayString = inputAction.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, m_DisplayStringOptions);
//
//             if (showIconInsteadOfText && inputReader != null)
//             {
//                 Sprite iconSprite = null;
//                 if (deviceLayoutName?.Contains("Gamepad") == true)
//                 {
//                     string controlName = controlPath?.Split('/').LastOrDefault();
//                     iconSprite = inputReader.gamepadIcons.GetSprite(controlName);
//                 }
//                 else if (deviceLayoutName?.Contains("Keyboard") == true || deviceLayoutName?.Contains("Mouse") == true)
//                 {
//                     iconSprite = inputReader.keyboardIcons.GetSprite(controlPath);
//                 }
//
//                 if (iconSprite && actionBindingIcon)
//                 {
//                     actionBindingIcon.sprite = iconSprite;
//                     actionBindingIcon.enabled = true;
//                     actionBindingIcon.gameObject.SetActive(true);
//                     if (m_BindingText)
//                         m_BindingText.gameObject.SetActive(false);
//                 }
//                 else
//                 {
//                     // If no icon found, hide the image and show text instead
//                     if (actionBindingIcon)
//                     {
//                         actionBindingIcon.enabled = false;
//                         actionBindingIcon.gameObject.SetActive(false);
//                     }
//                     if (m_BindingText)
//                     {
//                         m_BindingText.gameObject.SetActive(true);
//                         m_BindingText.text = displayString;
//                     }
//                 }
//             }
//             else
//             {
//                 // Show text, hide icon
//                 if (m_BindingText)
//                 {
//                     m_BindingText.gameObject.SetActive(true);
//                     m_BindingText.text = displayString;
//                 }
//                 if (actionBindingIcon)
//                 {
//                     actionBindingIcon.enabled = false;
//                     actionBindingIcon.gameObject.SetActive(false);
//                 }
//             }
//         }
//
//         // Update reset button state
//         if (resetButton)
//         {
//             resetButton.interactable = HasCustomBinding();
//         }
//         
//         updateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
//     }
//
//     #endregion
//
//     
//     #region Private Methods
//     
//     private bool HasCustomBinding()
//     {
//         if (!ResolveActionAndBinding(out var inputAction, out var bindingIndex))
//             return false;
//
//         // For composite bindings
//         if (inputAction.bindings[bindingIndex].isComposite)
//         {
//             for (var i = bindingIndex + 1; i < inputAction.bindings.Count && inputAction.bindings[i].isPartOfComposite; ++i)
//             {
//                 if (!string.IsNullOrEmpty(inputAction.bindings[i].overridePath))
//                     return true;
//             }
//             return false;
//         }
//     
//         // For regular bindings
//         return !string.IsNullOrEmpty(inputAction.bindings[bindingIndex].overridePath);
//     }
//     
//
//     
//     private void PerformInteractiveRebind(InputAction inputAction, int bindingIndex, bool allCompositeParts = false)
//     {
//         _rebindOperation?.Cancel();
//         _isRebinding = true;
//
//         // Disable all action maps to prevent any input during rebinding
//         _disabledActionMaps.Clear();
//         foreach (var actionMap in inputAction.actionMap.asset.actionMaps)
//         {
//             if (actionMap.enabled)
//             {
//                 actionMap.Disable();
//                 _disabledActionMaps.Add(actionMap);
//             }
//         }
//
//         _rebindOperation = inputAction.PerformInteractiveRebinding(bindingIndex)
//             .WithControlsExcluding("Mouse") // Disable input from mouse
//             .WithCancelingThrough("<Keyboard>/escape") // not fucking working???
//             .WithCancelingThrough("<Gamepad>/select")
//             .OnMatchWaitForAnother(0.1f)
//             .OnComplete(operation =>
//             {
//                 rebindStopEvent?.Invoke(this, operation);
//                 
//
//                 
//                 if (CheckDuplicateBindings(inputAction, bindingIndex, allCompositeParts)) // if the new binding is already in use
//                 {
//                     inputAction.RemoveBindingOverride(bindingIndex);
//                     _errorCoroutine = StartCoroutine(ShowErrorMessage());
//                     
//                 } else {
//                     
//                     rebindOverlay?.SetActive(false);
//                     foreach (var actionMap in _disabledActionMaps)
//                     {
//                         actionMap.Enable();
//                     }
//                     _disabledActionMaps.Clear();
//
//                     // if the new binding is the same as the default one
//                     string defaultPath = inputAction.bindings[bindingIndex].path;
//                     string currentPath = inputAction.bindings[bindingIndex].effectivePath;
//                     if (defaultPath == currentPath)
//                     {
//                         inputAction.RemoveBindingOverride(bindingIndex);
//                     }
//
//                 }
//
//                 _rebindOperation = null;
//                 _isRebinding = false;
//                 UpdateBindingDisplay();
//
//                 // If more composite bindings to set
//                 if (allCompositeParts)
//                 {
//                     var nextBindingIndex = bindingIndex + 1;
//                     if (nextBindingIndex < inputAction.bindings.Count && inputAction.bindings[nextBindingIndex].isPartOfComposite)
//                         PerformInteractiveRebind(inputAction, nextBindingIndex, true);
//                 }
//             })
//             .OnCancel(operation =>
//             {
//                 rebindStopEvent?.Invoke(this, operation);
//                 rebindOverlay?.SetActive(false);
//                 _rebindOperation = null;
//                 _isRebinding = false;
//                 foreach (var actionMap in _disabledActionMaps)
//                 {
//                     actionMap.Enable();
//                 }
//                 _disabledActionMaps.Clear();
//                 UpdateBindingDisplay();
//             });
//
//         // Show overlay
//         rebindOverlay?.SetActive(true);
//         if (rebindText)
//         {
//             var partName = inputAction.bindings[bindingIndex].isPartOfComposite ? $"Binding '{inputAction.bindings[bindingIndex].name}'. " : "";
//             rebindText.text = $"{partName}Press a button to bind...";
//         }
//
//         rebindStartEvent?.Invoke(this, _rebindOperation);
//         _rebindOperation.Start();
//     }
//
//         private bool CheckDuplicateBindings(InputAction inputAction, int bindingIndex, bool allCompositeParts = false)
//         {
//             var newBinding = inputAction.bindings[bindingIndex];
//             
//             foreach (var binding in inputAction.actionMap.bindings)
//             {
//                 if (binding.action == newBinding.action)
//                 {
//                     if (binding.isPartOfComposite && binding.id != newBinding.id)
//                     {
//                         if (binding.effectivePath == newBinding.effectivePath)
//                             return true;
//                     }
//                     continue;
//                 }
//
//                 if (binding.effectivePath == newBinding.effectivePath)
//                     return true;
//             }
//
//             if (allCompositeParts)
//             {
//                 for (int i = 1; i < bindingIndex; i++)
//                 {
//                     if (inputAction.bindings[i].effectivePath == newBinding.overridePath)
//                         return true;
//                 }
//             }
//
//             return false;
//         }
//
//     private IEnumerator ShowErrorMessage()
//     {
//         // Disable bind text
//         if (rebindText != null)
//         {
//             rebindText.gameObject.SetActive(false);
//         }
//     
//         // Show the error message
//         if (errorText)
//         {
//             if (overrideErrorLabel && !string.IsNullOrEmpty(errorLabelString))
//             {
//                 errorText.text = errorLabelString;
//             }
//             else
//             {
//                 errorText.text = "Binding already in use!";
//             }
//             errorText.gameObject.SetActive(true);
//             if (shakeErrorLabel) { Tween.ShakeLocalPosition(errorText.gameObject.transform, strength: shakeErrorLabelAxis, duration: shakeErrorLabelDuration, frequency: shakeErrorLabelFrequency); }
//         }
//
//         // Wait
//         yield return new WaitForSeconds(errorDisplayTime);
//
//         // Disable error message
//         if (errorText)
//         {
//             errorText.gameObject.SetActive(false);
//         }
//
//         // Re-enable action maps before restarting the rebind process
//         foreach (var actionMap in _disabledActionMaps)
//         {
//             actionMap.Enable();
//         }
//         _disabledActionMaps.Clear();
//     
//         // restart the rebinding process
//         if (rebindText)
//         {
//             rebindText.gameObject.SetActive(true);
//         }
//
//         // Get the current action and binding index
//         if (ResolveActionAndBinding(out var inputAction, out var bindingIndex))
//         {
//             // Restart the rebinding process
//             if (inputAction.bindings[bindingIndex].isComposite)
//             {
//                 var firstPartIndex = bindingIndex + 1;
//                 if (firstPartIndex < inputAction.bindings.Count && inputAction.bindings[firstPartIndex].isPartOfComposite)
//                     PerformInteractiveRebind(inputAction, firstPartIndex, true);
//             } 
//             else 
//             {
//                 PerformInteractiveRebind(inputAction, bindingIndex);
//             }
//         }
//
//         _errorCoroutine = null;
//     }
//
//     private void UpdateActionLabel()
//     {
//         if (m_ActionLabel != null)
//         {
//             var inputAction = this.action?.action;
//             m_ActionLabel.text = overrideActionLabel ? actionLabelString : 
//                                (inputAction != null ? inputAction.name : string.Empty);
//         }
//     }
//
//     private static void OnActionChange(object obj, InputActionChange change)
//     {
//         if (change != InputActionChange.BoundControlsChanged)
//             return;
//
//         var action = obj as InputAction;
//         var actionMap = action?.actionMap ?? obj as InputActionMap;
//         var actionAsset = actionMap?.asset ?? obj as InputActionAsset;
//
//         foreach (var component in rebindActionUIs)
//         {
//             var referencedAction = component.actionReference?.action;
//             if (referencedAction == null)
//                 continue;
//
//             if (referencedAction == action ||
//                 referencedAction.actionMap == actionMap ||
//                 referencedAction.actionMap?.asset == actionAsset)
//                 component.UpdateBindingDisplay();
//         }
//     }
//     #endregion
//
//     #region Event Classes
//     [Serializable]
//     public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
//     {
//     }
//
//     [Serializable]
//     public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
//     {
//     }
//     #endregion
// }