using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class RendererExtensions
    {
        #region Visibility

        /// <summary>
        /// Shows the renderer (sets enabled to true)
        /// </summary>
        public static void Show(this Renderer renderer)
        {
            if (!renderer) return;
            renderer.enabled = true;
        }

        /// <summary>
        /// Hides the renderer (sets enabled to false)
        /// </summary>
        public static void Hide(this Renderer renderer)
        {
            if (!renderer) return;
            renderer.enabled = false;
        }

        /// <summary>
        /// Toggles the renderer's visibility
        /// </summary>
        public static void ToggleVisibility(this Renderer renderer)
        {
            if (!renderer) return;
            renderer.enabled = !renderer.enabled;
        }

        #endregion

        #region Bounds

        /// <summary>
        /// Checks if the renderer's bounds are visible by any camera
        /// </summary>
        public static bool IsVisibleByAnyCamera(this Renderer renderer)
        {
            if (!renderer) return false;
            return renderer.isVisible;
        }

        /// <summary>
        /// Checks if the renderer's bounds are visible by a specific camera
        /// </summary>
        public static bool IsVisibleByCamera(this Renderer renderer, Camera camera)
        {
            if (!renderer || !camera) return false;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }

        /// <summary>
        /// Gets the center of the renderer's bounds in world space
        /// </summary>
        public static Vector3 GetBoundsCenter(this Renderer renderer)
        {
            if (!renderer) return Vector3.zero;
            return renderer.bounds.center;
        }

        /// <summary>
        /// Gets the size of the renderer's bounds
        /// </summary>
        public static Vector3 GetBoundsSize(this Renderer renderer)
        {
            if (!renderer) return Vector3.zero;
            return renderer.bounds.size;
        }

        #endregion

        #region Sorting (2D)

        /// <summary>
        /// Sets the sorting layer by name
        /// </summary>
        public static void SetSortingLayer(this Renderer renderer, string layerName)
        {
            if (!renderer) return;
            renderer.sortingLayerName = layerName;
        }

        /// <summary>
        /// Sets the sorting layer by ID
        /// </summary>
        public static void SetSortingLayer(this Renderer renderer, int layerID)
        {
            if (!renderer) return;
            renderer.sortingLayerID = layerID;
        }

        /// <summary>
        /// Sets the sorting order
        /// </summary>
        public static void SetSortingOrder(this Renderer renderer, int order)
        {
            if (!renderer) return;
            renderer.sortingOrder = order;
        }

        #endregion

        #region Material Instance Management

        /// <summary>
        /// Gets or creates a material instance (avoids sharing materials)
        /// Use this when you want to modify material properties without affecting other objects
        /// </summary>
        public static Material GetMaterialInstance(this Renderer renderer)
        {
            if (!renderer) return null;

            if (!renderer.material.name.EndsWith("(Instance)"))
            {
                renderer.material = new Material(renderer.material);
            }

            return renderer.material;
        }

        /// <summary>
        /// Destroys material instances to prevent memory leaks
        /// Call this when destroying the renderer or when you're done with instances
        /// </summary>
        public static void DestroyMaterialInstances(this Renderer renderer)
        {
            if (!renderer) return;

            foreach (Material material in renderer.materials)
            {
                if (material != null && material.name.EndsWith("(Instance)"))
                {
                    Object.Destroy(material);
                }
            }
        }

        #endregion
    }
}