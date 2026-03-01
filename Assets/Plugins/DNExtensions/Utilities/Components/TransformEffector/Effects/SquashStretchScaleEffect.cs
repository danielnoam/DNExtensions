using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Scales the primary axis up and down while compensating the other axes to preserve volume.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Squash Stretch", "Scale")]
    [SerializableSelectorTooltip("Scales the primary axis up and down while compensating the other axes to preserve volume.")]
    public class SquashStretchScaleEffect : ScaleEffect
    {
        public enum PrimaryAxis { X, Y, Z }

        [SerializeField] private PrimaryAxis primaryAxis = PrimaryAxis.Y;
        [SerializeField] private float stretchAmount = 0.3f;
        [SerializeField] private float stretchSpeed = 1f;
        [SerializeField] private AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _initialScale = Vector3.one;
        private float _time;

        public override void Initialize(Transform target)
        {
            _initialScale = target.localScale;
            _time = Random.value * Mathf.PI * 2f;
        }

        public override void Tick(Transform target)
        {
            _time += stretchSpeed * Time.deltaTime;

            float normalizedCycle = (Mathf.Sin(_time) + 1f) * 0.5f;
            float stretch = stretchCurve.Evaluate(normalizedCycle) * stretchAmount;
            
            float primary = 1f + stretch;
            float compensated = 1f / Mathf.Sqrt(primary);

            Vector3 scale = primaryAxis switch
            {
                PrimaryAxis.X => new Vector3(primary, compensated, compensated),
                PrimaryAxis.Y => new Vector3(compensated, primary, compensated),
                PrimaryAxis.Z => new Vector3(compensated, compensated, primary),
                _ => Vector3.one
            };

            target.localScale = Vector3.Scale(_initialScale, scale);
        }
    }
}