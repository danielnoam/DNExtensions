using System;
using UnityEngine;

namespace DNExtensions
{
    /// <summary>
    /// Utility functions for MonoBehaviour and GameObject operations
    /// </summary>
    public static class MonoBehaviourUtilities
    {
        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            T component = monoBehaviour.GetComponent<T>();
            if (component == null)
            {
                component = monoBehaviour.gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <param name="componentType">The type of component to get or add</param>
        /// <returns>The existing or newly added component</returns>
        public static Component GetOrAddComponent(this MonoBehaviour monoBehaviour, Type componentType)
        {
            Component component = monoBehaviour.GetComponent(componentType);
            if (component == null)
            {
                component = monoBehaviour.gameObject.AddComponent(componentType);
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the specified GameObject
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <param name="targetGameObject">The GameObject to get or add the component to</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, GameObject targetGameObject) where T : Component
        {
            T component = targetGameObject.GetComponent<T>();
            if (component == null)
            {
                component = targetGameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the specified GameObject
        /// </summary>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <param name="targetGameObject">The GameObject to get or add the component to</param>
        /// <param name="componentType">The type of component to get or add</param>
        /// <returns>The existing or newly added component</returns>
        public static Component GetOrAddComponent(this MonoBehaviour monoBehaviour, GameObject targetGameObject, Type componentType)
        {
            Component component = targetGameObject.GetComponent(componentType);
            if (component == null)
            {
                component = targetGameObject.AddComponent(componentType);
            }
            return component;
        }

        /// <summary>
        /// Tries to get a component of type T, and provides an out parameter indicating if it was added
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <param name="wasAdded">True if the component was added, false if it already existed</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, out bool wasAdded) where T : Component
        {
            T component = monoBehaviour.GetComponent<T>();
            wasAdded = component == null;
            if (wasAdded)
            {
                component = monoBehaviour.gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <param name="configureAction">Action to configure the component if it's newly added</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, Action<T> configureAction) where T : Component
        {
            T component = monoBehaviour.GetComponent<T>();
            if (component == null)
            {
                component = monoBehaviour.gameObject.AddComponent<T>();
                configureAction?.Invoke(component);
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// Also provides an out parameter indicating if it was added
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour to extend</param>
        /// <param name="configureAction">Action to configure the component if it's newly added</param>
        /// <param name="wasAdded">True if the component was added, false if it already existed</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour, Action<T> configureAction, out bool wasAdded) where T : Component
        {
            T component = monoBehaviour.GetComponent<T>();
            wasAdded = component == null;
            if (wasAdded)
            {
                component = monoBehaviour.gameObject.AddComponent<T>();
                configureAction?.Invoke(component);
            }
            return component;
        }
    }

    /// <summary>
    /// Utility functions for GameObject operations
    /// </summary>
    public static class GameObjectUtilities
    {
        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="gameObject">The GameObject to extend</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject to extend</param>
        /// <param name="componentType">The type of component to get or add</param>
        /// <returns>The existing or newly added component</returns>
        public static Component GetOrAddComponent(this GameObject gameObject, Type componentType)
        {
            Component component = gameObject.GetComponent(componentType);
            if (component == null)
            {
                component = gameObject.AddComponent(componentType);
            }
            return component;
        }

        /// <summary>
        /// Tries to get a component of type T, and provides an out parameter indicating if it was added
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="gameObject">The GameObject to extend</param>
        /// <param name="wasAdded">True if the component was added, false if it already existed</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject, out bool wasAdded) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            wasAdded = component == null;
            if (wasAdded)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="gameObject">The GameObject to extend</param>
        /// <param name="configureAction">Action to configure the component if it's newly added</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> configureAction) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
                configureAction?.Invoke(component);
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// Also provides an out parameter indicating if it was added
        /// </summary>
        /// <typeparam name="T">The type of component to get or add</typeparam>
        /// <param name="gameObject">The GameObject to extend</param>
        /// <param name="configureAction">Action to configure the component if it's newly added</param>
        /// <param name="wasAdded">True if the component was added, false if it already existed</param>
        /// <returns>The existing or newly added component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> configureAction, out bool wasAdded) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            wasAdded = component == null;
            if (wasAdded)
            {
                component = gameObject.AddComponent<T>();
                configureAction?.Invoke(component);
            }
            return component;
        }
    }
}