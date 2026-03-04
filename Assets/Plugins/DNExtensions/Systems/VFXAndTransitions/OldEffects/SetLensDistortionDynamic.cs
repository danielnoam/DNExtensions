// using System;
// using DNExtensions.Utilities.RangedValues;
// using PrimeTween;
// using UnityEngine;
// using UnityEngine.Rendering.Universal;
// using Random = UnityEngine.Random;
//
// namespace DNExtensions.Utilities.VFXManager
// {
//     [Serializable]
//     public class SetLensDistortionDynamic : EffectBase
//     {
//         [Tooltip("The duration of the effect, percentage from the sequence duration.")]
//         [SerializeField, MinMaxRange(0f, 1f)] private RangedFloat duration = new RangedFloat(0f, 1f);
//         
//         [Header("Intensity Animation")]
//         [Tooltip("If true, the effect will use the default intensity of the lens distortion. If false, it will use the start value.")]
//         [SerializeField] private bool useDefaultIntensity;
//         [SerializeField, Range(-1f, 1f)] private float startIntensity;
//         [SerializeField, Range(-1f, 1f)] private float endIntensity = 0.5f;
//         [SerializeField] private Ease intensityEase = Ease.Linear;
//         
//         [Header("Advanced Animation")]
//         [SerializeField] private bool useAdvancedAnimation;
//         [Tooltip("Creates a pulse/wave effect by animating to peak intensity and back")]
//         [SerializeField] private bool createPulseEffect;
//         [SerializeField, Range(-1f, 1f)] private float peakIntensity = 0.8f;
//         [SerializeField] private int pulseCount = 1;
//         
//         [Header("Preset Effects")]
//         [Tooltip("When enabled, presets override endIntensity values. Start values, timing, and pulse settings remain manual.")]
//         [SerializeField] private bool usePresets;
//         [SerializeField] private DistortionType distortionType = DistortionType.Custom;
//         
//         public enum DistortionType
//         {
//             Custom,
//             BarrelDistortion,
//             PincushionDistortion,
//             FishEye,
//             Subtle,
//             Extreme,
//             ReturnToDefault
//         }
//
//         private LensDistortion _lensDistortion;
//         private Sequence _sequence;
//         
//         public override void OnPlayEffect(float sequenceDuration)
//         {
//             if (!VFXManager.Instance) return;
//             
//             if (!_lensDistortion)
//             {
//                 _lensDistortion = VFXManager.Instance.LensDistortion;
//                 if (!_lensDistortion)
//                 {
//                     Debug.LogWarning("LensDistortion component not found in VFXManager!");
//                     return;
//                 }
//             }
//             
//             var startDelay = sequenceDuration * duration.minValue;
//             var effectDurationValue = sequenceDuration * duration.maxValue - startDelay;
//             
//             // Apply preset values if using presets
//             if (usePresets)
//             {
//                 ApplyDistortionPreset();
//             }
//             
//             if (_sequence.isAlive) _sequence.Stop();
//             _sequence = Sequence.Create(useUnscaledTime: true);
//             
//             var intensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
//             
//             if (useAdvancedAnimation && createPulseEffect)
//             {
//                 // Create pulse effect: start -> peak -> end (or back to start)
//                 var pulseDuration = effectDurationValue / (pulseCount * 2); // Each pulse has up and down
//                 
//                 for (int i = 0; i < pulseCount; i++)
//                 {
//                     var pulseStartDelay = startDelay + (i * pulseDuration * 2);
//                     
//                     // Pulse up
//                     _sequence.Group(Tween.Custom(i == 0 ? intensity : endIntensity, peakIntensity, pulseDuration, 
//                         onValueChange: value => _lensDistortion.intensity.value = value, 
//                         ease: intensityEase, startDelay: pulseStartDelay));
//                     
//                     // Pulse down
//                     _sequence.Group(Tween.Custom(peakIntensity, endIntensity, pulseDuration, 
//                         onValueChange: value => _lensDistortion.intensity.value = value, 
//                         ease: intensityEase, startDelay: pulseStartDelay + pulseDuration));
//                 }
//             }
//             else
//             {
//                 // Simple start to end animation
//                 _sequence.Group(Tween.Custom(intensity, endIntensity, effectDurationValue, 
//                     onValueChange: value => _lensDistortion.intensity.value = value, 
//                     ease: intensityEase, startDelay: startDelay));
//             }
//         }
//
//         public override void OnResetEffect()
//         {
//             if (!VFXManager.Instance) return;
//             
//             if (_lensDistortion)
//             {
//                 _lensDistortion.intensity.value = VFXManager.Instance.DefaultLensDistortionIntensity;
//                 if (_sequence.isAlive) _sequence.Stop();
//             }
//         }
//         
//         private void ApplyDistortionPreset()
//         {
//             switch (distortionType)
//             {
//                 case DistortionType.BarrelDistortion:
//                     startIntensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
//                     endIntensity = -0.5f; // Negative for barrel distortion
//                     break;
//                 case DistortionType.PincushionDistortion:
//                     startIntensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
//                     endIntensity = 0.5f; // Positive for pincushion distortion
//                     break;
//                 case DistortionType.FishEye:
//                     startIntensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
//                     endIntensity = -0.8f; // Strong barrel for fisheye effect
//                     break;
//                 case DistortionType.Subtle:
//                     startIntensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
//                     endIntensity = Random.Range(-0.2f, 0.2f); // Subtle random distortion
//                     break;
//                 case DistortionType.Extreme:
//                     startIntensity = useDefaultIntensity ? VFXManager.Instance.DefaultLensDistortionIntensity : startIntensity;
//                     endIntensity = Random.Range(-1f, 1f); // Extreme random distortion
//                     break;
//                 case DistortionType.ReturnToDefault:
//                     endIntensity = VFXManager.Instance.DefaultLensDistortionIntensity;
//                     break;
//             }
//         }
//     }
// }