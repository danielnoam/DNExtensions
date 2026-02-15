using UnityEngine;

namespace DNExtensions.FirstPersonController.Interactable
{
    [DisallowMultipleComponent]
    public class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] protected bool canInteract = true;
        [SerializeField] protected bool onlyOneInteraction;
        
        protected bool IsHighlighted;
        protected bool WasInteracted;
        

        public virtual void Interact(InteractorData interactorData)
        {
            if (!CanInteract()) return;
            
            WasInteracted = true;
        }

        public virtual bool CanInteract()
        {
            return canInteract && (!onlyOneInteraction || !WasInteracted);
        }

        public virtual void ShowInteractionTip()
        {
            if (IsHighlighted) return;

            IsHighlighted = true;
        }

        public virtual void HideInteractionTip()
        {
            if (!IsHighlighted) return;

            IsHighlighted = false;
        }
    }
}