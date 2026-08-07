using System;
using System.Linq;
using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class GameObjectExtensions
    {
        #region Component Management

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject) return null;

            if (!gameObject.TryGetComponent<T>(out T component))
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of the given type if it exists, otherwise adds it to the GameObject
        /// </summary>
        public static Component GetOrAddComponent(this GameObject gameObject, Type componentType)
        {
            if (!gameObject) return null;

            Component component = gameObject.GetComponent(componentType);
            if (!component)
            {
                component = gameObject.AddComponent(componentType);
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and provides an out parameter indicating if it was added
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject, out bool wasAdded) where T : Component
        {
            if (!gameObject)
            {
                wasAdded = false;
                return null;
            }

            wasAdded = !gameObject.TryGetComponent<T>(out T component);
            if (wasAdded)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> configureAction) where T : Component
        {
            if (!gameObject) return null;

            if (!gameObject.TryGetComponent<T>(out T component))
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
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> configureAction, out bool wasAdded) where T : Component
        {
            if (!gameObject)
            {
                wasAdded = false;
                return null;
            }

            wasAdded = !gameObject.TryGetComponent<T>(out T component);
            if (wasAdded)
            {
                component = gameObject.AddComponent<T>();
                configureAction?.Invoke(component);
            }
            return component;
        }

        #endregion

        #region Hierarchy Visibility

        /// <summary>
        /// Hides the GameObject from the hierarchy window
        /// </summary>
        public static void HideInHierarchy(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        /// <summary>
        /// Clears the GameObject's hide flags so it shows in the hierarchy window again
        /// </summary>
        public static void ShowInHierarchy(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.hideFlags = HideFlags.None;
        }

        #endregion

        #region Children Management

        /// <summary>
        /// Destroys all child GameObjects at the end of the frame
        /// </summary>
        public static void DestroyChildren(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.DestroyAllChildren(false);
        }

        /// <summary>
        /// Destroys all child GameObjects immediately (for editor use)
        /// </summary>
        public static void DestroyChildrenImmediate(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.DestroyAllChildren(true);
        }

        /// <summary>
        /// Activates all direct children
        /// </summary>
        public static void EnableChildren(this GameObject gameObject)
        {
            if (!gameObject) return;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Deactivates all direct children
        /// </summary>
        public static void DisableChildren(this GameObject gameObject)
        {
            if (!gameObject) return;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Transform Operations

        /// <summary>
        /// Resets the GameObject's world position, rotation, and scale to default values
        /// </summary>
        public static void ResetTransformation(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.ResetTransform(true);
        }

        #endregion

        #region Hierarchy Path

        /// <summary>
        /// Gets the hierarchy path of the GameObject's parents, from root down to the direct parent
        /// </summary>
        public static string GetPath(this GameObject gameObject)
        {
            if (!gameObject) return string.Empty;

            string[] parents = gameObject.GetComponentsInParent<Transform>()
                .Skip(1)
                .Select(t => t.name)
                .Reverse()
                .ToArray();

            return parents.Length == 0 ? string.Empty : "/" + string.Join("/", parents);
        }

        /// <summary>
        /// Gets the hierarchy path of the GameObject, from root down to and including itself
        /// </summary>
        public static string GetFullPath(this GameObject gameObject)
        {
            if (!gameObject) return string.Empty;
            return gameObject.GetPath() + "/" + gameObject.name;
        }

        #endregion

        #region Layer Management

        /// <summary>
        /// Sets the layer on the GameObject and all of its children
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (!gameObject) return;

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// Sets the layer on the GameObject and all of its children
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1)
            {
                Debug.LogWarning($"Layer '{layerName}' does not exist!");
                return;
            }
            gameObject.SetLayerRecursively(layer);
        }

        #endregion

    }
}
