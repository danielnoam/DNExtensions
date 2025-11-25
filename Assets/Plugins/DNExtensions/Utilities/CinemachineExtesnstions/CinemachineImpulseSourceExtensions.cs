using System;
using UnityEngine;
using Unity.Cinemachine;
using DNExtensions;

namespace DNExtensions.CinemachineImpulseSystem
{
    /// <summary>
    /// Extension methods for CinemachineImpulseSource that allow generating impulses 
    /// with custom settings, similar to how ControllerRumbleSource works.
    /// </summary>
    public static class CinemachineImpulseSourceExtensions
    {
        /// <summary>
        /// Generates an impulse using custom impulse settings.
        /// </summary>
        /// <param name="source">The impulse source component</param>
        /// <param name="settings">The impulse settings to use</param>
        public static void GenerateImpulse(this CinemachineImpulseSource source, ImpulseSettings settings)
        {
            if (source == null || settings == null) return;

            // Create a temporary impulse definition with the settings
            var impulseDefinition = new CinemachineImpulseDefinition
            {
                ImpulseChannel = settings.impulseChannel,
                ImpulseType = settings.impulseType,
                ImpulseShape = settings.impulseShape,
                ImpulseDuration = settings.duration,
                DissipationDistance = settings.dissipationDistance,
                DissipationRate = settings.dissipationRate,
                PropagationSpeed = settings.propagationSpeed
            };

       
            // Apply velocity
            Vector3 velocity = settings.velocity;

            // Apply intensity multiplier
            velocity *= settings.intensity;

            // Create the impulse event
            impulseDefinition.CreateEvent(source.transform.position, velocity);
        }
    }

    /// <summary>
    /// Settings for generating camera shake impulses.
    /// </summary>
    [Serializable]
    public class ImpulseSettings
    {
        public int impulseChannel = 1;
        public CinemachineImpulseDefinition.ImpulseTypes impulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        public CinemachineImpulseDefinition.ImpulseShapes impulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        [Min(0.01f)] public float duration = 0.3f;
        [Min(0.1f)] public float intensity = 1f;
        public Vector3 velocity = Vector3.one;

        
        [Header("Spatial (Dissipating/Propagating)")]
        [Min(0f)] public float dissipationDistance = 100f;
        [Range(0f, 1f)] public float dissipationRate = 0.25f;
        [Min(1f)] public float propagationSpeed = 343f;
        
        
        

        
        public ImpulseSettings()
        {
        }

        public ImpulseSettings(
            CinemachineImpulseDefinition.ImpulseShapes shape,
            float duration,
            float intensity,
            Vector3 velocity) {
            this.impulseShape = shape;
            this.duration = duration;
            this.intensity = intensity;
            this.velocity = velocity;
        }
    }
}