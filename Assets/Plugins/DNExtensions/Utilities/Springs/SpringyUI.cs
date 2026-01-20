using System;
using DNExtensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Button;

public class SpringyUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private bool position;
    [SerializeField, ShowIf("position")] private Vector3 positionOffset = new Vector3(0f, 500f, 0f);
    [SerializeField, ShowIf("position")] private Vector3Spring positionSpring = new Vector3Spring();
    [SerializeField] private bool scale;
    [SerializeField, ShowIf("scale")] private Vector3 scaleOffset = new Vector3(0.7f, 1.2f, 1f);
    [SerializeField, ShowIf("scale")] private Vector3Spring scaleSpring = new Vector3Spring();
    
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
        
        
        if (position) positionSpring.target = _baseAnchoredPosition;
        if (scale) scaleSpring.target = _baseScale;
        
        if (animateOnStart)
        {
            AnimateFromOffset();
        }

    }
    
    private void Update()
    {
        
        if (position)
        {
            positionSpring.Update(Time.deltaTime);
            rectTransform.anchoredPosition3D = positionSpring.Value;
        }

        if (scale)
        {
            scaleSpring.Update(Time.deltaTime);
            rectTransform.localScale = scaleSpring.Value;
        }


    }

    public void ToggleSpringsLock(bool resetVelocity)
    {
        if (position)
        {
            if (positionSpring.IsLocked)
            {
                positionSpring.Unlock();
            }
            else
            {
                positionSpring.Lock(resetVelocity);
            }
        }

        if (scale)
        {
            if (scaleSpring.IsLocked)
            {
                scaleSpring.Unlock();
            }
            else
            {
                scaleSpring.Lock(resetVelocity);
            }
        }
    }
    
    [Button]
    public void AnimateFromOffset()
    {
        if (position)
        {
            positionSpring.target = _baseAnchoredPosition;
            positionSpring.SetValue(_baseAnchoredPosition + positionOffset);
        }

        if (scale)
        {
            scaleSpring.target = _baseScale;
            scaleSpring.SetValue(scaleOffset);
        }
    }

    [Button]
    public void AnimateToOffset()
    {
        if (position)
        {
            positionSpring.target = _baseAnchoredPosition + positionOffset;
            positionSpring.SetValue(_baseAnchoredPosition);
        }

        if (scale)
        {
            scaleSpring.target = scaleOffset;
            scaleSpring.SetValue(_baseScale);
        }
    }
    
}