// using System;
// using DNExtensions.Utilities.RangedValues;
// using PrimeTween;
// using UnityEngine;
// using UnityEngine.Rendering.Universal;
//
// namespace DNExtensions.Utilities.VFXManager
// {
//     [Serializable]
//     public class SetDepthOfField : EffectBase
//     {
//         [Tooltip("The duration of the effect, percentage from the sequence duration.")]
//         [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
//         
//         [Header("Focus Distance")]
//         [Tooltip("If true, the effect will use the default focus distance. If false, it will use the start value.")]
//         [SerializeField] private bool useDefaultFocusDistance;
//         [SerializeField, Min(0.1f)] private float startFocusDistance = 10f;
//         [SerializeField, Min(0.1f)] private float endFocusDistance = 10f;
//         [SerializeField] private Ease focusDistanceEase = Ease.Linear;
//         
//         [Header("Aperture (Blur Amount)")]
//         [SerializeField] private bool animateAperture;
//         [Tooltip("If true, the effect will use the default aperture. If false, it will use the start value.")]
//         [SerializeField] private bool useDefaultAperture;
//         [SerializeField, Range(0.1f, 32f)] private float startAperture = 5.6f;
//         [SerializeField, Range(0.1f, 32f)] private float endAperture = 1.4f;
//         [SerializeField] private Ease apertureEase = Ease.Linear;
//         
//         [Header("Focal Length")]
//         [SerializeField] private bool animateFocalLength;
//         [Tooltip("If true, the effect will use the default focal length. If false, it will use the start value.")]
//         [SerializeField] private bool useDefaultFocalLength;
//         [SerializeField, Range(1f, 300f)] private float startFocalLength = 50f;
//         [SerializeField, Range(1f, 300f)] private float endFocalLength = 50f;
//         [SerializeField] private Ease focalLengthEase = Ease.Linear;
//         
//         [Header("Preset Effects")]
//         [Tooltip("When enabled, presets override endFOV values. Start values and timing remain manual.")]
//         [SerializeField] private bool useFocusPresets;
//         [SerializeField] private FocusType focusType = FocusType.Custom;
//         
//         public enum FocusType
//         {
//             Custom,
//             ShallowFocus,
//             DeepFocus,
//             BackgroundBlur,
//             ForegroundBlur,
//             ReturnToDefault
//         }
//
//         private DepthOfField _depthOfField;
//         private Sequence _sequence;
//         
//         public override void OnPlayEffect(float sequenceDuration)
//         {
//             if (!VFXManager.Instance) return;
//             
//             if (!_depthOfField)
//             {
//                 _depthOfField = VFXManager.Instance.DepthOfField;
//                 if (!_depthOfField)
//                 {
//                     Debug.LogWarning("DepthOfField component not found in VFXManager!");
//                     return;
//                 }
//             }
//             
//             var startDelay = sequenceDuration * duration.minValue;
//             var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
//             
//             if (_sequence.isAlive) _sequence.Stop();
//             _sequence = Sequence.Create(useUnscaledTime: true);
//             
//             // Apply preset values if using presets
//             if (useFocusPresets)
//             {
//                 ApplyFocusPreset();
//             }
//             
//             // Focus Distance animation
//             var focusDistance = useDefaultFocusDistance ? VFXManager.Instance.DefaultDepthOfFieldFocusDistance : startFocusDistance;
//             _sequence.Group(Tween.Custom(focusDistance, endFocusDistance, effectDurationValue, 
//                 onValueChange: value => _depthOfField.focusDistance.value = value, 
//                 ease: focusDistanceEase, startDelay: startDelay));
//             
//             // Aperture animation
//             if (animateAperture)
//             {
//                 var aperture = useDefaultAperture ? VFXManager.Instance.DefaultDepthOfFieldAperture : startAperture;
//                 _sequence.Group(Tween.Custom(aperture, endAperture, effectDurationValue, 
//                     onValueChange: value => _depthOfField.aperture.value = value, 
//                     ease: apertureEase, startDelay: startDelay));
//             }
//             
//             // Focal Length animation
//             if (animateFocalLength)
//             {
//                 var focalLength = useDefaultFocalLength ? VFXManager.Instance.DefaultDepthOfFieldFocalLength : startFocalLength;
//                 _sequence.Group(Tween.Custom(focalLength, endFocalLength, effectDurationValue, 
//                     onValueChange: value => _depthOfField.focalLength.value = value, 
//                     ease: focalLengthEase, startDelay: startDelay));
//             }
//         }
//
//         public override void OnResetEffect()
//         {
//             if (!VFXManager.Instance) return;
//             
//             if (_depthOfField)
//             {
//                 _depthOfField.focusDistance.value = VFXManager.Instance.DefaultDepthOfFieldFocusDistance;
//                 _depthOfField.aperture.value = VFXManager.Instance.DefaultDepthOfFieldAperture;
//                 _depthOfField.focalLength.value = VFXManager.Instance.DefaultDepthOfFieldFocalLength;
//                 if (_sequence.isAlive) _sequence.Stop();
//             }
//         }
//         
//         private void ApplyFocusPreset()
//         {
//             switch (focusType)
//             {
//                 case FocusType.ShallowFocus:
//                     startFocusDistance = useDefaultFocusDistance ? VFXManager.Instance.DefaultDepthOfFieldFocusDistance : startFocusDistance;
//                     endFocusDistance = 3f; // Close focus
//                     endAperture = 1.4f; // Wide aperture for shallow depth
//                     break;
//                 case FocusType.DeepFocus:
//                     startFocusDistance = useDefaultFocusDistance ? VFXManager.Instance.DefaultDepthOfFieldFocusDistance : startFocusDistance;
//                     endFocusDistance = 50f; // Far focus
//                     endAperture = 11f; // Narrow aperture for deep focus
//                     break;
//                 case FocusType.BackgroundBlur:
//                     endFocusDistance = 5f; // Focus on foreground
//                     endAperture = 2.8f; // Medium aperture
//                     break;
//                 case FocusType.ForegroundBlur:
//                     endFocusDistance = 25f; // Focus on background
//                     endAperture = 4f; // Medium aperture
//                     break;
//                 case FocusType.ReturnToDefault:
//                     endFocusDistance = VFXManager.Instance.DefaultDepthOfFieldFocusDistance;
//                     endAperture = VFXManager.Instance.DefaultDepthOfFieldAperture;
//                     endFocalLength = VFXManager.Instance.DefaultDepthOfFieldFocalLength;
//                     break;
//             }
//         }
//     }
// }