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
        [SerializeField] private bool localPosition = true;
        [SerializeReference, SerializableSelector] private PositionEffect positionEffect;
        [Separator]
        [SerializeField] private bool localRotation = true;
        [SerializeReference, SerializableSelector] private RotationEffect rotationEffect;
        [Separator]
        [SerializeReference, SerializableSelector] private ScaleEffect scaleEffect;

        private void Awake()
        {
            positionEffect?.Initialize(transform, localPosition);
            rotationEffect?.Initialize(transform, localRotation);
            scaleEffect?.Initialize(transform, true);
        }

        private void Update()
        {
            positionEffect?.Tick(transform, localPosition);
            rotationEffect?.Tick(transform, localRotation);
            scaleEffect?.Tick(transform, true);
        }
    }
}