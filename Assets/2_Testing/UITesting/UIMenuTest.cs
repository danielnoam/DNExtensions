using System;
using System.Collections.Generic;
using DNExtensions.Button;
using PrimeTween;
using UnityEngine;

public class UIMenuTest : MonoBehaviour
{

    [SerializeField] private RectTransform leftImage;
    [SerializeField] private RectTransform rightImage;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform[] objectsToAnimate;

    
    private Sequence _startSequence;
    private readonly List<Vector2> _startPositions = new List<Vector2>();
    private Vector2 _topBarStartSizeDelta;
    
    private void Awake()
    {
        foreach (var obj in objectsToAnimate)
        {
            _startPositions.Add(obj.anchoredPosition);
            obj.anchoredPosition = Vector2.down * 1000;
        }
        
        _topBarStartSizeDelta = topBar.sizeDelta;

        
        PlayStartAnimation();
    }

    private void Update()
    {
        if (leftImage)
        {
            leftImage.localEulerAngles = new Vector3(0, 0, Mathf.Sin(Time.time) * 25);
        }
        
        if (rightImage)
        {
            rightImage.localEulerAngles = new Vector3(0, 0, Mathf.Sin(Time.time) * -25);
        }
    }

    [Button(ButtonPlayMode.OnlyWhenPlaying)]
    public void PlayStartAnimation()
    {
        if (_startSequence.isAlive) _startSequence.Stop();
        
        foreach (var obj in objectsToAnimate)
        {
            obj.anchoredPosition = Vector2.down * 1000;
        }
        topBar.sizeDelta = Vector2.zero;
        
        

        _startSequence = Sequence.Create();
        _startSequence.Group(Tween.UISizeDelta(topBar, _topBarStartSizeDelta, 1f, Ease.OutBack));

        for (var index = 0; index < objectsToAnimate.Length; index++)
        {
            var obj = objectsToAnimate[index];
            var endPosition = _startPositions[Array.IndexOf(objectsToAnimate, obj)];
            if (index == 0)
            {
                _startSequence.Group(Tween.UIAnchoredPosition(obj, Vector2.down * 1000, endPosition, 1f, Ease.OutBack));
            }
            else
            {
                _startSequence.Group(Tween.UIAnchoredPosition(obj, Vector2.down * 1000, endPosition, index * 1.2f, Ease.OutBack));
            }

        }
    }
}
