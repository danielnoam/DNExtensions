using System;
using System.Collections.Generic;
using DNExtensions.Button;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.ControllerRumbleSystem
{
    public class ControllerRumbleSource : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Enable distance-based rumble falloff for 3D spatial rumble effects")]
        [SerializeField] private bool is3DSource;
        [Tooltip("Distance at which rumble is at full intensity (closer = full strength)")]
        [SerializeField, EnableIf("is3DSource"), Min(0f)] private float maxRumbleDistance = 1f;
        [Tooltip("Distance at which rumble completely stops (further = no rumble)")]
        [SerializeField, EnableIf("is3DSource"), Min(0f)]  private float minRumbleDistance = 10f;
        [Tooltip("Falloff curve from full intensity (at 0) to no rumble (at 1)")]
        [SerializeField, EnableIf("is3DSource")] private AnimationCurve distanceFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        private readonly List<ControllerRumbleListener> _rumbleListeners = new List<ControllerRumbleListener>();

        public bool Is3DSource => is3DSource;
        public float MinDistance => maxRumbleDistance;
        public float MaxDistance => minRumbleDistance;
        public AnimationCurve DistanceFalloffCurve => distanceFalloffCurve;
        
        
        private void OnValidate()
        {
            // Ensure full intensity distance is always less than no rumble distance
            if (maxRumbleDistance >= minRumbleDistance)
            {
                minRumbleDistance = maxRumbleDistance + 1f;
                Debug.LogWarning($"[{gameObject.name}] No Rumble Distance must be greater than Full Intensity Distance. Adjusted to {minRumbleDistance}.");
            }
        }

        private void Awake()
        {
            FindListeners();
        }

        private void OnEnable()
        {
            foreach (var listener in _rumbleListeners)
            {
                listener?.ConnectRumbleSource(this);
            }
        }

        private void OnDisable()
        {
            foreach (var listener in _rumbleListeners)
            {
                listener?.DisconnectRumbleSource(this);
            }
        }
        
        /// <summary>
        /// Finds and connects to all ControllerRumbleListeners in the scene
        /// </summary>
        public void FindListeners()
        {
            foreach (var listener in FindObjectsByType<ControllerRumbleListener>(FindObjectsSortMode.None))
            {
                _rumbleListeners.Add(listener);
            }
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.ConnectRumbleSource(this);
            }
        }
        
        /// <summary>
        /// Removes all connected listeners
        /// </summary>
        public void RemoveListeners() {
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.DisconnectRumbleSource(this);
            }
            
            _rumbleListeners.Clear();
        }
        

        /// <summary>
        /// Triggers rumble effect on all connected listeners (Takes custom parameters, frequencies are clamped between 0-1)
        /// </summary>
        [Button]
        public void Rumble(float lowFrequency = 0.2f, float highFrequency = 0.2f, float duration = 0.2f, AnimationCurve lowFreqCurve = null, AnimationCurve highFreqCurve = null)
        {
            var effect = new ControllerRumbleEffect(lowFrequency, highFrequency, duration, lowFreqCurve, highFreqCurve, is3DSource ? this : null);
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }

        /// <summary>
        /// Triggers rumble effect on all connected listeners (Takes vibration effect settings)
        /// </summary>
        public void Rumble(ControllerRumbleEffectSettings controllerRumbleEffectSettings)
        {
            var effect = new ControllerRumbleEffect(
                controllerRumbleEffectSettings.lowFrequency, 
                controllerRumbleEffectSettings.highFrequency, 
                controllerRumbleEffectSettings.duration, 
                controllerRumbleEffectSettings.lowFrequencyCurve,
                controllerRumbleEffectSettings.highFrequencyCurve,
                is3DSource ? this : null);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }

        /// <summary>
        /// Triggers rumble effect on all connected listeners (Uses custom curves to fade out the effect)
        /// </summary>
        [Button]
        public void RumbleFadeOut(float lowFreq = 0.2f, float highFreq = 0.2f, float duration = 0.2f)
        {
            var fadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, duration, fadeOutCurve, fadeOutCurve, is3DSource ? this : null);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }

        
        /// <summary>
        /// Triggers rumble effect on all connected listeners (Uses custom curves to fade in the effect)
        /// </summary>
        [Button]
        public void RumbleFadeIn(float lowFreq = 0.2f, float highFreq = 0.2f, float duration = 0.2f)
        {
            var fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, duration, fadeInCurve, fadeInCurve, is3DSource ? this : null);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }
        
        
        /// <summary>
        /// Triggers rumble effect on all connected listeners (Uses custom curves to pulse the effect)
        /// </summary>
        [Button]
        public void RumblePulse(float lowFreq = 0.2f, float highFreq = 0.2f, float duration = 0.2f, int pulses = 3)
        {

            var pulseCurve = new AnimationCurve();
            for (var i = 0; i < pulses; i++)
            {
                var time = (float)i / pulses;
                pulseCurve.AddKey(time, 0f);
                pulseCurve.AddKey(time + 0.1f / pulses, 1f);
            }
            
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, duration, pulseCurve, pulseCurve, is3DSource ? this : null);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }
        
        
        /// <summary>
        /// Triggers a continuous rumble effect that persists until the source is disabled or destroyed
        /// </summary>
        [Button]
        public void StartContinuousRumble(float lowFreq = 0.2f, float highFreq = 0.2f)
        {
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, this);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }
        
        
        /// <summary>
        /// Stops all continuous rumble effects originating from this source
        /// </summary>
        [Button]
        public void StopContinuousRumble()
        {
            foreach (var listener in _rumbleListeners)
            {
                listener?.RemoveContinuousEffectsFromSource(this);
            }
        }
        
    }
}