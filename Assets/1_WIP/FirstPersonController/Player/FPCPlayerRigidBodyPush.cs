
using System;
using UnityEngine;

[RequireComponent(typeof(FpcPlayerMovement))]
[RequireComponent(typeof(CharacterController))]
public class FPCPlayerRigidBodyPush : MonoBehaviour
{

    [SerializeField, Min(0f)] private float pushPower = 2f;
    

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        
        if (!rb || rb.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;
        
        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        rb.linearVelocity = pushDirection * pushPower;
    }

}