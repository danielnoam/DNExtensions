using System;
using System.Linq;
using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class GameObjectExtensions
    {
        #region Component Management

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject) return null;
            
            if (!gameObject.TryGetComponent<T>(out T component))
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

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

        #region Null Handling

        public static T OrNull<T>(this T obj) where T : UnityEngine.Object
        {
            return obj ? obj : null;
        }

        #endregion

        #region Hierarchy Visibility

        public static void HideInHierarchy(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        public static void ShowInHierarchy(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.hideFlags = HideFlags.None;
        }

        #endregion

        #region Children Management

        public static void DestroyChildren(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.DestroyAllChildren(false);
        }

        public static void DestroyChildrenImmediate(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.DestroyAllChildren(true);
        }

        public static void EnableChildren(this GameObject gameObject)
        {
            if (!gameObject) return;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(true);
            }
        }

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

        public static void ResetTransformation(this GameObject gameObject)
        {
            if (!gameObject) return;
            gameObject.transform.ResetTransform(true);
        }

        #endregion

        #region Hierarchy Path

        public static string GetPath(this GameObject gameObject)
        {
            if (!gameObject) return string.Empty;
            
            return "/" + string.Join("/",
                gameObject.GetComponentsInParent<Transform>()
                    .Select(t => t.name)
                    .Reverse()
                    .ToArray());
        }

        public static string GetFullPath(this GameObject gameObject)
        {
            if (!gameObject) return string.Empty;
            return gameObject.GetPath() + "/" + gameObject.name;
        }

        #endregion

        #region Layer Management

        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (!gameObject) return;
            
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

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

        public static T SetActive<T>(this T obj) where T : MonoBehaviour
        {
            if (!obj) return obj;
            obj.gameObject.SetActive(true);
            return obj;
        }

        public static T SetInactive<T>(this T obj) where T : MonoBehaviour
        {
            if (!obj) return obj;
            obj.gameObject.SetActive(false);
            return obj;
        }

        #endregion
    }
}