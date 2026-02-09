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
        public static Component GetOrAddComponent(this GameObject gameObject, Type componentType)
        {
            if (!gameObject) return null;
            
            Component component = gameObject.GetComponent(componentType);
            if (component == null)
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
            wasAdded = false;
            if (!gameObject) return null;
            
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
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> configureAction) where T : Component
        {
            if (!gameObject) return null;
            
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
        public static T GetOrAddComponent<T>(this GameObject gameObject, Action<T> configureAction, out bool wasAdded) where T : Component
        {
            wasAdded = false;
            if (!gameObject) return null;
            
            T component = gameObject.GetComponent<T>();
            wasAdded = component == null;
            if (wasAdded)
            {
                component = gameObject.AddComponent<T>();
                configureAction?.Invoke(component);
            }
            return component;
        }

        #endregion

        #region Null Handling

        /// <summary>
        /// Returns the object itself if it exists, null otherwise
        /// Helps differentiate between a null reference and a destroyed Unity object
        /// </summary>
        public static T OrNull<T>(this T obj) where T : UnityEngine.Object
        {
            return obj ? obj : null;
        }

        #endregion

        #region Hierarchy Visibility

        /// <summary>
        /// Hides the GameObject in the Hierarchy view
        /// </summary>
        public static void HideInHierarchy(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        /// <summary>
        /// Shows the GameObject in the Hierarchy view (removes HideInHierarchy flag)
        /// </summary>
        public static void ShowInHierarchy(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.hideFlags = HideFlags.None;
        }

        #endregion

        #region Children Management

        /// <summary>
        /// Destroys all children of the GameObject
        /// </summary>
        public static void DestroyChildren(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.DestroyAllChildren(false);
        }

        /// <summary>
        /// Immediately destroys all children of the GameObject
        /// </summary>
        public static void DestroyChildrenImmediate(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.DestroyAllChildren(true);
        }

        /// <summary>
        /// Enables all child GameObjects
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
        /// Disables all child GameObjects
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
        /// Resets the GameObject's transform position, rotation, and scale to default values
        /// </summary>
        public static void ResetTransformation(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.ResetTransform(true);
        }

        #endregion

        #region Hierarchy Path

        /// <summary>
        /// Returns the hierarchical path in the Unity scene hierarchy for this GameObject
        /// </summary>
        /// <returns>A '/'-separated string path from root to parent (excluding this GameObject)</returns>
        public static string GetPath(this GameObject gameObject)
        {
            if (!gameObject) return string.Empty;
            
            return "/" + string.Join("/",
                gameObject.GetComponentsInParent<Transform>()
                    .Select(t => t.name)
                    .Reverse()
                    .ToArray());
        }

        /// <summary>
        /// Returns the full hierarchical path in the Unity scene hierarchy for this GameObject
        /// </summary>
        /// <returns>A '/'-separated string path from root to this GameObject (including this GameObject)</returns>
        public static string GetFullPath(this GameObject gameObject)
        {
            if (!gameObject) return string.Empty;
            return gameObject.GetPath() + "/" + gameObject.name;
        }

        #endregion

        #region Layer Management

        /// <summary>
        /// Recursively sets the layer for this GameObject and all of its descendants
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
        /// Recursively sets the layer for this GameObject and all of its descendants
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

        #region Activation (MonoBehaviour)

        /// <summary>
        /// Activates the GameObject associated with the MonoBehaviour and returns the instance
        /// </summary>
        public static T SetActive<T>(this T obj) where T : MonoBehaviour
        {
            if (!obj) return obj;
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Deactivates the GameObject associated with the MonoBehaviour and returns the instance
        /// </summary>
        public static T SetInactive<T>(this T obj) where T : MonoBehaviour
        {
            if (!obj) return obj;
            obj.gameObject.SetActive(false);
            return obj;
        }

        #endregion
    }
}