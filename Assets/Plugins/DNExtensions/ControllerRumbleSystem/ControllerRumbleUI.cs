using UnityEngine;
using TMPro;

namespace  DNExtensions.ControllerRumbleSystem
{
    public class ControllerRumbleUI : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The maximum movement distance (in UI units) based on full rumble intensity (1.0).")]
        [SerializeField]
        private float maxMovementDistance = 10f;

        [Tooltip("How quickly the UI element moves towards the shake position. Higher = Snappier.")] [SerializeField]
        private float shakeSpeed = 25f;

        [Tooltip("Minimum intensity threshold to start shaking (prevents micro-jitters).")] [SerializeField]
        private float intensityThreshold = 0.01f;

        [Header("References")] [SerializeField]
        private ControllerRumbleListener listener;

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
            if (!listener)
            {
                return;
            }

            // Get the combined rumble intensity from the listener
            float combinedIntensity = Mathf.Max(listener.CurrentCombinedLow, listener.CurrentCombinedHigh);

            // Update info text if assigned
            if (infoText)
            {
                infoText.text =
                    $"Low: {listener.CurrentCombinedLow:F2}\nHigh: {listener.CurrentCombinedHigh:F2}\nIntensity: {combinedIntensity:F2}\n Effects: {listener.ActiveEffects}";
            }

            // Check if we should be rumbling
            bool shouldRumble = combinedIntensity > intensityThreshold;

            if (shouldRumble)
            {
                _isRumbling = true;

                // Update noise timer for random offset generation
                _noiseTimer += Time.deltaTime * shakeSpeed;

                // Generate random offset based on Perlin noise for smoother movement
                float offsetX = (Mathf.PerlinNoise(_noiseTimer, 0f) - 0.5f) * 2f;
                float offsetY = (Mathf.PerlinNoise(0f, _noiseTimer) - 0.5f) * 2f;

                // Scale offset by intensity and max distance
                Vector3 targetOffset = new Vector3(
                    offsetX * combinedIntensity * maxMovementDistance,
                    offsetY * combinedIntensity * maxMovementDistance,
                    0f
                );

                // Apply directly for immediate shake response
                uiRectTransform.localPosition = _originalLocalPosition + targetOffset;
            }
            else if (_isRumbling)
            {
                // We were rumbling but now stopped - smoothly return to original position
                uiRectTransform.localPosition = Vector3.SmoothDamp(
                    uiRectTransform.localPosition,
                    _originalLocalPosition,
                    ref _currentVelocity,
                    1f / shakeSpeed
                );

                // Check if we've reached the original position (within a small threshold)
                if (Vector3.Distance(uiRectTransform.localPosition, _originalLocalPosition) < 0.1f)
                {
                    uiRectTransform.localPosition = _originalLocalPosition;
                    _isRumbling = false;
                    _currentVelocity = Vector3.zero;
                }
            }
            else
            {
                // Not rumbling and already at rest - ensure we're at the exact original position
                uiRectTransform.localPosition = _originalLocalPosition;
            }
        }
    }
}