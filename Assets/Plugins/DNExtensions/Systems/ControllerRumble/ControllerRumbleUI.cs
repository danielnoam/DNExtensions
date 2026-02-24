using UnityEngine;
using TMPro;

namespace  DNExtensions.Systems.ControllerRumble
{
    
    /// <summary>
    /// Listens to a ControllerRumbleListener and applies a shaking effect to a UI element based on the rumble intensity.
    /// </summary>
    [DisallowMultipleComponent]
    public class ControllerRumbleUI : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The maximum movement distance (in UI units) based on full rumble intensity (1.0).")]
        [SerializeField] private float maxMovementDistance = 10f;
        [Tooltip("How quickly the UI element moves towards the shake position. Higher = Snappier.")] [SerializeField]
        private float shakeSpeed = 25f;
        [Tooltip("Minimum intensity threshold to start shaking (prevents micro-jitters).")] [SerializeField]
        private float intensityThreshold = 0.01f;

        [Header("References")] 
        [SerializeField] private ControllerRumbleListener listener;
        [SerializeField] private RectTransform uiRectTransform;
        [SerializeField] private TextMeshProUGUI infoText;

        private Vector3 _originalLocalPosition;
        private Vector3 _currentVelocity = Vector3.zero;
        private float _noiseTimer;
        private bool _isRumbling;

        private void Awake()
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
            if (!listener) return;
            
            float combinedIntensity = Mathf.Max(listener.CurrentCombinedLow, listener.CurrentCombinedHigh);
            
            if (infoText)
            {
                infoText.text =
                    $"Low: {listener.CurrentCombinedLow:F2}" +
                    $"\nHigh: {listener.CurrentCombinedHigh:F2}" +
                    $"\nIntensity: {combinedIntensity:F2}" +
                    $"\nEffects: {listener.ActiveEffects}";
            }


            bool shouldRumble = combinedIntensity > intensityThreshold;

            if (shouldRumble)
            {
                _isRumbling = true;
                _noiseTimer += Time.deltaTime * shakeSpeed;
                
                float offsetX = (Mathf.PerlinNoise(_noiseTimer, 0f) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(0f, _noiseTimer) - 0.5f) * 2f;
                
                Vector3 targetOffset = new Vector3(
                    offsetX * combinedIntensity * maxMovementDistance,
                    offsetY * combinedIntensity * maxMovementDistance,
                    0f
                );
                
                uiRectTransform.localPosition = _originalLocalPosition + targetOffset;
            }
            else if (_isRumbling)
            {
                uiRectTransform.localPosition = Vector3.SmoothDamp(
                    uiRectTransform.localPosition,
                    _originalLocalPosition,
                    ref _currentVelocity,
                    1f / shakeSpeed
                );
                
                if (Vector3.Distance(uiRectTransform.localPosition, _originalLocalPosition) < 0.1f)
                {
                    uiRectTransform.localPosition = _originalLocalPosition;
                    _isRumbling = false;
                    _currentVelocity = Vector3.zero;
                }
            }
            else
            {
                uiRectTransform.localPosition = _originalLocalPosition;
            }
        }
    }
}