using DNExtensions.Button;
using UnityEngine;

public class SpringExamples : MonoBehaviour
{
    [Header("Spring Parameters")]
    [SerializeField] private float stiffness = 100f;
    [SerializeField] private float dampingCoefficient = 10f;
    
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Spring Objects")]
    [SerializeField] private Transform undampedSpring;
    [SerializeField] private Transform underdampedSpring;
    [SerializeField] private Transform criticallyDampedSpring;
    [SerializeField] private Transform overdampedSpring;
    
    // Store positions separately
    private Vector3 undampedPosition;
    private Vector3 undampedVelocity;
    
    private Vector3 underdampedPosition;
    private Vector3 underdampedVelocity;
    
    private Vector3 criticalPosition;
    private Vector3 criticalVelocity;
    
    private Vector3 overPosition;
    private Vector3 overVelocity;
    
    private void Start()
    {
        // Initialize positions
        undampedPosition = undampedSpring.position;
        underdampedPosition = underdampedSpring.position;
        criticalPosition = criticallyDampedSpring.position;
        overPosition = overdampedSpring.position;
    }
    
    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        
        UpdateUndampedSpring(ref undampedPosition, ref undampedVelocity, dt);
        undampedSpring.position = undampedPosition;
        
        UpdateUnderdampedSpring(ref underdampedPosition, ref underdampedVelocity, dt);
        underdampedSpring.position = underdampedPosition;
        
        UpdateCriticallyDampedSpring(ref criticalPosition, ref criticalVelocity, dt);
        criticallyDampedSpring.position = criticalPosition;
        
        UpdateOverdampedSpring(ref overPosition, ref overVelocity, dt);
        overdampedSpring.position = overPosition;
    }
    
    private void UpdateUndampedSpring(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        Vector3 displacement = position - target.position;
        Vector3 springForce = -stiffness * displacement;
        Vector3 acceleration = springForce;
        
        velocity += acceleration * dt;
        position += velocity * dt;
    }
    
    private void UpdateUnderdampedSpring(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        Vector3 displacement = position - target.position;
        Vector3 springForce = -stiffness * displacement;
        Vector3 dampingForce = -dampingCoefficient * velocity;
        Vector3 acceleration = springForce + dampingForce;
        
        velocity += acceleration * dt;
        position += velocity * dt;
    }
    
    private void UpdateCriticallyDampedSpring(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        float criticalDamping = 2f * Mathf.Sqrt(stiffness);
        
        Vector3 displacement = position - target.position;
        Vector3 springForce = -stiffness * displacement;
        Vector3 dampingForce = -criticalDamping * velocity;
        Vector3 acceleration = springForce + dampingForce;
        
        velocity += acceleration * dt;
        position += velocity * dt;
    }
    
    private void UpdateOverdampedSpring(ref Vector3 position, ref Vector3 velocity, float dt)
    {
        float criticalDamping = 2f * Mathf.Sqrt(stiffness);
        float overdamping = criticalDamping * 3f;
        
        Vector3 displacement = position - target.position;
        Vector3 springForce = -stiffness * displacement;
        Vector3 dampingForce = -overdamping * velocity;
        Vector3 acceleration = springForce + dampingForce;
        
        velocity += acceleration * dt;
        position += velocity * dt;
    }
    
    [Button]
    public void ApplyImpulse(Vector3 impulse)
    {
        undampedVelocity += impulse;
        underdampedVelocity += impulse;
        criticalVelocity += impulse;
        overVelocity += impulse;
    }
}