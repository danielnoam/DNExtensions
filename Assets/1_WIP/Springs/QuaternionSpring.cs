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
    
    [Header("Limits")]
    [Tooltip("Enable clamping of spring values per axis")]
    public bool useLimits;
    [Tooltip("Minimum allowed Euler angles")]
    public Vector3 minEuler = Vector3.zero;
    [Tooltip("Maximum allowed Euler angles")]
    public Vector3 maxEuler = new Vector3(360f, 360f, 360f);
    
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
    public event Action<Vector3, Vector3> OnLimitHit; // (hitAxis, velocityAtImpact)
    
    public void Update(float deltaTime)
    {
        if (_isLocked) return;
        
        Vector3 oldValue = _valueEuler;
        
        Vector3 displacement = _valueEuler - targetEuler;
        
        // Handle angle wrapping
        displacement.x = NormalizeAngle(displacement.x);
        displacement.y = NormalizeAngle(displacement.y);
        displacement.z = NormalizeAngle(displacement.z);
        
        Vector3 springForce = -stiffness * displacement;
        Vector3 dampingForce = -damping * _velocity;
        
        _velocity += (springForce + dampingForce) * deltaTime;
        _valueEuler += _velocity * deltaTime;
        
        if (useLimits)
        {
            Vector3 hitAxis = Vector3.zero;
            Vector3 impactVelocity = _velocity;
            bool hitLimit = false;
            
            // X axis
            if (_valueEuler.x < minEuler.x)
            {
                _valueEuler.x = minEuler.x;
                hitAxis.x = -1f;
                hitLimit = true;
                _velocity.x = 0f;
            }
            else if (_valueEuler.x > maxEuler.x)
            {
                _valueEuler.x = maxEuler.x;
                hitAxis.x = 1f;
                hitLimit = true;
                _velocity.x = 0f;
            }
            
            // Y axis
            if (_valueEuler.y < minEuler.y)
            {
                _valueEuler.y = minEuler.y;
                hitAxis.y = -1f;
                hitLimit = true;
                _velocity.y = 0f;
            }
            else if (_valueEuler.y > maxEuler.y)
            {
                _valueEuler.y = maxEuler.y;
                hitAxis.y = 1f;
                hitLimit = true;
                _velocity.y = 0f;
            }
            
            // Z axis
            if (_valueEuler.z < minEuler.z)
            {
                _valueEuler.z = minEuler.z;
                hitAxis.z = -1f;
                hitLimit = true;
                _velocity.z = 0f;
            }
            else if (_valueEuler.z > maxEuler.z)
            {
                _valueEuler.z = maxEuler.z;
                hitAxis.z = 1f;
                hitLimit = true;
                _velocity.z = 0f;
            }
            
            if (hitLimit)
            {
                OnLimitHit?.Invoke(hitAxis, impactVelocity);
            }
        }
        
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
    
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}