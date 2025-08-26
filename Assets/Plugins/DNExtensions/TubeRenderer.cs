using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace DNExtensions
{
    public enum RadiusMode
    {
        Single,
        StartEnd,
        Curve
    }

    [ExecuteInEditMode]
    [SelectionBase]
    public class TubeRenderer : MonoBehaviour
    {
        [Header("Tube Settings")] [Tooltip("Number of sides around the tube circumference")] [SerializeField]
        private int sides = 8;

        [Tooltip("Close the start cap of the tube")] [SerializeField]
        private bool closeStartCap;

        [Tooltip("Close the end cap of the tube")] [SerializeField]
        private bool closeEndCap;

        [Tooltip("How the tube's radius is determined along its length")] [SerializeField]
        private RadiusMode radiusMode = RadiusMode.Single;

        [Tooltip("Animation curve controlling the radius when using Curve mode")]
        [SerializeField, ShowIf("radiusMode", RadiusMode.Curve)]
        private AnimationCurve radiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [Tooltip("Base radius value")] [SerializeField, HideIf("radiusMode", RadiusMode.Curve)]
        private float radiusOne = 1.0f;
        
        [Tooltip("End radius value (used only in StartEnd mode)")]
        [SerializeField, ShowIf("radiusMode", RadiusMode.StartEnd)]
        private float radiusTwo = 1.0f;

        [Header("Sharp Corner Handling")] [Tooltip("Enable smoothing of sharp corners")] [SerializeField]
        private bool enableCornerSmoothing = true;

        [Tooltip("Angle (in degrees) above which a corner is considered 'sharp' and will be smoothed")]
        [Range(20f, 160f)]
        [SerializeField, ShowIf("enableCornerSmoothing")]
        private float sharpAngleThreshold = 60f;


        [Tooltip("Number of segments to create when smoothing a sharp corner")]
        [Range(1, 8)]
        [SerializeField, ShowIf("enableCornerSmoothing")]
        private int cornerSmoothingSegments = 3;


        [Tooltip("How far to extend the smoothing before and after a sharp corner (0-1)")]
        [Range(0.1f, 0.5f)]
        [SerializeField, ShowIf("enableCornerSmoothing")]
        private float cornerSmoothingExtent = 0.3f;
        

#if UNITY_EDITOR
        [Tooltip("Whether to visualize the processed path points for debugging")]
        [SerializeField, ShowIf("enableCornerSmoothing")]
        private bool showProcessedPath = true;
        
#endif
        [Header("Orientation Control")]
        [Tooltip("Force a specific up direction for the tube (prevents twisting)")]
        [SerializeField]
        private bool useStableUpVector = true;

        [Tooltip("The up vector to use for consistent tube orientation")] [SerializeField, ShowIf("useStableUpVector")]
        private Vector3 upVector = Vector3.up;

        [Tooltip("Array of points defining the tube's path")] [SerializeField]
        private Vector3[] positions;

        private Vector3[] _vertices;
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private bool _meshNeedsRebuild = true;

        // Smoothed path with extra points for sharp angles
        private Vector3[] _processedPath;

        // Original point indices in the processed path (for UV mapping)
        private float[] _pathT;

        // Cached frames (position, tangent, normal, binormal) along the path for consistent orientation
        private Frame[] _frames;

        // Represents a frame (position, tangent, normal, binormal) at a point along the path
        private struct Frame
        {
            public Vector3 Position;
            public Vector3 Tangent;
            public Vector3 Normal;
            public Vector3 Binormal;
        }

        public Material Material
        {
            get => _meshRenderer.material;
            set => _meshRenderer.material = value;
        }

        public Vector3[] Positions
        {
            get => positions;
            set
            {
                positions = value;
                _meshNeedsRebuild = true;
            }
        }

        public bool CloseStartCap
        {
            get => closeStartCap;
            set
            {
                closeStartCap = value;
                _meshNeedsRebuild = true;
            }
        }

        public bool CloseEndCap
        {
            get => closeEndCap;
            set
            {
                closeEndCap = value;
                _meshNeedsRebuild = true;
            }
        }

        public bool EnableCornerSmoothing
        {
            get => enableCornerSmoothing;
            set
            {
                enableCornerSmoothing = value;
                _meshNeedsRebuild = true;
            }
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void Reset()
        {
            positions = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 1)
            };
        }

        private void OnEnable()
        {
            _meshRenderer.enabled = true;
        }

        private void OnDisable()
        {
            _meshRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_mesh);
                }
                else
                {
                    DestroyImmediate(_mesh);
                }

                _mesh = null;
            }
        }

        private void Update()
        {
            GenerateMesh();
        }

        private void OnValidate()
        {
            sides = Mathf.Max(3, sides);
            _meshNeedsRebuild = true;

            // Normalize up vector if it's used
            if (useStableUpVector && upVector != Vector3.zero)
            {
                upVector.Normalize();
            }
        }

        public void SetPositions(Vector3[] newPositions)
        {
            positions = newPositions;
            _meshNeedsRebuild = true;
            GenerateMesh();
        }

        private void InitializeComponents()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (!_meshFilter)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            _meshRenderer = GetComponent<MeshRenderer>();
            if (!_meshRenderer)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (!_mesh)
            {
                _mesh = new Mesh
                {
                    name = "TubeMesh"
                };
                _meshFilter.mesh = _mesh;
            }
        }

        private void ProcessPath()
        {
            // If smoothing is disabled, or we don't have enough points, use original path
            if (!enableCornerSmoothing || positions == null || positions.Length < 3)
            {
                _processedPath = positions;
                if (_processedPath is { Length: > 0 })
                {
                    _pathT = new float[_processedPath.Length];
                    for (int i = 0; i < _processedPath.Length; i++)
                    {
                        _pathT[i] = i / (float)(_processedPath.Length - 1);
                    }
                }

                return;
            }

            // Find corners that need smoothing
            List<int> sharpCorners = new List<int>();
            for (int i = 1; i < positions.Length - 1; i++)
            {
                Vector3 prevDir = (positions[i] - positions[i - 1]).normalized;
                Vector3 nextDir = (positions[i + 1] - positions[i]).normalized;

                float angle = Vector3.Angle(prevDir, nextDir);
                if (angle > sharpAngleThreshold)
                {
                    sharpCorners.Add(i);
                }
            }

            // If no sharp corners, use original path
            if (sharpCorners.Count == 0)
            {
                _processedPath = positions;
                _pathT = new float[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    _pathT[i] = i / (float)(positions.Length - 1);
                }

                return;
            }

            // Create a new path with additional points at sharp corners
            List<Vector3> processedList = new List<Vector3>();
            List<float> pathTList = new List<float>();

            // Add the first point
            processedList.Add(positions[0]);
            pathTList.Add(0f);

            // Process each corner
            for (int i = 1; i < positions.Length; i++)
            {
                if (sharpCorners.Contains(i))
                {
                    // Get previous and next positions
                    Vector3 prev = positions[i - 1];
                    Vector3 corner = positions[i];
                    Vector3 next = positions[i + 1];

                    // Get directions for calculating the new points
                    Vector3 prevDir = (corner - prev).normalized;
                    Vector3 nextDir = (next - corner).normalized;

                    float prevDist = Vector3.Distance(prev, corner);
                    float nextDist = Vector3.Distance(corner, next);

                    // Calculate control points for smoothing
                    float extentToPrev = Mathf.Min(cornerSmoothingExtent * prevDist, prevDist * 0.5f);
                    float extentToNext = Mathf.Min(cornerSmoothingExtent * nextDist, nextDist * 0.5f);

                    // Start point of the curve (slightly before the corner)
                    Vector3 curveStart = corner - prevDir * extentToPrev;
                    // End point of the curve (slightly after the corner)
                    Vector3 curveEnd = corner + nextDir * extentToNext;

                    // Control point at the original corner
                    Vector3 controlPoint = corner;

                    // Calculate original path t value for this corner
                    float cornerT = i / (float)(positions.Length - 1);

                    // Add the point just before the corner
                    processedList.Add(curveStart);
                    pathTList.Add(cornerT - cornerSmoothingExtent / (positions.Length - 1));

                    // Add smoothed corner points
                    for (int j = 1; j <= cornerSmoothingSegments; j++)
                    {
                        float t = j / (float)(cornerSmoothingSegments + 1);
                        Vector3 point = QuadraticBezier(curveStart, controlPoint, curveEnd, t);

                        processedList.Add(point);
                        pathTList.Add(cornerT);
                    }

                    // Add the point just after the corner
                    processedList.Add(curveEnd);
                    pathTList.Add(cornerT + cornerSmoothingExtent / (positions.Length - 1));
                }
                else if (!sharpCorners.Contains(i - 1) || i == positions.Length - 1)
                {
                    // Only add non-corner points if the previous point wasn't a sharp corner
                    // or if this is the last point (always add that)
                    processedList.Add(positions[i]);
                    pathTList.Add(i / (float)(positions.Length - 1));
                }
            }

            _processedPath = processedList.ToArray();
            _pathT = pathTList.ToArray();
        }

        private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            return uu * p0 + 2 * u * t * p1 + tt * p2;
        }

        private void CalculatePathFrames()
        {
            if (_processedPath == null || _processedPath.Length <= 1)
                return;

            int pathLength = _processedPath.Length;
            _frames = new Frame[pathLength];

            // First pass: Calculate tangents
            for (int i = 0; i < pathLength; i++)
            {
                _frames[i].Position = _processedPath[i];

                if (i == 0)
                {
                    // First point - use direction to next point
                    _frames[i].Tangent = (_processedPath[i + 1] - _processedPath[i]).normalized;
                }
                else if (i == pathLength - 1)
                {
                    // Last point - use direction from previous point
                    _frames[i].Tangent = (_processedPath[i] - _processedPath[i - 1]).normalized;
                }
                else
                {
                    // Middle points - average of adjacent segments
                    Vector3 prevDir = (_processedPath[i] - _processedPath[i - 1]).normalized;
                    Vector3 nextDir = (_processedPath[i + 1] - _processedPath[i]).normalized;

                    // Average the directions
                    _frames[i].Tangent = (prevDir + nextDir).normalized;
                }
            }

            // Find the tube's overall dominant direction, used for consistent initial frame
            Vector3 dominantDirection = (_processedPath[pathLength - 1] - _processedPath[0]).normalized;

            // Determine initial normal vector
            Vector3 initialNormal;
            if (useStableUpVector)
            {
                // Ensure upVector is a unit vector
                Vector3 upVectorNormalized = upVector.normalized;

                // Generate a normal perpendicular to both the tangent and up vector
                Vector3 bitangent = Vector3.Cross(_frames[0].Tangent, upVectorNormalized).normalized;

                // If they're nearly parallel, use a fallback vector
                if (bitangent.magnitude < 0.01f)
                {
                    // Find a non-parallel vector to use
                    Vector3 fallbackDir = Mathf.Abs(Vector3.Dot(_frames[0].Tangent, Vector3.right)) > 0.9f
                        ? Vector3.up
                        : Vector3.right;

                    bitangent = Vector3.Cross(_frames[0].Tangent, fallbackDir).normalized;
                }

                // Now compute the normal that's perpendicular to the tangent and aligns with the up vector
                initialNormal = Vector3.Cross(bitangent, _frames[0].Tangent).normalized;
            }
            else
            {
                // Without stable up vector, use a reasonable initial normal that's perpendicular to the tangent
                Vector3 referenceVector = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(_frames[0].Tangent, referenceVector)) > 0.9f)
                {
                    referenceVector = Vector3.right;
                }

                initialNormal = Vector3.Cross(Vector3.Cross(_frames[0].Tangent, referenceVector).normalized,
                    _frames[0].Tangent).normalized;
            }

            // Apply the initial frame
            _frames[0].Normal = initialNormal;
            _frames[0].Binormal = Vector3.Cross(_frames[0].Tangent, _frames[0].Normal).normalized;

            // Second pass: Propagate the frames using parallel transport
            for (int i = 1; i < pathLength; i++)
            {
                // Implementing parallel transport - keep the normal as perpendicular as possible between segments
                Vector3 prevTangent = _frames[i - 1].Tangent;
                Vector3 currTangent = _frames[i].Tangent;

                // If tangents are nearly the same, just copy the previous frame
                if (Vector3.Dot(prevTangent, currTangent) > 0.99999f)
                {
                    _frames[i].Normal = _frames[i - 1].Normal;
                    _frames[i].Binormal = _frames[i - 1].Binormal;
                    continue;
                }

                // Rotate the previous normal to be perpendicular to the current tangent
                // This method minimizes the twist between segments
                Quaternion rotation = Quaternion.FromToRotation(prevTangent, currTangent);
                _frames[i].Normal = rotation * _frames[i - 1].Normal;

                // Ensure the normal is exactly perpendicular to the tangent
                _frames[i].Normal = Vector3.Cross(Vector3.Cross(currTangent, _frames[i].Normal).normalized, currTangent)
                    .normalized;

                // Calculate binormal to complete the orthonormal frame
                _frames[i].Binormal = Vector3.Cross(currTangent, _frames[i].Normal).normalized;

                // Additional stabilization for sharp turns - if using stable up vector
                if (useStableUpVector)
                {
                    // Check if we're at a sharp corner
                    float angle = Vector3.Angle(prevTangent, currTangent);
                    if (angle > 45f) // Arbitrary threshold for sharp corners
                    {
                        // Recalculate normal to align better with up vector at sharp corners
                        Vector3 upVectorNormalized = upVector.normalized;
                        Vector3 bitangent = Vector3.Cross(currTangent, upVectorNormalized).normalized;

                        // Skip if too parallel
                        if (bitangent.magnitude > 0.01f)
                        {
                            Vector3 alignedNormal = Vector3.Cross(bitangent, currTangent).normalized;

                            // Blend between parallel transport normal and aligned normal based on angle sharpness
                            float blendFactor = Mathf.Clamp01((angle - 45f) / 45f); // 0 at 45°, 1 at 90°
                            _frames[i].Normal = Vector3.Slerp(_frames[i].Normal, alignedNormal, blendFactor);
                            _frames[i].Normal = _frames[i].Normal.normalized;

                            // Recalculate binormal
                            _frames[i].Binormal = Vector3.Cross(currTangent, _frames[i].Normal).normalized;
                        }
                    }
                }
            }
        }

        private void GenerateMesh()
        {
            // If we don't have enough points to create a tube
            if (!_mesh || positions == null || positions.Length <= 1)
            {
                if (_mesh)
                {
                    _mesh.Clear();
                }

                return;
            }

            // Process the path to handle sharp angles
            ProcessPath();

            if (_processedPath == null || _processedPath.Length <= 1)
            {
                if (_mesh)
                {
                    _mesh.Clear();
                }

                return;
            }

            // Calculate path frames for consistent tube orientation
            CalculatePathFrames();

            // Calculate the number of additional vertices needed for caps
            int capVertices = 0;
            if (closeStartCap) capVertices += 1; // Center vertex for start cap
            if (closeEndCap) capVertices += 1; // Center vertex for end cap

            // Calculate the vertices length based on positions and sides
            var verticesLength = sides * _processedPath.Length + capVertices;

            // Check if we need to rebuild arrays
            if (_vertices == null || _vertices.Length != verticesLength || _meshNeedsRebuild)
            {
                _vertices = new Vector3[verticesLength];

                // Generate indices and UVs with cap consideration
                var indices = GenerateIndices(_processedPath.Length);
                var uvs = GenerateUVs(_processedPath.Length);

                _mesh.Clear();
                _mesh.vertices = _vertices;
                _mesh.triangles = indices;
                _mesh.uv = uvs;

                _meshNeedsRebuild = false;
            }

            // Generate the mesh vertices
            var currentVertIndex = 0;
            for (int i = 0; i < _processedPath.Length; i++)
            {
                var circle = CalculateCircle(i);
                foreach (var vertex in circle)
                {
                    _vertices[currentVertIndex++] = vertex;
                }
            }

            // Add cap center vertices
            if (closeStartCap)
            {
                _vertices[currentVertIndex++] = _processedPath[0];
            }

            if (closeEndCap)
            {
                _vertices[currentVertIndex++] = _processedPath[^1];
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _meshFilter.mesh = _mesh;
        }

        private Vector2[] GenerateUVs(int positionCount)
        {
            // Calculate total vertices including caps
            int totalVertices = positionCount * sides;
            if (closeStartCap) totalVertices += 1;
            if (closeEndCap) totalVertices += 1;

            var uvs = new Vector2[totalVertices];

            // UVs for tube body - using _pathT to map UVs based on the original path proportions
            for (int segment = 0; segment < positionCount; segment++)
            {
                for (int side = 0; side < sides; side++)
                {
                    var vertIndex = (segment * sides + side);
                    var u = side / (float)(sides - 1);
                    var v = _pathT[segment]; // Use the mapped t-value instead of linear interpolation

                    uvs[vertIndex] = new Vector2(u, v);
                }
            }

            // UVs for caps (center of UV space)
            int capStartIndex = positionCount * sides;
            if (closeStartCap)
            {
                uvs[capStartIndex] = new Vector2(0.5f, 0);
            }

            if (closeEndCap)
            {
                int endCapIndex = capStartIndex;
                if (closeStartCap) endCapIndex++;
                uvs[endCapIndex] = new Vector2(0.5f, 1);
            }

            return uvs;
        }

        private int[] GenerateIndices(int positionCount)
        {
            // Calculate number of triangles
            int tubeTriangles = (positionCount - 1) * sides * 2;
            int capTriangles = 0;

            if (closeStartCap) capTriangles += sides;
            if (closeEndCap) capTriangles += sides;

            int totalTriangles = tubeTriangles + capTriangles;

            // Each triangle has 3 indices
            var indices = new int[totalTriangles * 3];

            // Generate indices for tube body
            var currentIndicesIndex = 0;
            for (int segment = 1; segment < positionCount; segment++)
            {
                for (int side = 0; side < sides; side++)
                {
                    var vertIndex = (segment * sides + side);
                    var prevVertIndex = vertIndex - sides;

                    // Get the next side index, wrapping around at the end
                    int nextSide = (side + 1) % sides;
                    int nextVertIndex = segment * sides + nextSide;
                    int nextPrevVertIndex = (segment - 1) * sides + nextSide;

                    // Triangle one
                    indices[currentIndicesIndex++] = prevVertIndex;
                    indices[currentIndicesIndex++] = nextPrevVertIndex;
                    indices[currentIndicesIndex++] = vertIndex;

                    // Triangle two
                    indices[currentIndicesIndex++] = nextPrevVertIndex;
                    indices[currentIndicesIndex++] = nextVertIndex;
                    indices[currentIndicesIndex++] = vertIndex;
                }
            }

            // Generate indices for start cap - CORRECTED WINDING ORDER
            if (closeStartCap)
            {
                int centerVertexIndex = positionCount * sides;

                for (int side = 0; side < sides; side++)
                {
                    // Get the next side index, wrapping around at the end
                    int nextSide = (side + 1) % sides;

                    // Reversed winding order for start cap so it faces outward
                    indices[currentIndicesIndex++] = centerVertexIndex;
                    indices[currentIndicesIndex++] = nextSide;
                    indices[currentIndicesIndex++] = side;
                }
            }

            // Generate indices for end cap - CORRECTED WINDING ORDER
            if (closeEndCap)
            {
                int centerVertexIndex = positionCount * sides;
                if (closeStartCap) centerVertexIndex++;

                int lastRingStartIndex = (positionCount - 1) * sides;

                for (int side = 0; side < sides; side++)
                {
                    // Get the next side index, wrapping around at the end
                    int nextSide = (side + 1) % sides;

                    // Correct winding order for end cap so it faces outward
                    indices[currentIndicesIndex++] = centerVertexIndex;
                    indices[currentIndicesIndex++] = lastRingStartIndex + side;
                    indices[currentIndicesIndex++] = lastRingStartIndex + nextSide;
                }
            }

            return indices;
        }

        private Vector3[] CalculateCircle(int index)
        {
            var circle = new Vector3[sides];
            var angleStep = (2 * Mathf.PI) / sides;

            // Get t-value for radius calculation (using _pathT for correct interpolation)
            var t = _pathT[index];

            // Calculate radius based on the selected mode
            float radius;
            switch (radiusMode)
            {
                case RadiusMode.StartEnd:
                    radius = Mathf.Lerp(radiusOne, radiusTwo, t);
                    break;
                case RadiusMode.Curve:
                    radius = radiusCurve.Evaluate(t);
                    break;
                case RadiusMode.Single:
                default:
                    radius = radiusOne;
                    break;
            }

            // Use the precalculated frame for this point
            Vector3 position = _frames[index].Position;
            Vector3 normal = _frames[index].Normal;
            Vector3 binormal = _frames[index].Binormal;

            // Create the circle points
            for (int i = 0; i < sides; i++)
            {
                float angle = i * angleStep;
                float cosA = Mathf.Cos(angle);
                float sinA = Mathf.Sin(angle);

                circle[i] = position + (normal * cosA + binormal * sinA) * radius;
            }

            return circle;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (positions == null || positions.Length <= 1) return;

            // Draw sphere for each point, properly transformed by object's rotation and scale
            Gizmos.color = Color.red;
            float sphereSize = 0.1f;

            foreach (var localPosition in positions)
            {
                // Transform the point from local space to world space
                Vector3 worldPosition = transform.TransformPoint(localPosition);

                Gizmos.DrawSphere(worldPosition, sphereSize);

                // Only draw labels if there aren't too many points
                if (positions.Length < 20)
                {
                    Handles.Label(worldPosition + Vector3.up * 0.2f,
                        $"Point: {Array.IndexOf(positions, localPosition)}");
                }
            }

            // Draw lines between original points
            Gizmos.color = Color.yellow;
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Vector3 from = transform.TransformPoint(positions[i]);
                Vector3 to = transform.TransformPoint(positions[i + 1]);
                Gizmos.DrawLine(from, to);
            }

            // Draw processed path points if smoothing is enabled
            if (enableCornerSmoothing && showProcessedPath && _processedPath is { Length: > 1 })
            {
                Gizmos.color = Color.green;
                float smallSphereSize = 0.05f;

                // Draw lines along processed path
                for (int i = 0; i < _processedPath.Length - 1; i++)
                {
                    Vector3 from = transform.TransformPoint(_processedPath[i]);
                    Vector3 to = transform.TransformPoint(_processedPath[i + 1]);
                    Gizmos.DrawLine(from, to);
                }

                // Draw added points (ones that aren't in the original path)
                foreach (var position in _processedPath)
                {
                    // Check if this is an original point or an added one
                    bool isOriginal = false;
                    foreach (var originalPos in positions)
                    {
                        if (Vector3.Distance(position, originalPos) < 0.001f)
                        {
                            isOriginal = true;
                            break;
                        }
                    }

                    if (!isOriginal)
                    {
                        Vector3 worldPos = transform.TransformPoint(position);
                        Gizmos.DrawSphere(worldPos, smallSphereSize);
                    }
                }
            }

            // Draw the frame vectors if frames are calculated
            if (_frames is { Length: > 0 })
            {
                float arrowLength = 0.2f;

                // Draw frame vectors at every few points (to avoid clutter)
                int step = Mathf.Max(1, _frames.Length / 10);
                for (int i = 0; i < _frames.Length; i += step)
                {
                    Vector3 worldPos = transform.TransformPoint(_frames[i].Position);

                    // Draw tangent vector (forward) in blue
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(worldPos, transform.TransformDirection(_frames[i].Tangent) * arrowLength);

                    // Draw normal vector in red
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(worldPos, transform.TransformDirection(_frames[i].Normal) * arrowLength);

                    // Draw binormal vector in green
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(worldPos, transform.TransformDirection(_frames[i].Binormal) * arrowLength);
                }
            }
        }
#endif
    }
}