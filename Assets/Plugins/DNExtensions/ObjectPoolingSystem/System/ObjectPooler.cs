using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.ObjectPooling
{
    /// <summary>
    /// Singleton manager for multiple object pools with automatic scene management.
    /// Provides static API for getting and returning objects from pools with fallback support.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        private bool _instantiateAsFallBack;
        private bool _destroyAsFallBack;
        private bool _showDebugMessages;
        private bool _hidePoolHolders;
        private List<Pool> _pools = new List<Pool>();

        private bool _isFirstScene;
        private Transform _dontDestroyOnLoadParent;
        private Transform _destroyOnLoadParent;
        
        public IReadOnlyList<Pool> Pools => _pools;

        /// <summary>
        /// Initializes the ObjectPooler with settings from the settings asset
        /// </summary>
        public void Initialize(ObjectPoolingSettings settings)
        {
            if (!settings)
            {
                Debug.LogError("[ObjectPooler] Cannot initialize without settings!");
                return;
            }

            _instantiateAsFallBack = settings.instantiateAsFallback;
            _destroyAsFallBack = settings.destroyAsFallback;
            _showDebugMessages = settings.showDebugMessages;
            _hidePoolHolders = settings.hidePoolHolders;
            _pools = settings.GetPoolsCopy();

            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _isFirstScene = true;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SetUpPools();
            
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            #endif
        }

        private void OnDestroy()
        {
            if (_dontDestroyOnLoadParent)
            {
                Destroy(_dontDestroyOnLoadParent.gameObject);
                _dontDestroyOnLoadParent = null;
            }

            if (_destroyOnLoadParent)
            {
                Destroy(_destroyOnLoadParent.gameObject);
                _destroyOnLoadParent = null;
            }

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        #if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;

            if (Instance && Instance.gameObject)
            {
                DestroyImmediate(Instance.gameObject);
            }

            Instance = null;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
        #endif

        /// <summary>
        /// Handles pool cleanup and reinitialization when scenes change.
        /// Pools marked as dontDestroyOnLoad persist across scenes.
        /// </summary>
        private static void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            if (!Instance) return;

            if (Instance._isFirstScene)
            {
                Instance._isFirstScene = false;
                return;
            }

            if (Instance._destroyOnLoadParent)
            {
                Destroy(Instance._destroyOnLoadParent.gameObject);
                Instance._destroyOnLoadParent = null;
            }

            Instance._destroyOnLoadParent = new GameObject() { name = "ObjectPools - Destroy On Load" }.transform;
            Instance._destroyOnLoadParent.gameObject.hideFlags = Instance._hidePoolHolders ? HideFlags.HideInHierarchy : HideFlags.None;

            List<Pool> poolsToReinitialize = new List<Pool>();
            foreach (var pool in Instance._pools)
            {
                if (!pool.dontDestroyOnLoad)
                {
                    pool.ClearPools();
                    poolsToReinitialize.Add(pool);
                }
            }

            foreach (var pool in poolsToReinitialize)
            {
                Transform parent;

                if (pool.usePoolHolder)
                {
                    var poolHolder = new GameObject() { name = $"{pool.poolName} Holder" };
                    poolHolder.transform.SetParent(Instance._destroyOnLoadParent);
                    parent = poolHolder.transform;
                }
                else
                {
                    parent = Instance._destroyOnLoadParent;
                }

                pool.SetUpPool(parent);
            }
        }

        /// <summary>
        /// Initializes all pools with their holder GameObjects and parent hierarchy.
        /// </summary>
        private void SetUpPools()
        {
            if (!_destroyOnLoadParent)
            {
                _destroyOnLoadParent = new GameObject() { name = "ObjectPools - Destroy On Load" }.transform;
                _destroyOnLoadParent.gameObject.hideFlags = _hidePoolHolders ? HideFlags.HideInHierarchy : HideFlags.None;
            }

            if (!_dontDestroyOnLoadParent)
            {
                _dontDestroyOnLoadParent = new GameObject() { name = "ObjectPools - Dont Destroy On Load" }.transform;
                _dontDestroyOnLoadParent.gameObject.hideFlags = _hidePoolHolders ? HideFlags.HideInHierarchy : HideFlags.None;
                DontDestroyOnLoad(_dontDestroyOnLoadParent.gameObject);
            }

            foreach (var pool in _pools)
            {
                Transform parent;

                if (pool.usePoolHolder)
                {
                    var poolHolder = new GameObject() { name = $"{pool.poolName} Holder" };
                    poolHolder.transform.SetParent(pool.dontDestroyOnLoad
                        ? _dontDestroyOnLoadParent
                        : _destroyOnLoadParent);
                    parent = poolHolder.transform;
                }
                else
                {
                    parent = pool.dontDestroyOnLoad
                        ? _dontDestroyOnLoadParent
                        : _destroyOnLoadParent;
                }

                pool.SetUpPool(parent);
            }
        }

        /// <summary>
        /// Gets an object from the appropriate pool or instantiates as fallback.
        /// Searches pools by matching prefab reference.
        /// </summary>
        /// <param name="obj">Prefab GameObject to get from pool</param>
        /// <param name="positon">World position for the object</param>
        /// <param name="rotation">World rotation for the object</param>
        /// <returns>GameObject from pool or new instance if no pool found</returns>
        public static GameObject GetObjectFromPool(GameObject obj, Vector3 positon = default, Quaternion rotation = default)
        {
            if (Instance)
            {
                foreach (var pool in Instance._pools)
                {
                    if (pool.prefab == obj)
                    {
                        return pool.GetObjectFromPool(positon, rotation);
                    }
                }

                if (Instance._instantiateAsFallBack)
                {
                    if (Instance._showDebugMessages) Debug.Log($"No pool found for {obj} was found, instantiating as fall back");
                    var fallbackObject = Instantiate(obj, positon, rotation);
                    return fallbackObject;
                }
            }


            return Instantiate(obj, positon, rotation);
        }

        /// <summary>
        /// Returns an object to its appropriate pool or destroys as fallback.
        /// Automatically finds the correct pool by checking object membership.
        /// </summary>
        /// <param name="obj">GameObject to return to pool</param>
        public static void ReturnObjectToPool(GameObject obj)
        {
            if (!obj) return;

            if (Instance)
            {
                foreach (var pool in Instance._pools)
                {
                    if (pool.IsObjectPartOfPool(obj))
                    {
                        pool.ReturnObjectToPool(obj);
                        return;
                    }
                }

                if (Instance._destroyAsFallBack)
                {
                    if (Instance._showDebugMessages) Debug.Log($"No pool found for {obj.name}, destroying as fallback");
                    Destroy(obj);
                    return;
                }
            }
    
            Destroy(obj);
        }
        
        /// <summary>
        /// Gets an object from the appropriate pool or instantiates as fallback.
        /// Searches pools by matching prefab reference.
        /// </summary>
        /// <param name="prefab">Prefab to get from pool</param>
        /// <param name="position">World position for the object</param>
        /// <param name="rotation">World rotation for the object</param>
        /// <returns>Prefab from pool or new instance if no pool found</returns>
        public static T GetObjectFromPool<T>(T prefab, Vector3 position = default, Quaternion rotation = default) where T : Component
        {
            GameObject obj = GetObjectFromPool(prefab.gameObject, position, rotation);
            if (obj && obj.TryGetComponent(out T component))
            {
                return component;
            }
            return null;
        }

        /// <summary>
        /// Returns an object to its appropriate pool or destroys as fallback.
        /// Automatically finds the correct pool by checking object membership.
        /// </summary>
        /// <param name="obj">GameObject to return to pool</param>
        public static void ReturnObjectToPool<T>(T obj) where T : Component
        {
            if (obj) ReturnObjectToPool(obj.gameObject);
        }
    }
}