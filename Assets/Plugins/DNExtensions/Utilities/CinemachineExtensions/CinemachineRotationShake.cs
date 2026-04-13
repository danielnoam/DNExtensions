using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace DNExtensions.Utilities.CinemachineExtensions
{
    /// <summary>
    /// Cinemachine extension that applies rotation-based camera shake, driven by events.
    /// Add this to a CinemachineCamera and call Shake() to trigger.
    /// </summary>
    public class CinemachineRotationShake : CinemachineExtension
    {
        private readonly List<ShakeInstance> _activeShakes = new();

        /// <summary>
        /// Triggers a rotation shake using the provided settings. Multiple shakes stack additively.
        /// </summary>
        public void Shake(RotationShakeSettings settings)
        {
            if (settings == null) return;
            _activeShakes.Add(new ShakeInstance(settings, Time.time));
        }

        /// <summary>
        /// Triggers a rotation shake with default settings, overriding intensity and duration.
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            _activeShakes.Add(new ShakeInstance(new RotationShakeSettings(intensity, duration), Time.time));
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Noise || _activeShakes.Count == 0) return;

            var totalOffset = Vector3.zero;
            float time = Time.time;

            for (int i = _activeShakes.Count - 1; i >= 0; i--)
            {
                var shake = _activeShakes[i];
                float elapsed = time - shake.StartTime;

                if (elapsed >= shake.Settings.duration)
                {
                    _activeShakes.RemoveAt(i);
                    continue;
                }

                float t = elapsed / shake.Settings.duration;
                float decay = shake.Settings.EvaluateDecay(t);

                float noiseTime = elapsed * shake.Settings.frequency;
                float nx = Mathf.PerlinNoise(shake.SeedX + noiseTime, 0f);
                float ny = Mathf.PerlinNoise(shake.SeedY + noiseTime, 0f);
                float nz = Mathf.PerlinNoise(shake.SeedZ + noiseTime, 0f);

                totalOffset += new Vector3(
                    Mathf.Lerp(shake.Settings.pitch.minValue, shake.Settings.pitch.maxValue, nx),
                    Mathf.Lerp(shake.Settings.yaw.minValue, shake.Settings.yaw.maxValue, ny),
                    Mathf.Lerp(shake.Settings.roll.minValue, shake.Settings.roll.maxValue, nz)
                ) * (shake.Settings.intensity * decay);
            }

            state.OrientationCorrection *= Quaternion.Euler(totalOffset);
        }

        private class ShakeInstance
        {
            public readonly RotationShakeSettings Settings;
            public readonly float StartTime;
            public readonly float SeedX;
            public readonly float SeedY;
            public readonly float SeedZ;

            public ShakeInstance(RotationShakeSettings settings, float startTime)
            {
                Settings = settings;
                StartTime = startTime;
                SeedX = Random.Range(0f, 100f);
                SeedY = Random.Range(0f, 100f);
                SeedZ = Random.Range(0f, 100f);
            }
        }
    }

    [System.Serializable]
    public class RotationShakeSettings
    {
        [Min(0.01f)] public float intensity = 1f;
        [Min(0.01f)] public float duration = 0.2f;
        [Min(0.1f)] public float frequency = 25f;
        [MinMaxRange(-15f, 15f)] public RangedFloat pitch = new(-1f, 1f);
        [MinMaxRange(-15f, 15f)] public RangedFloat yaw = new(-1f, 1f);
        [MinMaxRange(-15f, 15f)] public RangedFloat roll = new(0f, 0f);
        public ShakeDecayCurve decayCurve = ShakeDecayCurve.QuadraticOut;
        [ShowIf(nameof(decayCurve), ShakeDecayCurve.Custom)]
        public AnimationCurve customCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        public RotationShakeSettings() { }

        public RotationShakeSettings(float intensity, float duration)
        {
            this.intensity = intensity;
            this.duration = duration;
        }

        /// <summary>
        /// Returns the decay multiplier for the given normalized time (0 = start, 1 = end).
        /// </summary>
        public float EvaluateDecay(float t)
        {
            return decayCurve switch
            {
                ShakeDecayCurve.Linear => 1f - t,
                ShakeDecayCurve.QuadraticOut => (1f - t) * (1f - t),
                ShakeDecayCurve.QuadraticIn => 1f - t * t,
                ShakeDecayCurve.SquareRoot => Mathf.Sqrt(1f - t),
                ShakeDecayCurve.Exponential => Mathf.Exp(-4f * t),
                ShakeDecayCurve.Custom => customCurve.Evaluate(t),
                _ => 1f - t
            };
        }
    }

    public enum ShakeDecayCurve
    {
        Linear,
        QuadraticOut,
        QuadraticIn,
        SquareRoot,
        Exponential,
        Custom
    }
}