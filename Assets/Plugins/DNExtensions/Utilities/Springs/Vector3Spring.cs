using System;
using UnityEngine;

namespace DNExtensions.Utilities.Springs
{
    
    [Serializable]
    public class Vector3Spring
    {
        [Tooltip("How tightly the spring pulls toward the target. Higher = snappier response, more bouncing.")]
        public float stiffness = 10f;

        [Tooltip("How much the spring resists motion (friction). Higher = settles faster with less bouncing. 1.0 = critically damped (no overshoot).")]
        public float damping = 0.5f;
        [Tooltip("Where the spring wants to settle. Change this to make the spring move to a new position.")]
        public Vector3 target;
        
        [Header("Limits")]
        [Tooltip("Enable clamping of spring values")] public bool useLimits;
        [Tooltip("Minimum allowed value per axis")] public Vector3 min = Vector3.zero;
        [Tooltip("Maximum allowed value per axis")] public Vector3 max = Vector3.one;

        private Vector3 _value;
        private Vector3 _velocity;
        private bool _isLocked;

        public Vector3 Value => _value;
        public Vector3 Velocity => _velocity;
        public bool IsLocked => _isLocked;

        public event Action<Vector3> OnValueChanged;
        public event Action<Vector3> OnLocked;
        public event Action<Vector3> OnUnlocked;
        public event Action<Vector3, Vector3> OnLimitHit; // (hitAxis, velocityAtImpact)

        public void Update(float deltaTime)
        {
            if (_isLocked) return;

            Vector3 oldValue = _value;

            Vector3 displacement = _value - target;
            Vector3 springForce = -stiffness * displacement;
            Vector3 dampingForce = -damping * _velocity;

            _velocity += (springForce + dampingForce) * deltaTime;
            _value += _velocity * deltaTime;

            if (useLimits)
            {
                Vector3 hitAxis = Vector3.zero;
                Vector3 impactVelocity = _velocity;
                bool hitLimit = false;

                // X axis
                if (_value.x < min.x)
                {
                    _value.x = min.x;
                    hitAxis.x = -1;
                    hitLimit = true;
                    _velocity.x = 0f;
                }
                else if (_value.x > max.x)
                {
                    _value.x = max.x;
                    hitAxis.x = 1;
                    hitLimit = true;
                    _velocity.x = 0f;
                }

                // Y axis
                if (_value.y < min.y)
                {
                    _value.y = min.y;
                    hitAxis.y = -1;
                    hitLimit = true;
                    _velocity.y = 0f;
                }
                else if (_value.y > max.y)
                {
                    _value.y = max.y;
                    hitAxis.y = 1;
                    hitLimit = true;
                    _velocity.y = 0f;
                }

                // Z axis
                if (_value.z < min.z)
                {
                    _value.z = min.z;
                    hitAxis.z = -1;
                    hitLimit = true;
                    _velocity.z = 0f;
                }
                else if (_value.z > max.z)
                {
                    _value.z = max.z;
                    hitAxis.z = 1;
                    hitLimit = true;
                    _velocity.z = 0f;
                }

                if (hitLimit)
                {
                    OnLimitHit?.Invoke(hitAxis, impactVelocity);
                }
            }

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
}