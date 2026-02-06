using System;
using UnityEngine;

namespace DNExtensions.Utilities.Springs
{
    [Serializable]
    public class ColorSpring
    {
        [Tooltip("How tightly the spring pulls toward the target. Higher = snappier response, more bouncing.")]
        public float stiffness = 10f;
        [Tooltip("How much the spring resists motion (friction). Higher = settles faster with less bouncing. 1.0 = critically damped (no overshoot).")]
        public float damping = 0.5f;
        [Tooltip("Where the spring wants to settle. Change this to make the spring move to a new color.")]
        public Color target = Color.white;

        [Header("Limits")]
        [Tooltip("Enable clamping of spring values")]
        public bool useLimits;
        [Tooltip("Minimum allowed value per channel")]
        public Color min = new Color(0f, 0f, 0f, 0f);
        [Tooltip("Maximum allowed value per channel")]
        public Color max = new Color(1f, 1f, 1f, 1f);

        private Color _value;
        private Color _velocity;
        private bool _isLocked;

        public Color Value => _value;
        public Color Velocity => _velocity;
        public bool IsLocked => _isLocked;

        public event Action<Color> OnValueChanged;
        public event Action<Color> OnLocked;
        public event Action<Color> OnUnlocked;

        public void Update(float deltaTime)
        {
            if (_isLocked) return;

            Color oldValue = _value;

            float dr = _value.r - target.r;
            float dg = _value.g - target.g;
            float db = _value.b - target.b;
            float da = _value.a - target.a;

            _velocity.r += (-stiffness * dr - damping * _velocity.r) * deltaTime;
            _velocity.g += (-stiffness * dg - damping * _velocity.g) * deltaTime;
            _velocity.b += (-stiffness * db - damping * _velocity.b) * deltaTime;
            _velocity.a += (-stiffness * da - damping * _velocity.a) * deltaTime;

            _value.r += _velocity.r * deltaTime;
            _value.g += _velocity.g * deltaTime;
            _value.b += _velocity.b * deltaTime;
            _value.a += _velocity.a * deltaTime;

            if (useLimits)
            {
                _value.r = Mathf.Clamp(_value.r, min.r, max.r);
                _value.g = Mathf.Clamp(_value.g, min.g, max.g);
                _value.b = Mathf.Clamp(_value.b, min.b, max.b);
                _value.a = Mathf.Clamp(_value.a, min.a, max.a);

                if (_value.r <= min.r || _value.r >= max.r) _velocity.r = 0f;
                if (_value.g <= min.g || _value.g >= max.g) _velocity.g = 0f;
                if (_value.b <= min.b || _value.b >= max.b) _velocity.b = 0f;
                if (_value.a <= min.a || _value.a >= max.a) _velocity.a = 0f;
            }

            if (ColorDistance(oldValue, _value) > 0.0001f)
            {
                OnValueChanged?.Invoke(_value);
            }
        }

        public void Lock(bool resetVelocity = true)
        {
            if (_isLocked) return;
            _isLocked = true;
            if (resetVelocity) _velocity = new Color(0f, 0f, 0f, 0f);
            OnLocked?.Invoke(_value);
        }

        public void Unlock()
        {
            if (!_isLocked) return;
            _isLocked = false;
            OnUnlocked?.Invoke(_value);
        }

        public void Reset(Color newTarget)
        {
            _value = newTarget;
            _velocity = new Color(0f, 0f, 0f, 0f);
            target = newTarget;
        }

        public void Reset()
        {
            _value = target;
            _velocity = new Color(0f, 0f, 0f, 0f);
        }

        public void SetValue(Color newValue)
        {
            _value = newValue;
            _velocity = new Color(0f, 0f, 0f, 0f);
        }

        private static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            float da = a.a - b.a;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db + da * da);
        }
    }
}