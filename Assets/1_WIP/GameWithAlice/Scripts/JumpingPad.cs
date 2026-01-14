using DNExtensions;
using UnityEngine;

public class JumpingPad : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float jumpForce = 25f;
    [SerializeField] private float cooldownTime = 1f;
    [SerializeField, ReadOnly] private float cooldownTimer;

    
    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (cooldownTimer > 0f) return;
        
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.ForceJump(jumpForce);
            cooldownTimer = cooldownTime;
        }
    }
}
