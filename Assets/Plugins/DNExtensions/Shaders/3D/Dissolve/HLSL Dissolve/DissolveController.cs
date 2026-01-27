using System.Collections;
using System.Collections.Generic;
using DNExtensions.Utilities;
using UnityEngine;


public class DissolveController : MonoBehaviour
{

    [Header("Effect Shape")]
    [Tooltip("Type of shape to use for the dissolve effect")]
    public InteractorShape shape = InteractorShape.Sphere;

    [Tooltip("Radius of effect for sphere shape")]
    [ShowIf("shape", InteractorShape.Sphere)] public float radius = 5.0f;

    [Tooltip("Size of the box (x, y, z dimensions)")]
    [ShowIf("shape", InteractorShape.Box)] public Vector3 boxSize = new Vector3(1.0f, 1.0f, 1.0f);

    [Tooltip("Rotation of the box in degrees")]
    [ShowIf("shape", InteractorShape.Box)] public Vector3 boxRotation = Vector3.zero;

    
    [Header("Animation")]
    [Tooltip("Animation speed")]
    public float animationSpeed = 1.0f;
    
    [ShowIf("shape", InteractorShape.Sphere)] 
    [Tooltip("Animate the radius over time")]
    public bool animateRadius;
    [ShowIf("shape", InteractorShape.Sphere)] 
    [Tooltip("Minimum radius value")]
    public float minRadius;
    [ShowIf("shape", InteractorShape.Sphere)] 
    [Tooltip("Maximum radius value")]
    public float maxRadius = 10.0f;
    
    [ShowIf("shape", InteractorShape.Box)]
    [Tooltip("Animate the box size over time")]
    public bool animateBoxSize;
    [ShowIf("shape", InteractorShape.Box)]
    [Tooltip("Minimum box size")]
    public Vector3 minBoxSize = new Vector3(0.5f, 0.5f, 0.5f);
    [ShowIf("shape", InteractorShape.Box)]
    [Tooltip("Maximum box size")]
    public Vector3 maxBoxSize = new Vector3(5.0f, 5.0f, 5.0f);
    [ShowIf("shape", InteractorShape.Box)]
    [Tooltip("Animate the box rotation over time")]
    public bool animateBoxRotation;
    [ShowIf("shape", InteractorShape.Box)]
    [Tooltip("Minimum box rotation in degrees")]
    public Vector3 minBoxRotation = Vector3.zero;
    [ShowIf("shape", InteractorShape.Box)]
    [Tooltip("Maximum box rotation in degrees")]
    public Vector3 maxBoxRotation = new Vector3(0, 360, 0);
    


    
    
    [Header("Target Options")]
    [Tooltip("Follow another transform")]
    public bool followTarget;

    [Tooltip("Target transform to follow")]
    public Transform targetTransform;

    [Tooltip("Offset from the target position")]
    public Vector3 followOffset = Vector3.zero;

    [Header("Affected Renderers")]
    [Tooltip("List of renderers to affect. Only these renderers will be affected.")]
    public List<Renderer> targetRenderers = new List<Renderer>();
    
    
    


    
    
    public enum InteractorShape { Sphere, Box }
    private const int MaxInteractors = 20;
    private static readonly List<DissolveController> ActiveInteractors = new List<DissolveController>();
    private readonly List<Renderer> _affectedRenderers = new List<Renderer>();
    private bool _needsRefresh = true;
    private static readonly int PositionID = Shader.PropertyToID("_Position");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int ShapeTypeID = Shader.PropertyToID("_ShapeType");
    private static readonly int BoxSizeID = Shader.PropertyToID("_BoxSize");
    private static readonly int BoxRotationID = Shader.PropertyToID("_BoxRotation");
    private static readonly int InteractorCountID = Shader.PropertyToID("_InteractorCount");
    private static readonly int InteractorPositionsID = Shader.PropertyToID("_ShaderInteractorsPositions");
    private static readonly int InteractorRadiusesID = Shader.PropertyToID("_ShaderInteractorsRadiuses");
    private static readonly int InteractorBoxBoundsID = Shader.PropertyToID("_ShaderInteractorsBoxBounds");
    private static readonly int InteractorRotationID = Shader.PropertyToID("_ShaderInteractorRotation");


    private void OnEnable()
    {
        // Register this interactor
        if (!ActiveInteractors.Contains(this))
        {
            ActiveInteractors.Add(this);
        }
        
        // Update shader with new interactor
        UpdateShaders();
        _needsRefresh = true;
    }

