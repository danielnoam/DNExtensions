using DNExtensions.Utilities.Button;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [AddComponentMenu("DNExtensions/Align To Terrain")]
    public class AlignToTerrain : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool setPosition = true;
        [SerializeField, ShowIf(nameof(setPosition))] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private bool setRotation = true;

        [Button(ButtonPlayMode.OnlyWhenNotPlaying)]
        private void Align()
        {
            if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 50f)) return;

            if (setPosition)
            {
                transform.position = hit.point.Add(positionOffset);
            }

            if (setRotation)
            {
                Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                if (projectedForward.sqrMagnitude < 0.001f) projectedForward = Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized;
                transform.rotation = Quaternion.LookRotation(projectedForward, hit.normal);
            }
        }
    }
}
