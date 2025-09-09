using System;
using DNExtensions;
using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private SOAudioEvent interactionSfx;
    // [SerializeField] private float outlineWidthOnHighlight = 5f;
    // [SerializeField] private float outlineWidthOnUnHighlight;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    // [SerializeField] private Outline outline;
    
    private bool _isHighlighted;
    private Sequence _highlightSequence;
    
    public bool CanInteract => canInteract;
    
    public event Action<FPCPlayerInteraction> OnInteract;
    public event Action OnUnHighlight;
    public event Action OnHighlight;


    // private void Awake()
    // {
    //     if (outline)
    //     {
    //         outline.OutlineColor = outlineWidthOnUnHighlight;
    //         outline.OutlineWidth = 0;
    //         outline.OutlineMode = .OutlineMode;
    //     }
    // }
    

    public void Highlight()
    {
        if (_isHighlighted) return;

        _isHighlighted = true;
        
        // if (outline)
        // {
        //     if (_highlightSequence.isAlive) _highlightSequence.Stop();
        //     _highlightSequence = Sequence.Create()
        //         .Group(Tween.Custom(
        //             startValue: outline.OutlineWidth,
        //             endValue: outlineWidthOnHighlight,
        //             duration: 0.5f,
        //             onValueChange: value => outline.OutlineWidth = value
        //             ));
        // }
        OnHighlight?.Invoke();
    }
    
    public void UnHighlight()
    {
        if (!_isHighlighted) return;
        
        _isHighlighted = false;
        // if (outline)
        // {
        //     if (_highlightSequence.isAlive) _highlightSequence.Stop();
        //     _highlightSequence = Sequence.Create()
        //         .Group(Tween.Custom(
        //             startValue: outline.OutlineWidth,
        //             endValue: outlineWidthOnUnHighlight,
        //             duration: 0.5f,
        //             onValueChange: value => outline.OutlineWidth = value
        //         ));
        // }
        OnUnHighlight?.Invoke();
    }
    
    public void Interact(FPCPlayerInteraction interactor)
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