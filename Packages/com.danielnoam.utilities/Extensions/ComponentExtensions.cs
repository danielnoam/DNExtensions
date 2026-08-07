using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            if (!component) return null;
            return component.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it to the GameObject
        /// </summary>
        public static Component GetOrAddComponent(this Component component, Type componentType)
        {
            if (!component) return null;
            return component.gameObject.GetOrAddComponent(componentType);
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and provides an out parameter indicating if it was added
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component, out bool wasAdded) where T : Component
        {
            wasAdded = false;
            if (!component) return null;
            return component.gameObject.GetOrAddComponent<T>(out wasAdded);
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component, Action<T> configureAction) where T : Component
        {
            if (!component) return null;
            return component.gameObject.GetOrAddComponent(configureAction);
        }

        /// <summary>
        /// Gets a component of type T if it exists, otherwise adds it and configures it with the provided action
        /// Also provides an out parameter indicating if it was added
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component, Action<T> configureAction, out bool wasAdded) where T : Component
        {
            wasAdded = false;
            if (!component) return null;
            return component.gameObject.GetOrAddComponent(configureAction, out wasAdded);
        }

        /// <summary>
        /// Gets the closest component of type T in the parent hierarchy, excluding this GameObject
        /// </summary>
        public static T GetComponentInParentOnly<T>(this Component component, bool includeInactive = false) where T : Component
        {
            if (!component) return null;

            Transform parent = component.transform.parent;
            while (parent)
            {
                if ((includeInactive || parent.gameObject.activeInHierarchy) && parent.TryGetComponent(out T found))
                {
                    return found;
                }
                parent = parent.parent;
            }

            return null;
        }

        /// <summary>
        /// Gets a component of type T from anywhere in the hierarchy except this GameObject, searching parents before children
        /// </summary>
        public static T GetComponentInHierarchy<T>(this Component component, bool includeInactive = false) where T : Component
        {
            if (!component) return null;

            T parentComponent = component.GetComponentInParentOnly<T>(includeInactive);
            if (parentComponent) return parentComponent;

            foreach (T candidate in component.GetComponentsInChildren<T>(includeInactive))
            {
                if (candidate.transform != component.transform) return candidate;
            }

            return null;
        }

        /// <summary>
        /// Activates the component's GameObject and returns the component for method chaining
        /// </summary>
        public static T SetActive<T>(this T component) where T : Component
        {
            if (!component) return component;
            component.gameObject.SetActive(true);
            return component;
        }

        /// <summary>
        /// Deactivates the component's GameObject and returns the component for method chaining
        /// </summary>
        public static T SetInactive<T>(this T component) where T : Component
        {
            if (!component) return component;
            component.gameObject.SetActive(false);
            return component;
        }
    }
}
