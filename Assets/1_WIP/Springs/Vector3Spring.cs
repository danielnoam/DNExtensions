using System;
using UnityEngine;

[Serializable]
public class Vector3Spring
{
    [Tooltip("How tightly the spring pulls toward the target. Higher = snappier response, more bouncing.")]
    public float stiffness = 10f;
    [Tooltip("How much the spring resists motion (friction). Higher = settles faster with less bouncing. 1.0 = critically damped (no overshoot).")]
    public float damping = 0.5f;
    [Tooltip("Where the spring wants to settle. Change this to make the spring move to a new position.")]
    public Vector3 target;
    
    private Vector3 _value;
    private Vector3 _velocity;
    private bool _isLocked;
    
    public Vector3 Value => _value;
    public Vector3 Velocity => _velocity;
    public bool IsLocked => _isLocked;
    
    public event Action<Vector3> OnValueChanged;
    public event Action<Vector3> OnLocked;
    public event Action<Vector3> OnUnlocked;
    
    public void Update(float deltaTime)
    {
        if (_isLocked) return;
        
        Vector3 oldValue = _value;
        
        // Calculate spring force (Hooke's Law) - per axis
        Vector3 displacement = _value - target;
        Vector3 springForce = -stiffness * displacement;
        
        // Apply damping to velocity - per axis
        Vector3 dampingForce = -damping * _velocity;
        
        // Update velocity and position
        _velocity += (springForce + dampingForce) * deltaTime;
        _value += _velocity * deltaTime;
        
        if (Vector3.Distance(_value, oldValue) > 0.0001f)
        {
            OnValueChanged?.Invoke(_value);
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
        
        OnLocked?.Invoke(_value);
    }
    
    public void Unlock()
    {
        if (!_isLocked) return;
        
        _isLocked = false;
        OnUnlocked?.Invoke(_value);
    }
    
    public void Reset(Vector3 newTarget)
    {
        _value = newTarget;
        _velocity = Vector3.zero;
        target = newTarget;
    }

    public void Reset()
    {
        _value = target;
        _velocity = Vector3.zero;
    }
    
    public void SetValue(Vector3 newValue)
    {
        _value = newValue;
        _velocity = Vector3.zero;
    }
}