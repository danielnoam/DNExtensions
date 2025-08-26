
using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.ControllerRumbleSystem
{
    [DisallowMultipleComponent]
    public class ControllerRumbleSource : MonoBehaviour
    {
        private readonly List<ControllerRumbleListener> _rumbleListeners = new List<ControllerRumbleListener>();

        private void Awake()
        {
            foreach (var listener in FindObjectsByType<ControllerRumbleListener>(FindObjectsSortMode.None))
            {
                _rumbleListeners.Add(listener);
            }
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
        /// Triggers rumble effect on all connected listeners (Takes custom parameters, frequencies are clamped between 0-1)
        /// </summary>
        public void Rumble(float lowFrequency, float highFrequency, float duration, AnimationCurve lowFreqCurve = null, AnimationCurve highFreqCurve = null)
        {
            var effect = new ControllerRumbleEffect(lowFrequency, highFrequency, duration, lowFreqCurve, highFreqCurve);
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
                controllerRumbleEffectSettings.highFrequencyCurve);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }

        /// <summary>
        /// Triggers rumble effect on all connected listeners (Uses custom curves to fade out the effect)
        /// </summary>
        public void RumbleFadeOut(float lowFreq, float highFreq, float duration)
        {
            var fadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, duration, fadeOutCurve, fadeOutCurve);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }

        
        /// <summary>
        /// Triggers rumble effect on all connected listeners (Uses custom curves to fade in the effect)
        /// </summary>
        public void RumbleFadeIn(float lowFreq, float highFreq, float duration)
        {
            var fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, duration, fadeInCurve, fadeInCurve);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }
        
        
        /// <summary>
        /// Triggers rumble effect on all connected listeners (Uses custom curves to pulse the effect)
        /// </summary>
        public void RumblePulse(float lowFreq, float highFreq, float duration, int pulses = 3)
        {

            var pulseCurve = new AnimationCurve();
            for (var i = 0; i < pulses; i++)
            {
                var time = (float)i / pulses;
                pulseCurve.AddKey(time, 0f);
                pulseCurve.AddKey(time + 0.1f / pulses, 1f);
            }
            
            var effect = new ControllerRumbleEffect(lowFreq, highFreq, duration, pulseCurve, pulseCurve);
            
            foreach (var listener in _rumbleListeners)
            {
                listener?.AddRumbleEffect(effect);
            }
        }
    }
}