using UnityEngine;

namespace DNExtensions
{
    [System.Serializable]
    public class PositionField
    {
        [SerializeField] private Transform positionTransform;
        [SerializeField] private Vector3 positionVector;

        public Vector3 Position => positionTransform ? positionTransform.position : positionVector;

        public Transform Transform => positionTransform;

#if UNITY_EDITOR
        public void SetTransform(Transform newTransform)
        {
            positionTransform = newTransform;
            if (newTransform)
            {
                positionVector = newTransform.position;
            }
        }

        public void SetVector(Vector3 newVector)
        {
            positionVector = newVector;
        }

        public Vector3 GetVector()
        {
            return positionVector;
        }
#endif
    }
}