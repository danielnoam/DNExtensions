using UnityEngine;

namespace DNExtensions.ObjectPooling
{
    /// <summary>
    /// Automatically initializes the object pooling system at runtime.
    /// Runs before scene load to ensure pools are ready.
    /// </summary>
    public static class ObjectPoolingRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (ObjectPooler.Instance) return;
            
            ObjectPoolingSettings settings = ObjectPoolingSettings.Instance;
            
            if (!settings || !settings.enabled)
            {
                // Debug.LogError("ObjectPoolingSettings not found in Resources folder! Create one via: Tools > DNExtensions > Object Pooling Settings");
                return;
            }
            
            GameObject poolerObject = new GameObject("ObjectPooler");
            ObjectPooler pooler = poolerObject.AddComponent<ObjectPooler>();
            pooler.Initialize(settings);
        }
    }
}