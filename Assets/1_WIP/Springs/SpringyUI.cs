using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Button;

public class SpringyUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 500f, 0f);
    [SerializeField] private Vector3 scaleOffset = new Vector3(0.7f, 1.2f, 1f);
    [SerializeField] private Vector3Spring scaleSpring = new Vector3Spring();
    [SerializeField] private Vector3Spring positionSpring = new Vector3Spring();
    
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    

    private Vector3 _baseAnchoredPosition;
    private Vector3 _baseScale;

    private void OnValidate()
    {
        if (!rectTransform)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }


    private void Awake()
    {
        if (!rectTransform) return;
        
        _baseAnchoredPosition = rectTransform.anchoredPosition3D;
        _baseScale = rectTransform.localScale;
        positionSpring.target = _baseAnchoredPosition;
        scaleSpring.target = _baseScale;
        
        if (animateOnStart)
        {
            AnimateFromOffset();
        }

    }
    
    private void Update()
    {
        positionSpring.Update(Time.deltaTime);
        scaleSpring.Update(Time.deltaTime);
        
        rectTransform.localScale = scaleSpring.Value;
        rectTransform.anchoredPosition3D = positionSpring.Value;
        
        positionSpring.DrawDebug(transform.position, 125);
    }
    
    [Button]
    public void AnimateFromOffset()
    {
        positionSpring.target = _baseAnchoredPosition;
        scaleSpring.target = _baseScale;
        
        scaleSpring.SetValue(scaleOffset);
        positionSpring.SetValue(_baseAnchoredPosition + positionOffset);
    }

    [Button]
    public void AnimateToOffset()
    {
        positionSpring.target = _baseAnchoredPosition + positionOffset;
        scaleSpring.target = scaleOffset;
        
        scaleSpring.SetValue(_baseScale);
        positionSpring.SetValue(_baseAnchoredPosition);
    }
    
}