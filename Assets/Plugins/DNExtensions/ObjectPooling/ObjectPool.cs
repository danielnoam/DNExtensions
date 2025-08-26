using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.ObjectPooling
{
    
    [Serializable]
    public class ObjectPool
    {
        [Header("Pool Settings")] 
        public string poolName = "New Pool";
        [Min(1)] public int maxPoolSize = 50;
        public GameObject prefab;

        [Tooltip("Adds the pool to don't destroy list")]
        public bool dontDestroyOnLoad = true;

        [Tooltip("If max pool size reached and there are no objects in inactive pool, recycle the last active object (this is not recommended, objects are notified that they have been recycled but it is not performant")]
        public bool recycleActiveObjects;

        [Header("Pre Warm")] 
        [Tooltip("Pre populate the pool")]
        public bool preWarmPool = true;

        public int preWarmPoolSize = 5;

        [Header("Debug")]
        [SerializeField] private int poolSize;
        [SerializeField] private int activePoolCount;
        [SerializeField] private int inactivePoolCount;

        private readonly List<GameObject> _activePool = new List<GameObject>();
        private readonly Queue<GameObject> _inactivePool = new Queue<GameObject>();
        private readonly HashSet<GameObject> _activePoolSet = new HashSet<GameObject>();
        private readonly HashSet<GameObject> _objectsBeingReturned = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, IPooledObject> _pooledObjects = new Dictionary<GameObject, IPooledObject>();

        private Transform _poolHolder;
        private bool _isInitialized;

        public GameObject GetObjectFromPool(Vector3 position = default, Quaternion rotation = default)
        {
            if (!_isInitialized)
            {
                Debug.LogError($"[{poolName}] Pool not initialized!");
                return null;
            }

            // Try to get from inactive pool first
            if (_inactivePool.Count > 0)
            {
                var obj = _inactivePool.Dequeue();

                _activePool.Add(obj);
                _activePoolSet.Add(obj);

                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);

                if (_pooledObjects.TryGetValue(obj, out var pooledObject))
                {
                    pooledObject.OnPoolGet();
                }

                UpdateDebugFields();
                return obj;
            }
            // Create a new object if under max pool size
            else if ((_activePool.Count + _inactivePool.Count) < maxPoolSize)
            {
                var obj = InstantiatePoolObject();

                _activePool.Add(obj);
                _activePoolSet.Add(obj);

                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);

                if (_pooledObjects.TryGetValue(obj, out var pooledObject))
                {
                    pooledObject.OnPoolGet();
                }

                UpdateDebugFields();
                return obj;
            }
            // Recycle the oldest active object if allowed
            else if (recycleActiveObjects && _activePool.Count > 0)
            {
                var obj = _activePool[0];

                if (_pooledObjects.TryGetValue(obj, out var pooledObject))
                {
                    pooledObject.OnPoolRecycle();
                }

                obj.SetActive(false);
                _activePool.RemoveAt(0);
                _activePool.Add(obj);

                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);

                pooledObject?.OnPoolGet();
                UpdateDebugFields();
                return obj;
            }

            return null;
        }

        public void ReturnObjectToPool(GameObject obj)
        {
            if (!_isInitialized || !obj) return;

            if (_objectsBeingReturned.Contains(obj))
            {
                Debug.LogWarning($"[{poolName}] Object {obj.name} is already being returned to this pool");
                return;
            }

            if (!_activePoolSet.Contains(obj))
            {
                Debug.LogWarning($"[{poolName}] Object {obj.name} not found in active pool");
                return;
            }

            _objectsBeingReturned.Add(obj);

            try
            {
                obj.SetActive(false);

                _activePool.Remove(obj);
                _activePoolSet.Remove(obj);

                _inactivePool.Enqueue(obj);

                if (_pooledObjects.TryGetValue(obj, out var pooledObject))
                {
                    pooledObject.OnPoolReturn();
                }
            }
            finally
            {
                _objectsBeingReturned.Remove(obj);
                UpdateDebugFields();
            }
        }

        public bool IsObjectPartOfPool(GameObject obj)
        {
            return _activePoolSet.Contains(obj) || _inactivePool.Contains(obj);
        }

        public void SetUpPool(Transform poolHolder)
        {
            if (_isInitialized) return;

            _activePool.Clear();
            _inactivePool.Clear();
            _activePoolSet.Clear();
            _objectsBeingReturned.Clear();
            _pooledObjects.Clear();

            _poolHolder = poolHolder;
            if (preWarmPool) WarmPool();
            _isInitialized = true;
            UpdateDebugFields();
        }

        public void ClearPools()
        {
            foreach (var obj in _activePool.Where(obj => obj))
            {
                Object.Destroy(obj);
            }

            _activePool.Clear();
            _activePoolSet.Clear();


            while (_inactivePool.Count > 0)
            {
                var obj = _inactivePool.Dequeue();
                if (obj) Object.Destroy(obj);
            }

            _objectsBeingReturned.Clear();
            _pooledObjects.Clear();

            if (_poolHolder) Object.Destroy(_poolHolder.gameObject);
            _isInitialized = false;
            UpdateDebugFields();
        }

        public void LimitPreWorm()
        {
            preWarmPoolSize = !preWarmPool ? 0 : Mathf.Clamp(preWarmPoolSize, 1, maxPoolSize);
        }

        private void UpdateDebugFields()
        {
#if UNITY_EDITOR || DEBUG
            activePoolCount = _activePool.Count;
            inactivePoolCount = _inactivePool.Count;
            poolSize = activePoolCount + inactivePoolCount;
#endif
        }

        private void WarmPool()
        {
            if (_isInitialized) return;

            for (int i = 0; i < preWarmPoolSize; i++)
            {
                var obj = InstantiatePoolObject();
                _inactivePool.Enqueue(obj);
            }

            UpdateDebugFields();
        }

        private GameObject InstantiatePoolObject()
        {
            if (!prefab) return null;

            var obj = Object.Instantiate(prefab);
            obj.SetActive(false);
            if (_poolHolder) obj.transform.SetParent(_poolHolder);

            if (obj.TryGetComponent(out IPooledObject pooledObject))
            {
                _pooledObjects[obj] = pooledObject;
            }

            return obj;
        }
    }
}