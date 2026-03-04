using UnityEngine;
using DNExtensions.Utilities.SerializableSelector;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Applies a position, rotation, and scale effect to this transform.
    /// Each effect slot is independently selectable via the inspector dropdown.
    /// </summary>
    [AddComponentMenu("DNExtensions/Transform Effector", -1000)]
    [DisallowMultipleComponent]
    public class TransformEffector : MonoBehaviour
    {
        [Space(10)]
        [SerializeReference, SerializableSelector] private PositionEffect positionEffect;
        [Separator]
        [SerializeReference, SerializableSelector] private RotationEffect rotationEffect;
        [Separator]
        [SerializeReference, SerializableSelector] private ScaleEffect scaleEffect;

        private void Awake()
        {
            positionEffect?.Initialize(transform);
            rotationEffect?.Initialize(transform);
            scaleEffect?.Initialize(transform);
        }

        private void Update()
        {
            positionEffect?.Tick(transform);
            rotationEffect?.Tick(transform);
            scaleEffect?.Tick(transform);
        }
    }
}