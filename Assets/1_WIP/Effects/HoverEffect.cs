using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class HoverEffect : MonoBehaviour
{


    [Header("Hover Settings")]
    [SerializeField] private bool enableHover = false;
    [SerializeField] private float hoverSpeed = 0f;
    [SerializeField] private float hoverAmount = 0f;
    [SerializeField] private Vector3 hoverDirection;
    private Vector3 initialPosition;
    private float hoverTime;

    private void Start()
    {
        initialPosition = transform.localPosition;
        hoverTime = Random.value * Mathf.PI * 2;

    }

    private void FixedUpdate()
    {
        Hover();
        
    }
    
    

    private void Hover()
    {
        if (!enableHover) return;

        hoverTime += hoverSpeed * Time.fixedDeltaTime;

        float hoverOffset = Mathf.Sin(hoverTime) * hoverAmount;
        Vector3 normalizedHoverDir = hoverDirection.normalized;
        Vector3 newPosition = initialPosition + normalizedHoverDir * hoverOffset;
        transform.localPosition = newPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!enableHover) return;
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(initialPosition, hoverDirection * hoverAmount);
            Gizmos.DrawRay(initialPosition, -hoverDirection * hoverAmount);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, hoverDirection * hoverAmount);
            Gizmos.DrawRay(transform.position, -hoverDirection * hoverAmount);
        }
        
    }
#endif
}