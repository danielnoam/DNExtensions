using DNExtensions;
using DNExtensions.Button;
using UnityEngine;
using UnityEngine.Events;

public class InteractableComponent : MonoBehaviour, IInteractable
{
    
    [Header("Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool limitInteractionsToOnce;
    [Space(10)]
    [SerializeField] private UnityEvent onInteract;
    
    [SerializeField, ReadOnly] private bool hasInteracted;
    
    
    public bool CanInteract()
    {
        return canInteract;
    }

    [Button]
    public void Interact()
    {
        if (limitInteractionsToOnce && hasInteracted)  return;
        hasInteracted = true;
        onInteract?.Invoke();
    }
}