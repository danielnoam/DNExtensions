using System;
using DNExtensions;
using DNExtensions.Button;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[SelectionBase]
public class MovingPlatform : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool active;
    [SerializeField] private PositionField positionTwo;
    [SerializeField] private float waitTimeAtPositions = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private AnimationCurve moveSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 pathStart;
    private float pathLength;
    private float waitTimer;
    private bool movingToPositionTwo = true;
    private Rigidbody rb;
    private Vector3 velocity;

    public Vector3 Velocity => velocity;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        startPosition = transform.position;
        pathStart = startPosition;
        targetPosition = positionTwo.Position;
        pathLength = Vector3.Distance(pathStart, targetPosition);
    }
    
    private void FixedUpdate()
    {
        if (!active)
        {
            velocity = Vector3.zero;
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            velocity = Vector3.zero;
            return;
        }

        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Calculate curve multiplier for this frame
        float distanceTraveled = pathLength - distance;
        float t = Mathf.Clamp01(distanceTraveled / pathLength);
        float speedMultiplier = moveSpeedCurve.Evaluate(t);
        
        // Actual step distance with curve applied
        float actualStep = moveSpeed * speedMultiplier * Time.fixedDeltaTime;
        
        if (distance <= actualStep)
        {
            rb.MovePosition(targetPosition);
            velocity = Vector3.zero;
            waitTimer = waitTimeAtPositions;
            movingToPositionTwo = !movingToPositionTwo;
            
            pathStart = targetPosition;
            targetPosition = movingToPositionTwo ? positionTwo.Position : startPosition;
            pathLength = Vector3.Distance(pathStart, targetPosition);
        }
        else
        {
            velocity = direction * (moveSpeed * speedMultiplier);
            Vector3 newPosition = transform.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
    }

    public void SetActive(bool isActive)
    {
        active = isActive;
    }

    [Button]
    public void ToggleActive() => SetActive(!active);
    
    [Button]
    public void ResetPlatform()
    {
        transform.position = startPosition;
        targetPosition = positionTwo.Position;
        movingToPositionTwo = true;
        waitTimer = 0f;
        velocity = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(start, 0.3f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(positionTwo.Position, 0.3f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, positionTwo.Position);
    }
}