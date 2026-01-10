using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DNExtensions.Button;

public class UISpringAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;

    [Header("Animation Settings")]
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 500f, 0f);
    [SerializeField] private bool animateOnStart = true;
    
    [Header("Scale")]
    [SerializeField] private Vector3Spring scaleSpring = new Vector3Spring();
    
    [Header("Position")]
    [SerializeField] private Vector3Spring positionSpring = new Vector3Spring();


    private Vector3 _targetPosition;
    private Vector3 _baseScale;
    private Vector2 _basePivot;
    private float _limitSquashTimer;
    private Vector2 _targetPivot;

    private void Awake()
    {
        if (!rectTransform) return;
        
        _baseScale = rectTransform.localScale;
        _basePivot = rectTransform.pivot;
        _targetPivot = _basePivot;
        
        // Store target position (where the title should end up)
        _targetPosition = rectTransform.anchoredPosition3D;
        
        if (animateOnStart)
        {
            // Start from offscreen
            Vector3 startPosition = _targetPosition + startOffset;
            positionSpring.SetValue(startPosition);
            positionSpring.target = _targetPosition;
            
            // Start slightly squashed in direction of movement
            Vector3 startSquash = Vector3.one;
            if (Mathf.Abs(startOffset.y) > Mathf.Abs(startOffset.x))
            {
                // Vertical movement - squash vertically
                startSquash.y = 0.7f;
                startSquash.x = 1.2f;
            }
            else
            {
                // Horizontal movement - squash horizontally
                startSquash.x = 0.7f;
                startSquash.y = 1.2f;
            }
            scaleSpring.SetValue(startSquash);
        }
        else
        {
            positionSpring.SetValue(_targetPosition);
            positionSpring.target = _targetPosition;
            scaleSpring.SetValue(Vector3.one);
        }
        
        scaleSpring.target = Vector3.one;
        
    }
    
    private void Update()
    {
        positionSpring.Update(Time.deltaTime);
        scaleSpring.Update(Time.deltaTime);
        
        
        
        if (rectTransform)
        {
            // Smoothly lerp pivot back to base
            rectTransform.pivot = Vector2.Lerp(
                rectTransform.pivot, 
                _targetPivot, 
                Time.deltaTime * 10f
            );
            
            Vector3 finalScale = Vector3.Scale(_baseScale, scaleSpring.Value);
            rectTransform.localScale = finalScale;
            
            // Apply position directly
            rectTransform.anchoredPosition3D = positionSpring.Value;
        }
    }
    
    
    
    [Button]
    public void AnimateFromOffset()
    {
        Vector3 startPosition = _targetPosition + startOffset;
        positionSpring.SetValue(startPosition);
        positionSpring.target = _targetPosition;
    }

    [Button]
    public void AnimateToOffset()
    {
        Vector3 targetPosition = _targetPosition + startOffset;
        positionSpring.target = targetPosition;
    }
    
    
    [Button]
    public void ResetToTarget()
    {
        positionSpring.SetValue(_targetPosition);
        positionSpring.target = _targetPosition;
        scaleSpring.SetValue(Vector3.one);
        scaleSpring.target = Vector3.one;
    }
    
}