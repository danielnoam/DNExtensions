
namespace DNExtensions.Systems.FirstPersonController.Interactable
{
    public interface IInteractable
    {
        public void Interact(InteractorData interactorData);
        public bool CanInteract();
        public void ShowInteractionTip();
        public void HideInteractionTip();
    }
}