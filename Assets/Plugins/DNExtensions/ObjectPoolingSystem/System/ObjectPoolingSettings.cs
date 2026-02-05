using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.ObjectPooling
{
    /// <summary>
    /// Centralized settings asset for object pooling configuration.
    /// Create via: Assets > Create > DNExtensions > Object Pooling Settings
    /// Must be placed in Resources folder for runtime access.
    /// </summary>
    [CreateAssetMenu(fileName = "ObjectPoolingSettings", menuName = "DNExtensions/Object Pooling Settings", order = 1)]
    public class ObjectPoolingSettings : ScriptableObject
    {
        private static ObjectPoolingSettings _instance;
        
        public static ObjectPoolingSettings Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = Resources.Load<ObjectPoolingSettings>("ObjectPoolingSettings");
                    
                    #if UNITY_EDITOR
                    if (!_instance)
                    {
                        Debug.LogWarning("ObjectPoolingSettings not found in Resources folder. Create one via Tools > DNExtensions > Object Pooling Settings");
                    }
                    #endif
                }
                return _instance;
            }
        }
        
        [Header("Settings")]
        [Tooltip("Enable or disable the object pooling system")]
        public bool enabled;
        [Tooltip("If no pool exists for an object, instantiate it instead of returning null")]
        public bool instantiateAsFallback = true;
        [Tooltip("If returning an object that doesn't belong to any pool, destroy it")]
        public bool destroyAsFallback = true;
        [Tooltip("Show debug messages in console for pool operations")]
        public bool showDebugMessages = false;
        [SerializeField] private List<Pool> pools = new List<Pool>();

        public List<Pool> Pools => pools;

        private void OnValidate()
        {
            if (pools == null) return;
            
            foreach (var pool in pools)
            {
                pool?.LimitPreWorm();
            }
        }

        /// <summary>
        /// Creates a copy of all pools for runtime use (prevents modifying the asset).
        /// </summary>
        public List<Pool> GetPoolsCopy()
        {
            List<Pool> poolsCopy = new List<Pool>();
            
            if (pools == null) return poolsCopy;
            
            foreach (var pool in pools)
            {
                if (pool == null) continue;
                
                string json = JsonUtility.ToJson(pool);
                Pool poolCopy = JsonUtility.FromJson<Pool>(json);
                poolsCopy.Add(poolCopy);
            }
            
            return poolsCopy;
        }

        public void AddPool(Pool pool)
        {
            pools ??= new List<Pool>();
            pools.Add(pool);
        }

        public void RemovePool(Pool pool)
        {
            pools?.Remove(pool);
        }

        public void ClearPools()
        {
            pools?.Clear();
        }
    }
}