    private void OnDisable()
    {
        // Unregister this interactor
        if (ActiveInteractors.Contains(this))
        {
            ActiveInteractors.Remove(this);
        }
        
        // Update shader without this interactor
        UpdateShaders();
    }

    private void Update()
    {
        // Follow target if enabled
        if (followTarget && targetTransform)
        {
            transform.position = targetTransform.position + followOffset;
        }

        // Animate radius if enabled
        if (animateRadius && shape == InteractorShape.Sphere)
        {
            float t = (Mathf.Sin(Time.time * animationSpeed) + 1.0f) * 0.5f;
            radius = Mathf.Lerp(minRadius, maxRadius, t);
        }
        
        // Animate box size if enabled
        if (animateBoxSize && shape == InteractorShape.Box)
        {
            float t = (Mathf.Sin(Time.time * animationSpeed) + 1.0f) * 0.5f;
            boxSize.x = Mathf.Lerp(minBoxSize.x, maxBoxSize.x, t);
            boxSize.y = Mathf.Lerp(minBoxSize.y, maxBoxSize.y, t);
            boxSize.z = Mathf.Lerp(minBoxSize.z, maxBoxSize.z, t);
        }
        
        // Animate box rotation if enabled
        if (animateBoxRotation && shape == InteractorShape.Box)
        {
            float t = (Mathf.Sin(Time.time * animationSpeed) + 1.0f) * 0.5f;
            boxRotation.x = Mathf.Lerp(minBoxRotation.x, maxBoxRotation.x, t);
            boxRotation.y = Mathf.Lerp(minBoxRotation.y, maxBoxRotation.y, t);
            boxRotation.z = Mathf.Lerp(minBoxRotation.z, maxBoxRotation.z, t);
        }

        // Update single interactor mode for specified renderers
        if (targetRenderers.Count > 0)
        {
            foreach (Renderer rend in targetRenderers)
            {
                if (rend)
                {
                    foreach (Material mat in rend.sharedMaterials)
                    {
                        UpdateSingleInteractorMaterial(mat);
                    }
                }
            }
        }

        // Update all shaders for multi-interactor mode
        UpdateShaders();
        
        // Refresh affected renderers list occasionally
        if (_needsRefresh)
        {
            RefreshAffectedRenderers();
            _needsRefresh = false;
        }
    }

    // Update a single material with this interaction's properties
    private void UpdateSingleInteractorMaterial(Material material)
    {
        if (material.shader.name.Contains("Custom/UnifiedDissolve"))
        {
            material.SetVector(PositionID, transform.position);
            
            // Set shape type (0 for sphere, 1 for box)
            material.SetFloat(ShapeTypeID, (float)(shape == InteractorShape.Sphere ? 0 : 1));
            
            if (shape == InteractorShape.Sphere)
            {
                material.SetFloat(RadiusID, radius);
            }
            else // Box shape
            {
                // Set box size and rotation
                material.SetVector(BoxSizeID, boxSize);
                material.SetVector(BoxRotationID, boxRotation);
            }
        }
    }

    // Update all affected materials with all active interactors
    private static void UpdateShaders()
    {
        if (ActiveInteractors.Count == 0)
            return;

        // Create arrays to hold all interactor data
        Vector4[] positions = new Vector4[MaxInteractors];
        float[] radiuses = new float[MaxInteractors];
        Vector4[] boxBounds = new Vector4[MaxInteractors];
        Vector4[] rotations = new Vector4[MaxInteractors];

        // Get data from all active interactors
        int count = Mathf.Min(ActiveInteractors.Count, MaxInteractors);
        
        for (int i = 0; i < count; i++)
        {
            DissolveController interactor = ActiveInteractors[i];
            
            positions[i] = interactor.transform.position;
            radiuses[i] = interactor.radius;
            boxBounds[i] = new Vector4(
                interactor.boxSize.x,
                interactor.boxSize.y,
                interactor.boxSize.z,
                interactor.shape == InteractorShape.Box ? 1.0f : 0.0f // Use w component to store shape type
            );
            rotations[i] = new Vector4(
                interactor.boxRotation.x,
                interactor.boxRotation.y,
                interactor.boxRotation.z,
                0.0f
            );
        }

        // Only update materials from targetRenderers list
        Material[] materials = GetAllDissolveShaderMaterials();
        
        foreach (Material mat in materials)
        {
            // Set arrays with all interactors data
            mat.SetVectorArray(InteractorPositionsID, positions);
            mat.SetFloatArray(InteractorRadiusesID, radiuses);
            mat.SetVectorArray(InteractorBoxBoundsID, boxBounds);
            mat.SetVectorArray(InteractorRotationID, rotations);
            mat.SetInt(InteractorCountID, count);
        }
    }

