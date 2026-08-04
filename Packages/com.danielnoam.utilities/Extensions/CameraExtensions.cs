using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class CameraExtensions
    {
        #region Viewport and Frustum

        /// <summary>
        /// Calculates viewport extents with an optional margin (useful for frustum culling)
        /// </summary>
        /// <param name="camera">The camera</param>
        /// <param name="viewportMargin">Optional margin to apply to viewport extents</param>
        /// <returns>Viewport extents as a Vector2 after applying the margin</returns>
        public static Vector2 GetViewportExtentsWithMargin(this Camera camera, Vector2? viewportMargin = null)
        {
            if (!camera) return Vector2.zero;

            Vector2 margin = viewportMargin ?? new Vector2(0.2f, 0.2f);
            Vector2 result;
            float halfFieldOfView = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            result.y = camera.nearClipPlane * Mathf.Tan(halfFieldOfView);
            result.x = result.y * camera.aspect + margin.x;
            result.y += margin.y;
            return result;
        }

        /// <summary>
        /// Gets the frustum height at a specific distance from the camera
        /// </summary>
        public static float GetFrustumHeightAtDistance(this Camera camera, float distance)
        {
            if (!camera) return 0f;
            return 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Gets the frustum width at a specific distance from the camera
        /// </summary>
        public static float GetFrustumWidthAtDistance(this Camera camera, float distance)
        {
            if (!camera) return 0f;
            return camera.GetFrustumHeightAtDistance(distance) * camera.aspect;
        }

        /// <summary>
        /// Gets the size of the frustum at a specific distance from the camera
        /// </summary>
        public static Vector2 GetFrustumSizeAtDistance(this Camera camera, float distance)
        {
            if (!camera) return Vector2.zero;
            float height = camera.GetFrustumHeightAtDistance(distance);
            return new Vector2(height * camera.aspect, height);
        }

        #endregion

        #region World/Screen/Viewport Conversions

        /// <summary>
        /// Checks if a world position is visible in the camera's viewport
        /// </summary>
        public static bool IsVisible(this Camera camera, Vector3 worldPosition)
        {
            if (!camera) return false;

            Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);
            return viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                   viewportPoint.y >= 0f && viewportPoint.y <= 1f &&
                   viewportPoint.z > 0f;
        }

        /// <summary>
        /// Checks if a bounds is visible in the camera's viewport
        /// </summary>
        public static bool IsVisible(this Camera camera, Bounds bounds)
        {
            if (!camera) return false;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        /// <summary>
        /// Gets the world position of the mouse cursor at a specific distance from the camera
        /// </summary>
        public static Vector3 GetMouseWorldPosition(this Camera camera, float distance)
        {
            if (!camera) return Vector3.zero;

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = distance;
            return camera.ScreenToWorldPoint(mousePos);
        }

        /// <summary>
        /// Gets the world position of the mouse cursor on a plane
        /// </summary>
        public static bool GetMouseWorldPositionOnPlane(this Camera camera, Plane plane, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (!camera) return false;

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }
            return false;
        }

        #endregion

        #region Bounds and Size

        /// <summary>
        /// Gets the orthographic size needed to fit a bounds in the camera view
        /// </summary>
        public static float GetOrthographicSizeToFit(this Camera camera, Bounds bounds)
        {
            if (!camera) return 0f;

            float verticalSize = bounds.size.y * 0.5f;
            float horizontalSize = bounds.size.x / camera.aspect * 0.5f;
            return Mathf.Max(verticalSize, horizontalSize);
        }

        /// <summary>
        /// Gets the distance needed to fit a bounds in the camera view (perspective)
        /// </summary>
        public static float GetDistanceToFit(this Camera camera, Bounds bounds)
        {
            if (!camera || camera.orthographic) return 0f;

            float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float fov = camera.fieldOfView * Mathf.Deg2Rad;
            float distance = objectSize / (2f * Mathf.Tan(fov * 0.5f));
            
            return distance + bounds.extents.magnitude;
        }

        #endregion

        #region Camera Rays

        /// <summary>
        /// Creates a ray from the camera through the center of the screen
        /// </summary>
        public static Ray GetCenterRay(this Camera camera)
        {
            if (!camera) return new Ray();
            return camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        /// <summary>
        /// Creates a ray from the camera through the mouse position
        /// </summary>
        public static Ray GetMouseRay(this Camera camera)
        {
            if (!camera) return new Ray();
            return camera.ScreenPointToRay(Input.mousePosition);
        }

        #endregion

        #region Shake Effects

        /// <summary>
        /// Gets the current shake offset (call this in an update loop during shake)
        /// </summary>
        public static Vector3 GetShakeOffset(float intensity, float frequency, float time)
        {
            float x = Mathf.PerlinNoise(time * frequency, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0f, time * frequency) * 2f - 1f;
            float z = Mathf.PerlinNoise(time * frequency, time * frequency) * 2f - 1f;
            
            return new Vector3(x, y, z) * intensity;
        }

        #endregion

        #region Screen Bounds

        /// <summary>
        /// Gets the world bounds of the camera's view at a specific distance
        /// </summary>
        public static Bounds GetWorldBoundsAtDistance(this Camera camera, float distance)
        {
            if (!camera) return new Bounds();

            Vector2 frustumSize = camera.GetFrustumSizeAtDistance(distance);
            Vector3 center = camera.transform.position + camera.transform.forward * distance;
            
            return new Bounds(center, new Vector3(frustumSize.x, frustumSize.y, 0f));
        }

        /// <summary>
        /// Gets the corners of the camera frustum at a specific distance in world space
        /// </summary>
        public static Vector3[] GetFrustumCornersAtDistance(this Camera camera, float distance)
        {
            if (!camera) return new Vector3[4];

            Vector3[] corners = new Vector3[4];
            camera.CalculateFrustumCorners(
                new Rect(0, 0, 1, 1),
                distance,
                Camera.MonoOrStereoscopicEye.Mono,
                corners
            );

            for (int i = 0; i < 4; i++)
            {
                corners[i] = camera.transform.TransformPoint(corners[i]);
            }

            return corners;
        }

        #endregion

        #region Zoom

        /// <summary>
        /// Smoothly zooms orthographic camera to target size
        /// </summary>
        public static void ZoomOrthographic(this Camera camera, float targetSize, float speed)
        {
            if (!camera || !camera.orthographic) return;
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, targetSize, speed * Time.deltaTime);
        }

        /// <summary>
        /// Smoothly zooms perspective camera by adjusting FOV
        /// </summary>
        public static void ZoomPerspective(this Camera camera, float targetFOV, float speed)
        {
            if (!camera || camera.orthographic) return;
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, speed * Time.deltaTime);
        }

        #endregion
    }
}