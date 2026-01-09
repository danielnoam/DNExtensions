using System;
using UnityEngine;

[Serializable]
public class Spring
{
    [Tooltip("How tightly the spring pulls toward the target. Higher = snappier response, more bouncing.")]
    public float stiffness = 10f;
    [Tooltip("How much the spring resists motion (friction). Higher = settles faster with less bouncing. 1.0 = critically damped (no overshoot).")]
    public float damping = 0.5f;
    [Tooltip("Where the spring wants to settle. Change this to make the spring move to a new position.")]
    public float target;

    private float _value;
    private float _velocity;
    private bool _isLocked;
    
    public float Value => _value;
    public float Velocity => _velocity;
    public bool IsLocked => _isLocked;
    
    public event Action<float> OnValueChanged;
    public event Action<float> OnLocked;
    public event Action<float> OnUnlocked;
    
    public void Update(float deltaTime)
    {
        if (_isLocked) return;
        
        float oldValue = _value;
        
        // Calculate spring force (Hooke's Law)
        float displacement = _value - target;
        float springForce = -stiffness * displacement;
        
        // Apply damping to velocity
        float dampingForce = -damping * _velocity;
        
        // Update velocity and position
        _velocity += (springForce + dampingForce) * deltaTime;
        _value += _velocity * deltaTime;
        
        if (Mathf.Abs(_value - oldValue) > 0.0001f)
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
            _velocity = 0f;
        }
        
        OnLocked?.Invoke(_value);
    }
    
    public void Unlock()
    {
        if (!_isLocked) return;
        
        _isLocked = false;
        OnUnlocked?.Invoke(_value);
    }
    
    public void Reset(float newTarget)
    {
        _value = newTarget;
        _velocity = 0f;
        target = newTarget;
    }
    
    public void Reset()
    {
        _value = target;
        _velocity = 0f;
    }
    
    
    public void SetValue(float newValue)
    {
        _value = newValue;
        _velocity = 0f;
    }
}