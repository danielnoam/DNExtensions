using System;
using DNExtensions.Utilities.CustomFields;
using DNExtensions.Utilities.SerializableSelector;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Rotates the transform to face a target defined by a PositionField.
    /// </summary>
    [Serializable]
    [SerializableSelectorName("Look At", "Rotation")]
    [SerializableSelectorTooltip("Rotates the transform to face a target defined by a PositionField.")]
    public class LookAtRotationEffect : RotationEffect
    {
        [SerializeField] private PositionField target;
        [SerializeField] private Vector3 upVector = Vector3.up;
        [SerializeField] private float speed = 5f;

        public override void Initialize(Transform transform) { }

        public override void Tick(Transform transform)
        {
            Vector3 direction = target.Position - transform.position;

            if (direction.sqrMagnitude < 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, upVector);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }
}