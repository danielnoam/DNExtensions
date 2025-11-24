using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;


namespace DNExtensions.ControllerRumbleSystem
{
    public class ControllerRumbleListener : MonoBehaviour, IDualShockHaptics
    {
        
        [Header("Settings")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat lowFrequencyRange = new RangedFloat(0, 1f);
        [SerializeField, MinMaxRange(0f,1f)] private RangedFloat highFrequencyRange = new RangedFloat(0, 1f);

        
        private readonly List<ControllerRumbleSource> _rumbleSources = new List<ControllerRumbleSource>();
        private readonly HashSet<ControllerRumbleEffect> _activeRumbleEffects = new HashSet<ControllerRumbleEffect>();
        private Gamepad _gamepad;
        private DualShockGamepad _dualShockGamepad;
        private bool _motorsActive;

        
        public float CurrentCombinedLow { get; private set; }
        public float CurrentCombinedHigh { get; private set; }


        private void OnValidate()
        {
            if (!playerInput)
            {
                if (TryGetComponent(out PlayerInput inputComponent))
                {
                    playerInput = inputComponent;
                }
            };
        }
        

        private void OnEnable()
        {
            if (!playerInput) return;
            
            playerInput.onControlsChanged += OnControlsChanged;
            if (playerInput.currentControlScheme == "Gamepad")
            {
                _gamepad = playerInput.devices[0] as Gamepad;
                _dualShockGamepad = playerInput.devices[0] as DualShockGamepad;
            }
            else
            {
                _gamepad = null;
                _dualShockGamepad = null;
            }
        }

        private void OnDisable()
        {
            if (!playerInput) return;
            
            playerInput.onControlsChanged -= OnControlsChanged;
            ResetHaptics();

        }
        
        
        private void OnControlsChanged(PlayerInput input)
        {
            if (input.currentControlScheme == "Gamepad")
            {
                if (_gamepad != null)
                {
                    ResetHaptics();
                    SetLightBarColor(Color.white);
                }

                _gamepad = playerInput.devices[0] as Gamepad;
                _dualShockGamepad = playerInput.devices[0] as DualShockGamepad;
            }
            else
            {
                _gamepad = null;
                _dualShockGamepad = null;
            }

        }

        private void Update()
        {
            if (_gamepad == null) return;

            _activeRumbleEffects.RemoveWhere(effect =>
            {
                effect.Update(Time.deltaTime);
        
                bool shouldRemove = effect.IsExpired;
                if (effect.SourceReference && effect.SourceReference.Is3DSource)
                {
                    shouldRemove |= !effect.SourceReference.gameObject.activeInHierarchy;
                }

                return shouldRemove;
            });

            if (_activeRumbleEffects.Count == 0)
            {
                if (_motorsActive)
                {
                    SetMotorSpeeds(0f, 0f);
                    _motorsActive = false;
                }
            }
            else
            {
                float combinedLow = 0f;
                float combinedHigh = 0f;

                foreach (var effect in _activeRumbleEffects)
                {
                    float normalizedTime = effect.ElapsedTime / effect.Duration;
                    float lowIntensity = effect.LowFrequency * effect.LowFrequencyCurve.Evaluate(normalizedTime);
                    float highIntensity = effect.HighFrequency * effect.HighFrequencyCurve.Evaluate(normalizedTime);
            
                    if (effect.SourceReference && effect.SourceReference.Is3DSource)
                    {
                        float distanceMultiplier = CalculateDistanceFalloff(effect.SourceReference);
                        lowIntensity *= distanceMultiplier;
                        highIntensity *= distanceMultiplier;
                    }

                    combinedLow = Mathf.Max(combinedLow, lowIntensity);
                    combinedHigh = Mathf.Max(combinedHigh, highIntensity);
                    _motorsActive = true;
                }
                
                CurrentCombinedLow = combinedLow;
                CurrentCombinedHigh = combinedHigh;

                SetMotorSpeeds(CurrentCombinedLow, CurrentCombinedHigh);
            }
        }



        

        #region Rumble Effects ------------------------------------------------------------------------------

        /// <summary>
        /// Adds a rumble effect to the active effects queue for processing
        /// </summary>
        /// <param name="effect">The rumble effect to add</param>
        public void AddRumbleEffect(ControllerRumbleEffect effect)
        {
            _activeRumbleEffects.Add(effect);
        }

        /// <summary>
        /// Clears all active rumble effects and stops controller haptics immediately
        /// </summary>
        public void DisableAllRumbleEffects()
        {
            _activeRumbleEffects.Clear();
            ResetHaptics();
        }
        
        

        #endregion Rumble Effects ------------------------------------------------------------------------------



        #region Rumble Sources ----------------------------------------------------------------------------------


        /// <summary>
        /// Connects a rumble source to this listener, allowing it to receive rumble effects
        /// </summary>
        /// <param name="source">The rumble source to connect</param>
        public void ConnectRumbleSource(ControllerRumbleSource source)
        {
            if (!source || _rumbleSources.Contains(source)) return;

            _rumbleSources.Add(source);
        }

        /// <summary>
        /// Disconnects a rumble source from this listener, preventing it from sending effects
        /// </summary>
        /// <param name="source">The rumble source to disconnect</param>
        public void DisconnectRumbleSource(ControllerRumbleSource source)
        {
            if (!source || !_rumbleSources.Contains(source)) return;

            
            _rumbleSources.Remove(source);
        }
        
        /// <summary>
        /// Calculates the distance falloff multiplier for a 3D rumble source
        /// </summary>
        /// <param name="source">The 3D rumble source</param>
        /// <returns>Distance falloff multiplier (0-1)</returns>
        private float CalculateDistanceFalloff(ControllerRumbleSource source)
        {
            if (!source || !source.gameObject.activeInHierarchy) return 0f;
            
            float distance = Vector3.Distance(transform.position, source.transform.position);
            
            if (distance <= source.MinDistance) return 1f;
            if (distance >= source.MaxDistance) return 0f;
            

            float normalizedDistance = (distance - source.MinDistance) / (source.MaxDistance - source.MinDistance);
            
            return source.DistanceFalloffCurve.Evaluate(normalizedDistance);
        }

        #endregion Rumble Sources ----------------------------------------------------------------------------------



        #region Motor Interface --------------------------------------------------------------------------------------


        /// <summary>
        /// Temporarily pauses all haptic feedback on the controller
        /// </summary>
        public void PauseHaptics()
        {
            _gamepad?.PauseHaptics();
        }

        /// <summary>
        /// Resumes haptic feedback on the controller after being paused
        /// </summary>
        public void ResumeHaptics()
        {
            _gamepad?.ResumeHaptics();
        }

        /// <summary>
        /// Stops all haptic feedback and resets the controller motors to idle state
        /// </summary>
        public void ResetHaptics()
        {
            _gamepad?.ResetHaptics();
        }

        /// <summary>
        /// Sets the motor speeds for low and high frequency rumble motors
        /// </summary>
        /// <param name="lowFrequency">Low frequency motor intensity (0-1, clamped by frequency range)</param>
        /// <param name="highFrequency">High frequency motor intensity (0-1, clamped by frequency range)</param>
        public void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            lowFrequency  = lowFrequencyRange.Clamp(lowFrequency);
            highFrequency = highFrequencyRange.Clamp(highFrequency);
            _gamepad?.SetMotorSpeeds(lowFrequency, highFrequency);
        }

        /// <summary>
        /// Sets the light bar color on DualShock controllers (no effect on other controller types)
        /// </summary>
        /// <param name="color">The color to set for the light bar</param>
        public void SetLightBarColor(Color color)
        {
            _dualShockGamepad?.SetLightBarColor(color);
        }


        #endregion Motor Interface --------------------------------------------------------------------------------------




    }

}