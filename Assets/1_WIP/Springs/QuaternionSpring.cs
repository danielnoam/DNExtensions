using System;
using UnityEngine;

[Serializable]
public class QuaternionSpring
{
    [Tooltip("How tightly the spring pulls toward the target. Higher = snappier response, more bouncing.")]
    public float stiffness = 10f;
    [Tooltip("How much the spring resists motion (friction). Higher = settles faster with less bouncing. 1.0 = critically damped (no overshoot).")]
    public float damping = 0.5f;
    [Tooltip("Target rotation in Euler angles (degrees). Change this to make the spring rotate to a new orientation.")]
    public Vector3 targetEuler;
    
    private Vector3 _valueEuler;
    private Vector3 _velocity;
    private bool _isLocked;
    
    public Quaternion Value => Quaternion.Euler(_valueEuler);
    public Vector3 ValueEuler => _valueEuler;
    public Vector3 Velocity => _velocity;
    public bool IsLocked => _isLocked;
    
    public Quaternion Target
    {
        get => Quaternion.Euler(targetEuler);
        set => targetEuler = value.eulerAngles;
    }
    
    public event Action<Quaternion> OnValueChanged;
    public event Action<Quaternion> OnLocked;
    public event Action<Quaternion> OnUnlocked;
    
    public void Update(float deltaTime)
    {
        if (_isLocked) return;
        
        Vector3 oldValue = _valueEuler;
        
        // Calculate spring force (Hooke's Law) - per axis in Euler space
        Vector3 displacement = _valueEuler - targetEuler;
        
        // Handle angle wrapping (e.g., 359° to 1° should be 2° difference, not 358°)
        displacement.x = NormalizeAngle(displacement.x);
        displacement.y = NormalizeAngle(displacement.y);
        displacement.z = NormalizeAngle(displacement.z);
        
        Vector3 springForce = -stiffness * displacement;
        
        // Apply damping to velocity - per axis
        Vector3 dampingForce = -damping * _velocity;
        
        // Update velocity and position
        _velocity += (springForce + dampingForce) * deltaTime;
        _valueEuler += _velocity * deltaTime;
        
        if (Vector3.Distance(_valueEuler, oldValue) > 0.0001f)
        {
            OnValueChanged?.Invoke(Value);
        }
    }
    
    public void Lock(bool resetVelocity = true)
    {
        if (_isLocked) return;
        
        _isLocked = true;
        
        if (resetVelocity)
        {
            _velocity = Vector3.zero;
        }
        
        OnLocked?.Invoke(Value);
    }
    
    public void Unlock()
    {
        if (!_isLocked) return;
        
        _isLocked = false;
        OnUnlocked?.Invoke(Value);
    }
    
    public void Reset(Quaternion newTarget)
    {
        _valueEuler = newTarget.eulerAngles;
        _velocity = Vector3.zero;
        targetEuler = newTarget.eulerAngles;
    }

    public void Reset()
    {
        _valueEuler = targetEuler;
        _velocity = Vector3.zero;
    }
    
    public void SetValue(Quaternion newRotation)
    {
        _valueEuler = newRotation.eulerAngles;
        _velocity = Vector3.zero;
    }
    
    // Normalize angle to -180 to 180 range for proper spring behavior
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}