    // Find all materials using our dissolve shader, only from targetRenderers
    private static Material[] GetAllDissolveShaderMaterials()
    {
        List<Material> materials = new List<Material>();
        
        // Collect materials only from targetRenderers of all active interactors
        foreach (DissolveController controller in ActiveInteractors)
        {
            if (controller.targetRenderers.Count > 0)
            {
                foreach (Renderer renderer in controller.targetRenderers)
                {
                    if (renderer != null)
                    {
                        foreach (Material mat in renderer.sharedMaterials)
                        {
                            if (mat.shader.name.Contains("Custom/UnifiedDissolve") && !materials.Contains(mat))
                            {
                                materials.Add(mat);
                            }
                        }
                    }
                }
            }
        }
        
        return materials.ToArray();
    }

    // Reset all effects
    public static void ClearAllInteractors()
    {
        ActiveInteractors.Clear();
        UpdateShaders();
    }
    
    // Find all renderers affected by this controller (only from targetRenderers)
    private void RefreshAffectedRenderers()
    {
        _affectedRenderers.Clear();
        
        // Only use renderers from the targetRenderers list
        foreach (Renderer rend in targetRenderers)
        {
            if (rend)
            {
                bool hasDissolveShader = false;
                
                foreach (Material mat in rend.sharedMaterials)
                {
                    if (mat.shader.name.Contains("Custom/UnifiedDissolve"))
                    {
                        hasDissolveShader = true;
                        break;
                    }
                }
                
                if (hasDissolveShader)
                {
                    _affectedRenderers.Add(rend);
                }
            }
        }
    }
    
    
#if UNITY_EDITOR
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        DrawShapeGizmo();
        DrawConnectionGizmos();
    }
    
    private void DrawShapeGizmo()
    {
        // Draw the shape based on the current shape type
        if (shape == InteractorShape.Sphere)
        {
            DrawSphereGizmo();
        }
        else // Box shape
        {
            DrawBoxGizmo();
        }
        
        // If animating sphere, also show the max range
        if (animateRadius && shape == InteractorShape.Sphere)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
        
        // If animating box, also show the max range
        if ((animateBoxSize || animateBoxRotation) && shape == InteractorShape.Box)
        {
            Gizmos.color = Color.yellow;
            
            Matrix4x4 oldMatrix = Gizmos.matrix;
            
            // Draw max size box with max rotation
            if (animateBoxSize && animateBoxRotation)
            {
                // Create rotation matrix with max rotation
                Quaternion maxRotation = Quaternion.Euler(maxBoxRotation);
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, maxRotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(Vector3.zero, maxBoxSize * 2);
            }
            // Draw max size box with current rotation
            else if (animateBoxSize)
            {
                Quaternion rotation = Quaternion.Euler(boxRotation);
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(Vector3.zero, maxBoxSize * 2);
            }
            // Draw current size box with max rotation
            else if (animateBoxRotation)
            {
                Quaternion maxRotation = Quaternion.Euler(maxBoxRotation);
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, maxRotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(Vector3.zero, boxSize * 2);
            }
            
            // Restore original matrix
            Gizmos.matrix = oldMatrix;
        }
    }
    
    private void DrawSphereGizmo()
    {
        
        // Draw wireframe for better visibility
        Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    
    private void DrawBoxGizmo()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        
        // Create rotation matrix
        Quaternion rotation = Quaternion.Euler(boxRotation);
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        
        
        // Draw wireframe for better visibility
        Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero, boxSize * 2);
        
        // Restore original matrix
        Gizmos.matrix = oldMatrix;
    }
    
    private void DrawConnectionGizmos()
    {
        // Refresh renderers list if empty
        if (_affectedRenderers.Count == 0)
        {
            RefreshAffectedRenderers();
        }
        
        // Draw connections to affected renderers
        Gizmos.color = Color.green;
        
        foreach (Renderer rend in _affectedRenderers)
        {
            if (rend)
            {
                // Get renderer center position
                Vector3 rendererCenter = rend.bounds.center;
                
                // Draw connection line
                Gizmos.DrawLine(transform.position, rendererCenter);
                
                // Draw small sphere at renderer position
                Gizmos.DrawSphere(rendererCenter, 0.1f);
                
                // Draw distance label
                float distance = Vector3.Distance(transform.position, rendererCenter);
                    
                // Only draw labels in scene view, not in game view

                UnityEditor.Handles.Label(
                    Vector3.Lerp(transform.position, rendererCenter, 0.5f), 
                    distance.ToString("F1") + "m");

            }
        }
    }
    
    #endregion
#endif
}