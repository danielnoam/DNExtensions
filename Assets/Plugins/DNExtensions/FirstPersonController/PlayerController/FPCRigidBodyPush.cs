
using UnityEngine;

namespace DNExtensions.FirstPersonController
{
    [DisallowMultipleComponent]
    public class FPCRigidBodyPush : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enablePush = true;
        [SerializeField, Min(0f)] private float pushPower = 2f;
        

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!enablePush) return;
            
            Rigidbody rb = hit.collider.attachedRigidbody;
            
            if (!rb || rb.isKinematic) return;
            if (hit.moveDirection.y < -0.3f) return;
            
            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            rb.linearVelocity = pushDirection * pushPower;
        }

    }
}