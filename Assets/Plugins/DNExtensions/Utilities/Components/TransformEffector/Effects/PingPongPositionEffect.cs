using System;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Moves the transform back and forth between its start position and an end position.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Ping Pong", "Position")]
    [SerializableSelectorTooltip("Moves the transform back and forth between its start position and an end position.")]
    public class PingPongPositionEffect : PositionEffect
    {
        [SerializeField] private bool useLocalSpace = true;
        [SerializeField] private Vector3 endPosition = Vector3.right;
        [SerializeField] private float speed = 2f;


        private Vector3 _startPosition;
        private float _t;
        private bool _movingForward = true;

        public override void Initialize(Transform target)
        {
            _startPosition = useLocalSpace ? target.localPosition : target.position;
            _t = 0f;
            _movingForward = true;
        }

        public override void Tick(Transform target)
        {
            _t += Time.deltaTime * speed * (_movingForward ? 1f : -1f);

            if (_t >= 1f)
            {
                _t = 1f;
                _movingForward = false;
            }
            else if (_t <= 0f)
            {
                _t = 0f;
                _movingForward = true;
            }

            Vector3 newPosition = Vector3.Lerp(_startPosition, endPosition, _t);

            if (useLocalSpace)
            {
                target.localPosition = newPosition;
            }
            else
            {
                target.position = newPosition;
            }
        }
    }
}