using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Oscillates the transform position along a direction using a sine wave.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Hover", "Position")]
    [SerializableSelectorTooltip("Oscillates the transform along a direction using a sine wave.")]
    public class HoverPositionEffect : PositionEffect
    {
        [SerializeField] private float hoverSpeed = 1f;
        [SerializeField] private float hoverAmount = 0.5f;
        [SerializeField] private Vector3 hoverDirection = Vector3.up;

        private Vector3 _startPosition;
        private float _hoverTime;

        public override void Initialize(Transform target)
        {
            _startPosition = target.localPosition;
            _hoverTime = Random.value * Mathf.PI * 2f;
        }

        public override void Tick(Transform target)
        {
            _hoverTime += hoverSpeed * Time.deltaTime;
            float offset = Mathf.Sin(_hoverTime) * hoverAmount;
            target.localPosition = _startPosition + hoverDirection.normalized * offset;
        }
    }
}