using System;
using System.Collections;
using DNExtensions;
using PrimeTween;
using TMPro;
using UnityEngine;

[SelectionBase]
public class SpeechBubble : MonoBehaviour
{

    [Header("Settings")]
    [Tooltip("Duration of the fade in/out animation")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Whether the speech bubble should rotate to face the camera")]
    [SerializeField] private bool rotateToCamera = true;
    [SerializeField, EnableIf("rotateToCamera")] private float rotationSpeed = 5f;
    [Tooltip("Whether the speech bubble's scale should change based on its distance to the camera")]
    [SerializeField] private bool distanceToCameraAffectsScale = true;
    [SerializeField, MinMaxRange(1, 2), EnableIf("distanceToCameraAffectsScale")] private RangedFloat minMaxScale = new RangedFloat(1, 1.5f);
    [SerializeField, MinMaxRange(0, 50), EnableIf("distanceToCameraAffectsScale")] private RangedFloat minMaxDistance = new RangedFloat(5, 15);
    
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI text;
    
    private Camera _cam;
    private Vector3 _baseScale;
    private RectTransform _rectTransform;
    private Coroutine _hideCoroutine;
    private Sequence _fadeSequence;

    private void Awake()
    {
        Hide(false);
        _cam = Camera.main;
        _rectTransform = canvasGroup.transform as RectTransform;
        if (_rectTransform) _baseScale = _rectTransform.localScale;
    }

    private void OnDestroy()
    {
        if (_fadeSequence.isAlive)
        {
            _fadeSequence.Stop();
        }
    }

    private void Update()
    {
        RotateToCamera();
        ChangeSizeBasedOnDistance();
    }

    private void RotateToCamera()
    {
        if (!_cam || !rotateToCamera) return;
        
        Vector3 directionToCamera = _rectTransform.position - _cam.transform.position;
        directionToCamera.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
        _rectTransform.rotation = Quaternion.Slerp(_rectTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    private void ChangeSizeBasedOnDistance()
    {
        if (!_cam || !distanceToCameraAffectsScale) return;

        float distance = Vector3.Distance(_rectTransform.position, _cam.transform.position);
        float t = Mathf.InverseLerp(minMaxDistance.minValue, minMaxDistance.maxValue, distance);
        float scaleMultiplier = Mathf.Lerp(minMaxScale.minValue, minMaxScale.maxValue, t);
        _rectTransform.localScale = _baseScale * scaleMultiplier;
    }
    
    
    public void Show(string message, float duration = 3.5f)
    {
        if (_fadeSequence.isAlive)
        {
            _fadeSequence.Stop();
        }
        
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
        }

        text.text = message;
        
        _fadeSequence = Sequence.Create();
        _fadeSequence.Group(Tween.Alpha(canvasGroup, 1f, fadeDuration));
        
        _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private void Hide(bool animate)
    {
        if (_fadeSequence.isAlive) return;
        
        if (animate)
        {
            _fadeSequence = Sequence.Create();
            _fadeSequence.Group(Tween.Alpha(canvasGroup, 0f, fadeDuration));
            _fadeSequence.OnComplete(() => { text.text = ""; }); 
        }
        else
        {
                    
            text.text = "";
            canvasGroup.alpha = 0f;
        }
    }
    
    
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide(true);
    }
    
}
