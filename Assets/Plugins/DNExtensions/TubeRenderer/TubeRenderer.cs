using UnityEngine;

namespace DNExtensions.TubeRenderer
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
        [SerializeField] private int sides = 8;
        [SerializeField] private bool closeStartCap;
        [SerializeField] private bool closeEndCap;
        [SerializeField] private RadiusMode radiusMode = RadiusMode.Single;
        [SerializeField] private float radiusOne = 1.0f;
        [SerializeField] private float radiusTwo = 1.0f;
        [SerializeField] private AnimationCurve radiusCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private bool enableCornerSmoothing = true;
        [SerializeField] private float sharpAngleThreshold = 60f;
        [SerializeField] private int cornerSmoothingSegments = 3;
        [SerializeField] private float cornerSmoothingExtent = 0.3f;
        [SerializeField] private bool useStableUpVector = true;
        [SerializeField] private Vector3 upVector = Vector3.up;
        [SerializeField] private Vector3[] positions;


        private Vector3[] _vertices;
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private bool _meshNeedsRebuild = true;
        private Vector3[] _processedPath;
        private float[] _pathT;
        private Frame[] _frames;

        private struct Frame
        {
            public Vector3 Position;
            public Vector3 Tangent;
            public Vector3 Normal;
            public Vector3 Binormal;
        }

        public Material Material
        {
            get => _meshRenderer.sharedMaterial;
            set => _meshRenderer.sharedMaterial = value;
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
                _mesh = new Mesh { name = "TubeMesh" };
                _meshFilter.mesh = _mesh;
            }
        }

        private void ProcessPath()
        {
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

            System.Collections.Generic.List<int> sharpCorners = new System.Collections.Generic.List<int>();
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

            System.Collections.Generic.List<Vector3> processedList = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<float> pathTList = new System.Collections.Generic.List<float>();

            processedList.Add(positions[0]);
            pathTList.Add(0f);

            for (int i = 1; i < positions.Length; i++)
            {
                if (sharpCorners.Contains(i))
                {
                    Vector3 prev = positions[i - 1];
                    Vector3 corner = positions[i];
                    Vector3 next = positions[i + 1];

                    Vector3 prevDir = (corner - prev).normalized;
                    Vector3 nextDir = (next - corner).normalized;

                    float prevDist = Vector3.Distance(prev, corner);
                    float nextDist = Vector3.Distance(corner, next);

                    float extentToPrev = Mathf.Min(cornerSmoothingExtent * prevDist, prevDist * 0.5f);
                    float extentToNext = Mathf.Min(cornerSmoothingExtent * nextDist, nextDist * 0.5f);

                    Vector3 curveStart = corner - prevDir * extentToPrev;
                    Vector3 curveEnd = corner + nextDir * extentToNext;
                    Vector3 controlPoint = corner;

                    float cornerT = i / (float)(positions.Length - 1);

                    processedList.Add(curveStart);
                    pathTList.Add(cornerT - cornerSmoothingExtent / (positions.Length - 1));

                    for (int j = 1; j <= cornerSmoothingSegments; j++)
                    {
                        float t = j / (float)(cornerSmoothingSegments + 1);
                        Vector3 point = QuadraticBezier(curveStart, controlPoint, curveEnd, t);
                        processedList.Add(point);
                        pathTList.Add(cornerT);
                    }

                    processedList.Add(curveEnd);
                    pathTList.Add(cornerT + cornerSmoothingExtent / (positions.Length - 1));
                }
                else if (!sharpCorners.Contains(i - 1) || i == positions.Length - 1)
                {
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
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        private void CalculatePathFrames()
        {
            if (_processedPath == null || _processedPath.Length <= 1)
                return;

            int pathLength = _processedPath.Length;
            _frames = new Frame[pathLength];

            for (int i = 0; i < pathLength; i++)
            {
                _frames[i].Position = _processedPath[i];

                if (i == 0)
                {
                    _frames[i].Tangent = (_processedPath[i + 1] - _processedPath[i]).normalized;
                }
                else if (i == pathLength - 1)
                {
                    _frames[i].Tangent = (_processedPath[i] - _processedPath[i - 1]).normalized;
                }
                else
                {
                    Vector3 prevDir = (_processedPath[i] - _processedPath[i - 1]).normalized;
                    Vector3 nextDir = (_processedPath[i + 1] - _processedPath[i]).normalized;
                    _frames[i].Tangent = (prevDir + nextDir).normalized;
                }
            }

            Vector3 initialNormal;
            if (useStableUpVector)
            {
                Vector3 upVectorNormalized = upVector.normalized;
                Vector3 bitangent = Vector3.Cross(_frames[0].Tangent, upVectorNormalized).normalized;

                if (bitangent.magnitude < 0.01f)
                {
                    Vector3 fallbackDir = Mathf.Abs(Vector3.Dot(_frames[0].Tangent, Vector3.right)) > 0.9f
                        ? Vector3.up
                        : Vector3.right;
                    bitangent = Vector3.Cross(_frames[0].Tangent, fallbackDir).normalized;
                }

                initialNormal = Vector3.Cross(bitangent, _frames[0].Tangent).normalized;
            }
            else
            {
                Vector3 referenceVector = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(_frames[0].Tangent, referenceVector)) > 0.9f)
                {
                    referenceVector = Vector3.right;
                }

                initialNormal = Vector3.Cross(Vector3.Cross(_frames[0].Tangent, referenceVector).normalized,
                    _frames[0].Tangent).normalized;
            }

            _frames[0].Normal = initialNormal;
            _frames[0].Binormal = Vector3.Cross(_frames[0].Tangent, _frames[0].Normal).normalized;

            for (int i = 1; i < pathLength; i++)
            {
                Vector3 prevTangent = _frames[i - 1].Tangent;
                Vector3 currTangent = _frames[i].Tangent;

                if (Vector3.Dot(prevTangent, currTangent) > 0.99999f)
                {
                    _frames[i].Normal = _frames[i - 1].Normal;
                    _frames[i].Binormal = _frames[i - 1].Binormal;
                    continue;
                }

                Quaternion rotation = Quaternion.FromToRotation(prevTangent, currTangent);
                _frames[i].Normal = rotation * _frames[i - 1].Normal;
                _frames[i].Normal = Vector3.Cross(Vector3.Cross(currTangent, _frames[i].Normal).normalized, currTangent).normalized;
                _frames[i].Binormal = Vector3.Cross(currTangent, _frames[i].Normal).normalized;

                if (useStableUpVector)
                {
                    float angle = Vector3.Angle(prevTangent, currTangent);
                    if (angle > 45f)
                    {
                        Vector3 upVectorNormalized = upVector.normalized;
                        Vector3 bitangent = Vector3.Cross(currTangent, upVectorNormalized).normalized;

                        if (bitangent.magnitude > 0.01f)
                        {
                            Vector3 alignedNormal = Vector3.Cross(bitangent, currTangent).normalized;
                            float blendFactor = Mathf.Clamp01((angle - 45f) / 45f);
                            _frames[i].Normal = Vector3.Slerp(_frames[i].Normal, alignedNormal, blendFactor).normalized;
                            _frames[i].Binormal = Vector3.Cross(currTangent, _frames[i].Normal).normalized;
                        }
                    }
                }
            }
        }

        private void GenerateMesh()
        {
            if (!_mesh || positions == null || positions.Length <= 1)
            {
                if (_mesh)
                {
                    _mesh.Clear();
                }
                return;
            }

            ProcessPath();

            if (_processedPath == null || _processedPath.Length <= 1)
            {
                if (_mesh)
                {
                    _mesh.Clear();
                }
                return;
            }

            CalculatePathFrames();

            int capVertices = 0;
            if (closeStartCap) capVertices += 1;
            if (closeEndCap) capVertices += 1;

            var verticesLength = sides * _processedPath.Length + capVertices;

            if (_vertices == null || _vertices.Length != verticesLength || _meshNeedsRebuild)
            {
                _vertices = new Vector3[verticesLength];
                var indices = GenerateIndices(_processedPath.Length);
                var uvs = GenerateUVs(_processedPath.Length);

                _mesh.Clear();
                _mesh.vertices = _vertices;
                _mesh.triangles = indices;
                _mesh.uv = uvs;

                _meshNeedsRebuild = false;
            }

            var currentVertIndex = 0;
            for (int i = 0; i < _processedPath.Length; i++)
            {
                var circle = CalculateCircle(i);
                foreach (var vertex in circle)
                {
                    _vertices[currentVertIndex++] = vertex;
                }
            }

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
            int totalVertices = positionCount * sides;
            if (closeStartCap) totalVertices += 1;
            if (closeEndCap) totalVertices += 1;

            var uvs = new Vector2[totalVertices];

            for (int segment = 0; segment < positionCount; segment++)
            {
                for (int side = 0; side < sides; side++)
                {
                    var vertIndex = (segment * sides + side);
                    var u = side / (float)(sides - 1);
                    var v = _pathT[segment];
                    uvs[vertIndex] = new Vector2(u, v);
                }
            }

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
            int tubeTriangles = (positionCount - 1) * sides * 2;
            int capTriangles = 0;

            if (closeStartCap) capTriangles += sides;
            if (closeEndCap) capTriangles += sides;

            int totalTriangles = tubeTriangles + capTriangles;
            var indices = new int[totalTriangles * 3];

            var currentIndicesIndex = 0;
            for (int segment = 1; segment < positionCount; segment++)
            {
                for (int side = 0; side < sides; side++)
                {
                    var vertIndex = (segment * sides + side);
                    var prevVertIndex = vertIndex - sides;

                    int nextSide = (side + 1) % sides;
                    int nextVertIndex = segment * sides + nextSide;
                    int nextPrevVertIndex = (segment - 1) * sides + nextSide;

                    indices[currentIndicesIndex++] = prevVertIndex;
                    indices[currentIndicesIndex++] = nextPrevVertIndex;
                    indices[currentIndicesIndex++] = vertIndex;

                    indices[currentIndicesIndex++] = nextPrevVertIndex;
                    indices[currentIndicesIndex++] = nextVertIndex;
                    indices[currentIndicesIndex++] = vertIndex;
                }
            }

            if (closeStartCap)
            {
                int centerVertexIndex = positionCount * sides;

                for (int side = 0; side < sides; side++)
                {
                    int nextSide = (side + 1) % sides;
                    indices[currentIndicesIndex++] = centerVertexIndex;
                    indices[currentIndicesIndex++] = nextSide;
                    indices[currentIndicesIndex++] = side;
                }
            }

            if (closeEndCap)
            {
                int centerVertexIndex = positionCount * sides;
                if (closeStartCap) centerVertexIndex++;

                int lastRingStartIndex = (positionCount - 1) * sides;

                for (int side = 0; side < sides; side++)
                {
                    int nextSide = (side + 1) % sides;
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
            var t = _pathT[index];

            float radius = radiusMode switch
            {
                RadiusMode.StartEnd => Mathf.Lerp(radiusOne, radiusTwo, t),
                RadiusMode.Curve => radiusCurve.Evaluate(t),
                _ => radiusOne
            };

            Vector3 position = _frames[index].Position;
            Vector3 normal = _frames[index].Normal;
            Vector3 binormal = _frames[index].Binormal;

            for (int i = 0; i < sides; i++)
            {
                float angle = i * angleStep;
                float cosA = Mathf.Cos(angle);
                float sinA = Mathf.Sin(angle);
                circle[i] = position + (normal * cosA + binormal * sinA) * radius;
            }

            return circle;
        }
    }
}