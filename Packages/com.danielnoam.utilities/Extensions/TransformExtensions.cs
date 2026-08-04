using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Utility functions for Transform operations that extend MonoBehaviour
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Checks if a transform is a child of another transform (direct or indirect)
        /// </summary>
        /// <param name="child">The potential child transform</param>
        /// <param name="parent">The potential parent transform</param>
        /// <returns>True if the transform is a child, false otherwise</returns>
        public static bool IsChildOf(this Transform child, Transform parent)
        {
            if (!child || !parent)
                return false;

            Transform currentParent = child.parent;

            while (currentParent)
            {
                if (currentParent == parent)
                    return true;

                currentParent = currentParent.parent;
            }

            return false;
        }

        /// <summary>
        /// Gets the full hierarchy path of a transform from root to the transform
        /// </summary>
        /// <param name="transform">The transform to get the path for</param>
        /// <returns>A string representing the full hierarchy path</returns>
        public static string GetHierarchyPath(this Transform transform)
        {
            if (!transform) return "";

            // Build the full path from root to this transform
            string path = transform.name;
            Transform parent = transform.parent;

            while (parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Gets all children transforms recursively
        /// </summary>
        /// <param name="transform">The parent transform</param>
        /// <returns>An enumerable of all child transforms</returns>
        public static IEnumerable<Transform> GetChildrenRecursive(this Transform transform)
        {
            if (!transform) yield break;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                yield return child;

                foreach (Transform grandChild in child.GetChildrenRecursive())
                {
                    yield return grandChild;
                }
            }
        }

        /// <summary>
        /// Finds a child transform by name recursively
        /// </summary>
        /// <param name="transform">The parent transform to search in</param>
        /// <param name="name">The name of the child to find</param>
        /// <returns>The found transform or null if not found</returns>
        public static Transform FindChildRecursive(this Transform transform, string name)
        {
            if (!transform) return null;

            // Check direct children first
            Transform directChild = transform.Find(name);
            if (directChild) return directChild;

            // Search recursively in grandchildren
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform found = transform.GetChild(i).FindChildRecursive(name);
                if (found) return found;
            }

            return null;
        }

        /// <summary>
        /// Gets the root transform of the hierarchy
        /// </summary>
        /// <param name="transform">The transform to get the root for</param>
        /// <returns>The root transform</returns>
        public static Transform GetRoot(this Transform transform)
        {
            if (!transform) return null;

            Transform root = transform;
            while (root.parent)
            {
                root = root.parent;
            }
            return root;
        }

        /// <summary>
        /// Gets the depth/level of this transform in the hierarchy (0 = root)
        /// </summary>
        /// <param name="transform">The transform to get the depth for</param>
        /// <returns>The depth level</returns>
        public static int GetDepth(this Transform transform)
        {
            if (!transform) return -1;

            int depth = 0;
            Transform parent = transform.parent;
            while (parent != null)
            {
                depth++;
                parent = parent.parent;
            }
            return depth;
        }

        /// <summary>
        /// Resets the transform's position, rotation, and scale to default values
        /// </summary>
        /// <param name="transform">The transform to reset</param>
        /// <param name="local">Whether to reset local or world transform</param>
        public static void ResetTransform(this Transform transform, bool local = true)
        {
            if (!transform) return;

            if (local)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
            else
            {
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one; // Scale is always local
            }
        }

        /// <summary>
        /// Copies transform values from another transform
        /// </summary>
        /// <param name="transform">The transform to copy to</param>
        /// <param name="source">The transform to copy from</param>
        /// <param name="local">Whether to copy local or world transform</param>
        public static void CopyFrom(this Transform transform, Transform source, bool local = true)
        {
            if (!transform || !source) return;

            if (local)
            {
                transform.localPosition = source.localPosition;
                transform.localRotation = source.localRotation;
                transform.localScale = source.localScale;
            }
            else
            {
                transform.position = source.position;
                transform.rotation = source.rotation;
                transform.localScale = source.localScale;
            }
        }

        /// <summary>
        /// Destroys all child GameObjects
        /// </summary>
        /// <param name="transform">The parent transform</param>
        /// <param name="immediate">Whether to use DestroyImmediate (for editor use)</param>
        public static void DestroyAllChildren(this Transform transform, bool immediate = false)
        {
            if (!transform) return;

            // Iterate backwards to avoid index shifting issues
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (immediate)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Gets all components of type T from this transform and all its children
        /// </summary>
        /// <typeparam name="T">The component type to search for</typeparam>
        /// <param name="transform">The transform to search in</param>
        /// <returns>An array of all found components</returns>
        public static T[] GetComponentsInChildrenRecursive<T>(this Transform transform) where T : Component
        {
            if (!transform) return Array.Empty<T>();

            List<T> components = new List<T>();
            
            // Add components from this transform
            components.AddRange(transform.GetComponents<T>());

            // Add components from all children recursively
            foreach (Transform child in transform.GetChildrenRecursive())
            {
                components.AddRange(child.GetComponents<T>());
            }

            return components.ToArray();
        }

        /// <summary>
        /// Gets the sibling index path (useful for recreating hierarchy positions)
        /// </summary>
        /// <param name="transform">The transform to get the sibling path for</param>
        /// <returns>An array of sibling indices from root to this transform</returns>
        public static int[] GetSiblingIndexPath(this Transform transform)
        {
            if (!transform) return Array.Empty<int>();

            List<int> path = new List<int>();
            Transform current = transform;

            while (current.parent != null)
            {
                path.Insert(0, current.GetSiblingIndex());
                current = current.parent;
            }

            return path.ToArray();
        }

        /// <summary>
        /// Finds a transform by following a sibling index path
        /// </summary>
        /// <param name="root">The root transform to start from</param>
        /// <param name="siblingPath">The sibling index path to follow</param>
        /// <returns>The found transform or null if path is invalid</returns>
        public static Transform FindByIndexPath(this Transform root, int[] siblingPath)
        {
            if (!root || siblingPath == null || siblingPath.Length == 0) return root;

            Transform current = root;
            
            foreach (int index in siblingPath)
            {
                if (index < 0 || index >= current.childCount)
                    return null;
                
                current = current.GetChild(index);
            }

            return current;
        }

        /// <summary>
        /// Checks if this transform is a direct child of the specified parent
        /// </summary>
        /// <param name="transform">The transform to check</param>
        /// <param name="parent">The potential parent</param>
        /// <returns>True if it's a direct child, false otherwise</returns>
        public static bool IsDirectChildOf(this Transform transform, Transform parent)
        {
            return transform && transform.parent == parent;
        }

        /// <summary>
        /// Gets all direct children as an enumerable
        /// </summary>
        /// <param name="transform">The parent transform</param>
        /// <returns>An enumerable of direct children</returns>
        public static IEnumerable<Transform> GetDirectChildren(this Transform transform)
        {
            if (!transform) yield break;

            for (int i = 0; i < transform.childCount; i++)
            {
                yield return transform.GetChild(i);
            }
        }

        /// <summary>
        /// Sets the parent and resets the local transform
        /// </summary>
        /// <param name="transform">The transform to reparent</param>
        /// <param name="parent">The new parent</param>
        /// <param name="worldPositionStays">Whether to maintain world position</param>
        public static void SetParentAndReset(this Transform transform, Transform parent, bool worldPositionStays = false)
        {
            if (!transform) return;

            transform.SetParent(parent, worldPositionStays);
            if (!worldPositionStays)
            {
                transform.ResetTransform(true);
            }
        }
    }
}