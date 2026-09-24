using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    /// <summary>
    /// Automatically returns object after a specified lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DNExtensions/Object Pooling/Auto Return To Pool")]
    public class PoolableAutoReturn : MonoBehaviour, IPoolable
    {
        public float lifeTime;
        [Tooltip("Count down in real time, so the object still returns while the game is paused or slowed. Use for audio and UI effects.")]
        public bool useUnscaledTime;

        private float _remaining;
        private bool _isInitialized;

        private void Update()
        {
            if (!_isInitialized) return;

            _remaining -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (_remaining > 0f) return;

            _isInitialized = false;
            ObjectPooler.ReturnObjectToPool(gameObject);
        }

        /// <summary>
        /// Starts the countdown for this use only. The configured lifeTime is left untouched for the next use.
        /// </summary>
        public void Initialize(float time)
        {
            _remaining = time;
            _isInitialized = true;
        }

        public void OnPoolGet()
        {
            Initialize(lifeTime);
        }

        public void OnPoolReturn()
        {
            _isInitialized = false;
        }

        public void OnPoolRecycle()
        {
            _isInitialized = false;
        }
    }
}
