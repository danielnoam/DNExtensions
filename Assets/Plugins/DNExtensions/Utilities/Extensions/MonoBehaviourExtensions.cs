using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            if (!monoBehaviour) return null;
            return monoBehaviour.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        public static Component GetOrAddComponent(this MonoBehaviour monoBehaviour, Type componentType)
        {
            if (!monoBehaviour) return null;
            return monoBehaviour.gameObject.GetOrAddComponent(componentType);
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and provides an out parameter indicating if it was added
        /// </summary>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, out bool wasAdded) where T : Component
        {
            wasAdded = false;
            if (!monoBehaviour) return null;
            return monoBehaviour.gameObject.GetOrAddComponent<T>(out wasAdded);
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// </summary>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, Action<T> configureAction) where T : Component
        {
            if (!monoBehaviour) return null;
            return monoBehaviour.gameObject.GetOrAddComponent(configureAction);
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// Also provides an out parameter indicating if it was added
        /// </summary>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, Action<T> configureAction, out bool wasAdded) where T : Component
        {
            wasAdded = false;
            if (!monoBehaviour) return null;
            return monoBehaviour.gameObject.GetOrAddComponent(configureAction, out wasAdded);
        }
    }
}