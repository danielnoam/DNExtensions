using UnityEngine;

namespace DNExtensions.ObjectPooling
{
    public static class ObjectPoolingRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (ObjectPooler.Instance) return;
            
            ObjectPoolingSettings settings = ObjectPoolingSettings.Instance;
            
            if (!settings)
            {
                Debug.LogError("ObjectPoolingSettings not found in Resources folder! Create one via: Assets > Create > DNExtensions > Object Pooling Settings, then place it in a Resources folder.");
                return;
            }
            
            GameObject poolerObject = new GameObject("ObjectPooler");
            ObjectPooler pooler = poolerObject.AddComponent<ObjectPooler>();
            pooler.Initialize(settings);
        }
    }
}