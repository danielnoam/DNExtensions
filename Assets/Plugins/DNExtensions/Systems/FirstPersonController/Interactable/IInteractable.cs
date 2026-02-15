

namespace DNExtensions.FirstPersonController.Interactable
{
    public interface IInteractable
    {
        void Interact(InteractorData interactorData);
        bool CanInteract();
        void ShowInteractionTip();
        void HideInteractionTip();
    }
}