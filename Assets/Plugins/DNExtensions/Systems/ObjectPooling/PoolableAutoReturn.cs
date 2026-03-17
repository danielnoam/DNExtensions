using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    /// <summary>
    /// Automatically returns object after a specified lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DNExtensions/ObjectPooling/Auto Return To Pool")]
    public class PoolableAutoReturn : MonoBehaviour, IPoolable
    {
        public float lifeTime;
        private bool _isInitialized;

        private void Update()
        {
            if (!_isInitialized) return;

            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                lifeTime = 0f;
                _isInitialized = false;
                ObjectPooler.ReturnObjectToPool(gameObject);
            }
        }
        
        public void Initialize(float time)
        {
            lifeTime = time;
            _isInitialized = true;
        }

        public void OnPoolGet()
        {
            Initialize(lifeTime);
        }

        public void OnPoolReturn()
        {
        }

        public void OnPoolRecycle()
        {
            _isInitialized = false;
        }
    }
}