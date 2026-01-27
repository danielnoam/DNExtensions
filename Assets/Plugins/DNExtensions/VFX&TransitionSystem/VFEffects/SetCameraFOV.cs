using System;
using DNExtensions.Utilities.RangedValues;
using PrimeTween;
using UnityEngine;

namespace DNExtensions.Utilities.VFXManager
{
    [Serializable]
    public class SetCameraFOV : VFEffectsEffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
        
        [Header("Field of View")]
        [Tooltip("If true, the effect will use the default FOV of the camera. If false, it will use the start value.")]
        [SerializeField] private bool useDefaultFOV;
        [SerializeField, Range(1f, 179f)] private float startFOV = 60f;
        [SerializeField, Range(1f, 179f)] private float endFOV = 60f;
        [SerializeField] private Ease fovEase = Ease.Linear;
        
        [Header("Preset Effects")]
        [Tooltip("When enabled, presets override endFocusDistance and endAperture values. Start values and timing remain manual")]
        [SerializeField] private bool useFOVPresets;
        [SerializeField] private FOVType fovType = FOVType.Custom;
        
        public enum FOVType
        {
            Custom,
            ZoomIn,
            ZoomOut,
            Telephoto,
            WideAngle,
            ReturnToDefault
        }

        private Camera _camera;
        private Sequence _sequence;
        
        public override void OnPlayEffect(float sequenceDuration)
        {
            if (!VFXManager.Instance) return;
            
            if (!_camera)
            {
                _camera = VFXManager.Instance.MainCamera;
                if (!_camera)
                {
                    Debug.LogWarning("Camera component not found in VFXManager!");
                    return;
                }
            }
            
            var startDelay = sequenceDuration * duration.minValue;
            var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
            
            // Apply preset values if using presets
            if (useFOVPresets)
            {
                ApplyFOVPreset();
            }
            
            var fov = useDefaultFOV ? VFXManager.Instance.DefaultCameraFOV : startFOV;
            
            if (_sequence.isAlive) _sequence.Stop();
            _sequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(fov, endFOV, effectDurationValue, 
                    onValueChange: value => _camera.fieldOfView = value, 
                    ease: fovEase, startDelay: startDelay));
        }

        public override void OnResetEffect()
        {
            if (!VFXManager.Instance) return;
            
            if (_camera)
            {
                _camera.fieldOfView = VFXManager.Instance.DefaultCameraFOV;
                if (_sequence.isAlive) _sequence.Stop();
            }
        }
        
        private void ApplyFOVPreset()
        {
            switch (fovType)
            {
                case FOVType.ZoomIn:
                    startFOV = useDefaultFOV ? VFXManager.Instance.DefaultCameraFOV : startFOV;
                    endFOV = Mathf.Max(startFOV * 0.5f, 10f); // Zoom in by 50%
                    break;
                case FOVType.ZoomOut:
                    startFOV = useDefaultFOV ? VFXManager.Instance.DefaultCameraFOV : startFOV;
                    endFOV = Mathf.Min(startFOV * 1.5f, 120f); // Zoom out by 50%
                    break;
                case FOVType.Telephoto:
                    startFOV = useDefaultFOV ? VFXManager.Instance.DefaultCameraFOV : startFOV;
                    endFOV = 30f; // Telephoto lens effect
                    break;
                case FOVType.WideAngle:
                    startFOV = useDefaultFOV ? VFXManager.Instance.DefaultCameraFOV : startFOV;
                    endFOV = 90f; // Wide angle lens effect
                    break;
                case FOVType.ReturnToDefault:
                    endFOV = VFXManager.Instance.DefaultCameraFOV;
                    break;
            }
        }
    }
}