using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace DNExtensions.ObjectPooling
{
    
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [Header("Settings")] 
        [SerializeField] private bool instantiateAsFallBack = true;
        [SerializeField] private bool destroyAsFallBack = true;
        [SerializeField] private List<ObjectPool> pools = new List<ObjectPool>();

        private bool _isFirstScene;


        private void OnValidate()
        {
            foreach (var pool in pools)
            {
                pool.LimitPreWorm();
            }
        }


        private void Awake()
        {
            if (!Instance || Instance == this)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            

            _isFirstScene = true;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SetUpPools();
            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            #endif
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
        



        private static void OnActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            if (!Instance) return;

            // Let awake run if it's the first time the game is loaded
            if (Instance._isFirstScene)
            {
                Instance._isFirstScene = false;
                return;
            }

            List<ObjectPool> poolsToReinitialize = new List<ObjectPool>();
            foreach (var pool in Instance.pools)
            {
                if (!pool.dontDestroyOnLoad)
                {
                    pool.ClearPools();
                    poolsToReinitialize.Add(pool);
                }
            }

            foreach (var pool in poolsToReinitialize)
            {
                var poolHolder = new GameObject() { name = $"{pool.poolName} Holder" };
                pool.SetUpPool(poolHolder.transform);
            }
        }


        private void SetUpPools()
        {
            foreach (var pool in pools)
            {
                var poolHolder = new GameObject() { name = $"{pool.poolName} Holder" };
                if (pool.dontDestroyOnLoad) poolHolder.transform.SetParent(transform);
                pool.SetUpPool(poolHolder.transform);
            }
        }

        public static GameObject GetObjectFromPool(GameObject obj, Vector3 positon = default,
            Quaternion rotation = default)
        {
            if (Instance)
            {
                foreach (var pool in Instance.pools)
                {
                    if (pool.prefab == obj)
                    {
                        return pool.GetObjectFromPool(positon, rotation);
                    }
                }

                if (Instance.instantiateAsFallBack)
                {
                    // Debug.Log($"No pool found for {obj} was found, instantiating as fall back");
                    var fallbackObject = Instantiate(obj, positon, rotation);
                    return fallbackObject;
                }
            }

            // Debug.LogError($"Can't get object, No object pooler in scene");
            return Instantiate(obj, positon, rotation);
        }



        public static void ReturnObjectToPool(GameObject obj)
        {
            if (!obj) return;

            if (Instance)
            {
                foreach (var pool in Instance.pools)
                {
                    if (pool.IsObjectPartOfPool(obj))
                    {
                        pool.ReturnObjectToPool(obj);
                        return;
                    }
                }

                if (Instance.destroyAsFallBack)
                {
                    // Debug.Log($"No pool found for {obj.name}, destroying as fallback");
                    Destroy(obj);
                    return;
                }
            }


            // Debug.LogError($"Can't return object, No object pooler in scene");
            Destroy(obj);
        }
        


    }

}