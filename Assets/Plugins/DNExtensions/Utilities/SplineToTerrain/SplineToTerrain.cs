#if DNEXTENSIONS_SPLINES
using UnityEngine;
using UnityEngine.Splines;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Bridges a SplineContainer with a Terrain — snapping spline knots to terrain height,
    /// deforming terrain to match the spline path, and painting terrain layers along it.
    /// </summary>
    [AddComponentMenu("DNExtensions/Spline To Terrain")]
    [ExecuteAlways]
    public class SplineToTerrain : MonoBehaviour
    {
        [SerializeField, Tooltip("The Spline to bridge with terrain.")]
        private SplineContainer container;

        [SerializeField, Tooltip("The terrain to interact with. If null, will search for nearest terrain.")]
        private Terrain terrain;

        [SerializeField, Tooltip("Enable to automatically update when the spline is modified.")]
        private bool rebuildOnSplineChange = true;

        [SerializeField, Tooltip("The maximum number of times per-second that updates will occur.")]
        private int rebuildFrequency = 30;

        [Header("Spline to Terrain")]
        [SerializeField, Tooltip("Snap spline knot positions to terrain height.")]
        private bool snapSplineToTerrain;

        [SerializeField, Tooltip("Additional vertical offset applied to snapped spline points.")]
        private float splineHeightOffset;

        [Header("Terrain to Spline")]
        [SerializeField, Tooltip("Deform terrain to match the spline path.")]
        private bool deformTerrainToSpline;

        [SerializeField, Tooltip("The width (in world units) where terrain fully matches spline height.")]
        private float deformWidth = 5f;

        [SerializeField, Tooltip("Additional distance (in world units) for smooth falloff beyond the width.")]
        private float smoothingDistance = 3f;

        [SerializeField, Tooltip("Offset applied to the target terrain height (X=along spline, Y=vertical, Z=perpendicular to spline).")]
        private Vector3 terrainOffset;

        [SerializeField, Tooltip("How many terrain samples per world unit along the spline.")]
        private float samplesPerUnit = 2f;

        [SerializeField, Range(0f, 1f), Tooltip("Blend factor between original terrain and target height. 0 = no change, 1 = full deformation.")]
        private float deformStrength = 1f;

        [Header("Paint Terrain Layer")]
        [SerializeField, Tooltip("Paint a terrain layer along the spline path.")]
        private bool paintTerrainLayer;

        [SerializeField, Tooltip("The terrain layer to paint along the spline.")]
        private TerrainLayer terrainLayer;

        [SerializeField, Tooltip("The width (in world units) where the layer is painted at full strength.")]
        private float paintWidth = 5f;

        [SerializeField, Tooltip("Additional distance (in world units) for smooth falloff beyond the paint width.")]
        private float paintSmoothingDistance = 3f;

        [SerializeField, Range(0f, 1f), Tooltip("Blend factor between the current layer weights and full paint. 0 = no change, 1 = full paint.")]
        private float paintStrength = 1f;

        private float _nextScheduledRebuild;
        private bool _rebuildRequested;

        public Spline Spline => container ? container.Spline : null;

        private void OnEnable()
        {
            if (!terrain)
                terrain = FindNearestTerrain();

            Spline.Changed += OnSplineChanged;
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        private void Update()
        {
            if (_rebuildRequested && rebuildOnSplineChange && Time.time > _nextScheduledRebuild)
                Rebuild();
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (spline != Spline) return;
            if (rebuildOnSplineChange)
                _rebuildRequested = true;
        }

        private Terrain FindNearestTerrain()
        {
            var terrains = Terrain.activeTerrains;
            if (terrains.Length == 0)
                return null;

            Terrain nearest = terrains[0];
            float minDist = Vector3.Distance(transform.position, nearest.transform.position);

            for (int i = 1; i < terrains.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, terrains[i].transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = terrains[i];
                }
            }

            return nearest;
        }

        private bool IsNullOrEmptyContainer()
        {
            return !container || container.Splines == null || container.Splines.Count == 0;
        }

        public void Rebuild()
        {
            if (IsNullOrEmptyContainer())
                return;

            if (!terrain)
            {
                terrain = FindNearestTerrain();
                if (!terrain)
                {
                    Debug.LogWarning("SplineToTerrain: No terrain found.", this);
                    return;
                }
            }

            if (snapSplineToTerrain)
                SnapSplinePointsToTerrain();

            if (deformTerrainToSpline)
                DeformTerrainAlongSpline();

            if (paintTerrainLayer)
                PaintTerrainLayerAlongSpline();

            _nextScheduledRebuild = Time.time + 1f / rebuildFrequency;
            _rebuildRequested = false;
        }

        private void SnapSplinePointsToTerrain()
        {
            var spline = Spline;
            if (spline == null)
                return;

            for (int i = 0; i < spline.Count; i++)
            {
                var knot = spline[i];
                var worldPos = container.transform.TransformPoint(knot.Position);

                float terrainHeight = terrain.SampleHeight(worldPos) + terrain.transform.position.y;
                worldPos.y = terrainHeight + splineHeightOffset;

                var localPos = container.transform.InverseTransformPoint(worldPos);
                knot.Position = localPos;
                spline[i] = knot;
            }
        }

        private static float SmoothstepFalloff(float distance, float fullStrengthWidth, float smoothingDistance)
        {
            if (distance <= fullStrengthWidth) return 1f;
            float t = (distance - fullStrengthWidth) / smoothingDistance;
            return 1f - (t * t * (3f - 2f * t));
        }

        private void DeformTerrainAlongSpline()
        {
            var spline = Spline;
            if (spline == null)
                return;

            var terrainData = terrain.terrainData;
            int heightmapWidth = terrainData.heightmapResolution;
            int heightmapHeight = terrainData.heightmapResolution;

            float splineLength = spline.GetLength();
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(splineLength * samplesPerUnit));
            float totalInfluenceDistance = deformWidth + smoothingDistance;
            int radius = Mathf.CeilToInt((totalInfluenceDistance / terrainData.size.x) * heightmapWidth);

            var samples = new (int centerX, int centerZ, float targetHeight)[sampleCount];
            int minX = heightmapWidth, maxX = 0, minZ = heightmapHeight, maxZ = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                var worldSplinePoint = container.transform.TransformPoint(spline.EvaluatePosition(t));

                var worldTangent = container.transform.TransformDirection(spline.EvaluateTangent(t)).normalized;
                var worldRight = Vector3.Cross(Vector3.up, worldTangent).normalized;

                worldSplinePoint += worldTangent * terrainOffset.x +
                                    Vector3.up * terrainOffset.y +
                                    worldRight * terrainOffset.z;

                Vector3 terrainLocalPos = worldSplinePoint - terrain.transform.position;
                float xNormalized = terrainLocalPos.x / terrainData.size.x;
                float zNormalized = terrainLocalPos.z / terrainData.size.z;

                int centerX = Mathf.RoundToInt(xNormalized * (heightmapWidth - 1));
                int centerZ = Mathf.RoundToInt(zNormalized * (heightmapHeight - 1));
                float targetHeight = terrainLocalPos.y / terrainData.size.y;

                samples[i] = (centerX, centerZ, targetHeight);
                minX = Mathf.Min(minX, centerX - radius);
                maxX = Mathf.Max(maxX, centerX + radius);
                minZ = Mathf.Min(minZ, centerZ - radius);
                maxZ = Mathf.Max(maxZ, centerZ + radius);
            }

            minX = Mathf.Clamp(minX, 0, heightmapWidth - 1);
            maxX = Mathf.Clamp(maxX, 0, heightmapWidth - 1);
            minZ = Mathf.Clamp(minZ, 0, heightmapHeight - 1);
            maxZ = Mathf.Clamp(maxZ, 0, heightmapHeight - 1);

            int rectWidth = maxX - minX + 1;
            int rectHeight = maxZ - minZ + 1;
            if (rectWidth <= 0 || rectHeight <= 0)
                return;

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(terrainData, "Deform Terrain to Spline");
#endif

            float[,] heights = terrainData.GetHeights(minX, minZ, rectWidth, rectHeight);

            foreach (var (centerX, centerZ, targetHeight) in samples)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int heightmapX = centerX + x;
                        int heightmapZ = centerZ + z;

                        if (heightmapX < minX || heightmapX > maxX || heightmapZ < minZ || heightmapZ > maxZ)
                            continue;

                        float xWorldDist = (x / (float)heightmapWidth) * terrainData.size.x;
                        float zWorldDist = (z / (float)heightmapHeight) * terrainData.size.z;
                        float worldDistance = Mathf.Sqrt(xWorldDist * xWorldDist + zWorldDist * zWorldDist);

                        if (worldDistance > totalInfluenceDistance)
                            continue;

                        float falloff = SmoothstepFalloff(worldDistance, deformWidth, smoothingDistance);

                        int localX = heightmapX - minX;
                        int localZ = heightmapZ - minZ;
                        float currentHeight = heights[localZ, localX];
                        heights[localZ, localX] = Mathf.Lerp(currentHeight, targetHeight, falloff * deformStrength);
                    }
                }
            }

            terrainData.SetHeights(minX, minZ, heights);
        }

        private void PaintTerrainLayerAlongSpline()
        {
            var spline = Spline;
            if (spline == null)
                return;

            if (!terrainLayer)
            {
                Debug.LogWarning("SplineToTerrain: No terrain layer assigned.", this);
                return;
            }

            var terrainData = terrain.terrainData;
            var terrainLayers = terrainData.terrainLayers;

            int layerIndex = -1;
            for (int i = 0; i < terrainLayers.Length; i++)
            {
                if (terrainLayers[i] == terrainLayer)
                {
                    layerIndex = i;
                    break;
                }
            }

            if (layerIndex == -1)
            {
                Debug.LogWarning($"SplineToTerrain: Layer '{terrainLayer.name}' is not present on the target terrain.", this);
                return;
            }

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int layerCount = terrainData.alphamapLayers;

            float splineLength = spline.GetLength();
            int sampleCount = Mathf.Max(2, Mathf.CeilToInt(splineLength * samplesPerUnit));
            float totalInfluenceDistance = paintWidth + paintSmoothingDistance;
            int radius = Mathf.CeilToInt((totalInfluenceDistance / terrainData.size.x) * alphamapWidth);

            var centers = new (int centerX, int centerZ)[sampleCount];
            int minX = alphamapWidth, maxX = 0, minZ = alphamapHeight, maxZ = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                var worldPoint = container.transform.TransformPoint(spline.EvaluatePosition(t));

                Vector3 terrainLocalPos = worldPoint - terrain.transform.position;
                float xNorm = terrainLocalPos.x / terrainData.size.x;
                float zNorm = terrainLocalPos.z / terrainData.size.z;

                int centerX = Mathf.RoundToInt(xNorm * (alphamapWidth - 1));
                int centerZ = Mathf.RoundToInt(zNorm * (alphamapHeight - 1));

                centers[i] = (centerX, centerZ);
                minX = Mathf.Min(minX, centerX - radius);
                maxX = Mathf.Max(maxX, centerX + radius);
                minZ = Mathf.Min(minZ, centerZ - radius);
                maxZ = Mathf.Max(maxZ, centerZ + radius);
            }

            minX = Mathf.Clamp(minX, 0, alphamapWidth - 1);
            maxX = Mathf.Clamp(maxX, 0, alphamapWidth - 1);
            minZ = Mathf.Clamp(minZ, 0, alphamapHeight - 1);
            maxZ = Mathf.Clamp(maxZ, 0, alphamapHeight - 1);

            int rectWidth = maxX - minX + 1;
            int rectHeight = maxZ - minZ + 1;
            if (rectWidth <= 0 || rectHeight <= 0)
                return;

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCompleteObjectUndo(terrainData, "Paint Terrain Layer Along Spline");
#endif

            float[,,] alphamaps = terrainData.GetAlphamaps(minX, minZ, rectWidth, rectHeight);

            foreach (var (centerX, centerZ) in centers)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        int ax = centerX + x;
                        int az = centerZ + z;

                        if (ax < minX || ax > maxX || az < minZ || az > maxZ)
                            continue;

                        float xWorldDist = (x / (float)alphamapWidth) * terrainData.size.x;
                        float zWorldDist = (z / (float)alphamapHeight) * terrainData.size.z;
                        float worldDistance = Mathf.Sqrt(xWorldDist * xWorldDist + zWorldDist * zWorldDist);

                        if (worldDistance > totalInfluenceDistance)
                            continue;

                        float falloff = SmoothstepFalloff(worldDistance, paintWidth, paintSmoothingDistance);

                        int localX = ax - minX;
                        int localZ = az - minZ;

                        float oldValue = alphamaps[localZ, localX, layerIndex];
                        float newValue = Mathf.Lerp(oldValue, 1f, falloff * paintStrength);
                        float remaining = 1f - newValue;
                        float previousOthers = 1f - oldValue;

                        alphamaps[localZ, localX, layerIndex] = newValue;

                        for (int l = 0; l < layerCount; l++)
                        {
                            if (l == layerIndex) continue;
                            alphamaps[localZ, localX, l] = previousOthers > 0f
                                ? alphamaps[localZ, localX, l] * (remaining / previousOthers)
                                : 0f;
                        }
                    }
                }
            }

            terrainData.SetAlphamaps(minX, minZ, alphamaps);
        }

#if UNITY_EDITOR

        public void SetSplineContainerOnGO()
        {
            if (!container && TryGetComponent<SplineContainer>(out var foundContainer))
                container = foundContainer;
        }

        public void Reset()
        {
            SetSplineContainerOnGO();
            if (!terrain)
                terrain = FindNearestTerrain();
            Rebuild();
        }
#endif
    }
}
#endif
