using UnityEngine;
using DNExtensions.ControllerRumbleSystem;

public class ControllerRumbleUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The maximum movement distance (in UI units) based on full rumble intensity (1.0).")]
    [SerializeField] private float maxMovementDistance = 10f;
    [Tooltip("How quickly the UI element moves towards the shake position. Higher = Snappier.")]
    [SerializeField] private float shakeSpeed = 25f;
    
    [Header("References")]
    [SerializeField] private ControllerRumbleListener listener;
    [Tooltip("The RectTransform of the UI element (e.g., an Image of a controller).")]
    [SerializeField] private RectTransform uiRectTransform;

    private Vector3 _originalLocalPosition;
    private Vector3 _currentVelocity = Vector3.zero;
    private float _noiseTimer;
    private Vector3 _targetOffset = Vector3.zero;

    private void Start()
    {
        if (!listener || !uiRectTransform)
        {
            Debug.LogError("ControllerRumbleUI requires a Listener and a RectTransform reference.");
            enabled = false;
            return;
        }

        _originalLocalPosition = uiRectTransform.localPosition;
    }

    private void Update()
    {
        if (!listener)
        {
            return;
        }
        
        float lowIntensity = listener.CurrentCombinedLow; 
        float highIntensity = listener.CurrentCombinedHigh;

        // Get the strongest signal
        float intensity = Mathf.Max(lowIntensity, highIntensity);
        
        // Only calculate shake if intensity is above a small threshold
        if (intensity > 0.01f)
        {
            float shakeMagnitude = intensity * maxMovementDistance;
            
            _noiseTimer -= Time.deltaTime;
            if (_noiseTimer <= 0f)
            {
                // Generate a random direction (normalized) then multiply by magnitude
                Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
                _targetOffset = randomDir * shakeMagnitude;
                
                _noiseTimer = Random.Range(0.01f, 0.05f);
            }
            
            Vector3 targetPosition = _originalLocalPosition + _targetOffset;
            
            // Move towards the shaky target
            uiRectTransform.localPosition = Vector3.SmoothDamp(
                uiRectTransform.localPosition,
                targetPosition,
                ref _currentVelocity,
                1f / shakeSpeed
            );
        }
        else
        {
            // Return to center (Idle state)
            // This must be in an 'else' block so SmoothDamp isn't called twice
            uiRectTransform.localPosition = Vector3.SmoothDamp(
                uiRectTransform.localPosition,
                _originalLocalPosition,
                ref _currentVelocity,
                0.05f 
            );
        }
    }
}