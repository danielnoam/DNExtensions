using System;
using PrimeTween;
using UnityEngine;

public class Screen : MonoBehaviour
{
    
    [Header("Animation")]
    [SerializeField] protected TweenSettings showSettings = new TweenSettings(duration: 0.25f);
    [SerializeField] protected TweenSettings hideSettings = new TweenSettings(duration: 0.25f);

    [Header("References")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform rectTransform;
    
    protected Vector3 TransformOriginalAnchoredPosition;
    protected Vector3 TransformOriginalScale;
    protected Sequence AnimationSequence;
    
    public TweenSettings ShowSettings => showSettings;
    public TweenSettings HideSettings => hideSettings;
    

    private void Awake()
    {
        if (rectTransform)
        {
            TransformOriginalScale = rectTransform.localScale;
            TransformOriginalAnchoredPosition = rectTransform.anchoredPosition3D;
        }
    }

    public void Show(bool animated = true, Action onComplete = null)
    {
        gameObject.SetActive(true);
        
        if (!animated)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            rectTransform.localScale = TransformOriginalScale;
            rectTransform.anchoredPosition3D = TransformOriginalAnchoredPosition;
            
            onComplete?.Invoke();
            return;
        }

        AnimationSequence.Stop();
        
        if (canvasGroup) canvasGroup.alpha = 0f;
        
        AnimationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, new TweenSettings<float>(1f, showSettings)))
            .ChainCallback(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                onComplete?.Invoke();
            });
    }


    
    public void Hide(bool animated = true, Action onComplete = null)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (!animated)
        {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        AnimationSequence.Stop();
        
        AnimationSequence = Sequence.Create()
            .Group(Tween.Alpha(canvasGroup, new TweenSettings<float>(0f, hideSettings)))
            .ChainCallback(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }
    
}