using UnityEngine;

namespace DNExtensions.Systems.ObjectPooling
{
    /// <summary>
    /// Automatically initializes the object pooling system at runtime.
    /// Runs before scene load to ensure pools are ready.
    /// </summary>
    internal static class ObjectPoolingRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (ObjectPooler.Instance) return;
            
            ObjectPoolingSettings settings = ObjectPoolingSettings.Instance;
            
            if (!settings || !settings.enabled) return;

            
            GameObject poolerObject = new GameObject("ObjectPooler");
            ObjectPooler pooler = poolerObject.AddComponent<ObjectPooler>();
            pooler.Initialize(settings);
        }
    }
}