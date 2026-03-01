using System;
using UnityEngine;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.SerializableSelector;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Orbits the transform around a point defined by a PositionField.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Orbit", "Position")]
    [SerializableSelectorTooltip("Orbits the transform around a point defined by a PositionField.")]
    public class OrbitPositionEffect : PositionEffect
    {
        [SerializeField] private PositionField center;
        [SerializeField] private float speed = 45f;
        [SerializeField] private Vector3 axis = Vector3.up;

        private float _currentAngle;
        private float _radius;

        public override void Initialize(Transform target)
        {
            Vector3 offset = target.position - center.Position;
            _radius = new Vector3(offset.x, offset.y, offset.z).magnitude;
            _currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        }

        public override void Tick(Transform target)
        {
            _currentAngle += speed * Time.deltaTime;

            Quaternion rotation = Quaternion.AngleAxis(_currentAngle, axis.normalized);
            Vector3 offset = rotation * (Vector3.right * _radius);
            target.position = center.Position + offset;
        }
    }
}