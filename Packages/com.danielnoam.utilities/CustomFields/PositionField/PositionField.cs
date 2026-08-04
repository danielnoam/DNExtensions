using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Represents a world position sourced from either a <see cref="Transform"/> or a <see cref="Vector3"/>.
    /// When a transform is assigned, its live position is used. Falls back to the vector otherwise.
    /// </summary>
    [Serializable]
    public class PositionField
    {
        [SerializeField] private Transform positionTransform;
        [SerializeField] private Vector3 positionVector;

        public Vector3 Position => positionTransform ? positionTransform.position : positionVector;
        public Transform Transform => positionTransform;

        /// <summary>
        /// Sets the source transform and syncs <see cref="Position"/> to its current position.
        /// Pass <c>null</c> to clear the transform and keep the last synced vector.
        /// </summary>
        public void SetTransform(Transform newTransform)
        {
            positionTransform = newTransform;
            if (newTransform)
            {
                positionVector = newTransform.position;
            }
        }

        public static implicit operator Vector3(PositionField field) => field.Position;
        public static implicit operator PositionField(Transform transform) => new PositionField { positionTransform = transform };
    }
}