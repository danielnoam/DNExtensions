using System;
using DNExtensions.Systems.Scriptables;
using UnityEngine;

namespace DNExtensions.Systems.FirstPersonController.Interactable
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private bool canInteract = true;
        [SerializeField] private SOAudioEvent interactionSfx;

        [Header("References")]
        [SerializeField] private AudioSource audioSource;

        private bool _isHighlighted;

        public bool CanInteract => canInteract;
        
        public event Action<FPCInteraction> OnInteract;
        public event Action OnUnHighlight;
        public event Action OnHighlight;

        public void Highlight()
        {
            if (_isHighlighted) return;

            _isHighlighted = true;
            OnHighlight?.Invoke();
        }
        
        public void UnHighlight()
        {
            if (!_isHighlighted) return;

            _isHighlighted = false;
            OnUnHighlight?.Invoke();
        }
        
        public void Interact(FPCInteraction interactor)
        {
            if (!canInteract) return;
            interactionSfx?.Play(audioSource);
            OnInteract?.Invoke(interactor);
        }
        
        public void SetCanInteract(bool value)
        {
            canInteract = value;
        }
    }
}