using UnityEngine;

namespace DNExtensions.ObjectPooling
{
    /// <summary>
    /// Automatically returns pooled objects after a specified lifetime.
    /// Useful for temporary effects, projectiles, or time-based spawned objects.
    /// </summary>
    public class AutoReturnToPool : MonoBehaviour, IPooledObject
    {
        private float _lifeTime;
        private bool _isInitialized;

        private void Update()
        {
            if (!_isInitialized) return;

            _lifeTime -= Time.deltaTime;
            if (_lifeTime <= 0f)
            {
                _isInitialized = false;
                ObjectPooler.ReturnObjectToPool(gameObject);
            }
        }

        /// <summary>
        /// Sets the object's lifetime before automatic return to pool.
        /// </summary>
        /// <param name="lifeTime">Time in seconds before returning to pool</param>
        public void Initialize(float lifeTime)
        {
            _lifeTime = lifeTime;
            _isInitialized = true;
        }

        public void OnPoolGet()
        {

        }

        public void OnPoolReturn()
        {

        }

        /// <summary>
        /// Called when object is forcibly recycled due to pool constraints.
        /// </summary>
        public void OnPoolRecycle()
        {
            _isInitialized = false;
        }
    }
}