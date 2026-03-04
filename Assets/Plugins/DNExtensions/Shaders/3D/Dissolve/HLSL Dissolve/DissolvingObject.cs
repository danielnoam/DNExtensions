
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages dissolve effect setup for renderers, automatically applying dissolve materials
/// and supporting child renderer inclusion.
/// </summary>
public class DissolvingObject : MonoBehaviour
{
    [FormerlySerializedAs("autoSetupOnStart")]
    [Header("Settings")]
    [Tooltip("Whether to automatically set up the renderer with dissolve material on Awake")]
    public bool autoSetupOnAwake = true;

    [Tooltip("Whether to include all child renderers in the dissolve effect")]
    public bool includeChildRenderers = false;

    [Tooltip("Main renderer to be dissolved. If not set, will use the renderer on this GameObject.")]
    public Renderer rendererToDissolve;

    [Tooltip("Material with dissolve shader to apply. If null, will use the existing material.")]
    public Material dissolveMaterial;



    private void Awake()
    {
        if (autoSetupOnAwake)
        {
            SetupRenderer();
        }
    }


    private void SetupRenderer()
    {
        if (rendererToDissolve == null)
        {
            rendererToDissolve = GetComponent<Renderer>();

            if (rendererToDissolve == null)
            {
                Debug.LogError("DissolvingObject: No renderer found on " + gameObject.name);
                return;
            }
        }

        if (dissolveMaterial != null)
        {
            if (!dissolveMaterial.shader.name.Contains("Custom/UnifiedDissolve"))
            {
                Debug.LogWarning("DissolvingObject: Material does not use the UnifiedDissolve shader: " + dissolveMaterial.name);
            }

            // Create a unique instance of the material for this renderer
            rendererToDissolve.material = dissolveMaterial;
        }
        else
        {
            bool hasDissolveShader = false;
            foreach (Material mat in rendererToDissolve.sharedMaterials)
            {
                if (mat.shader.name.Contains("Custom/UnifiedDissolve"))
                {
                    hasDissolveShader = true;
                    break;
                }
            }

            if (!hasDissolveShader)
            {
                Debug.LogWarning("DissolvingObject: No dissolve material assigned and existing materials don't use the UnifiedDissolve shader");
            }
        }

        if (includeChildRenderers && dissolveMaterial != null)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer childRend in childRenderers)
            {
                // Skip the main renderer (already handled)
                if (childRend == rendererToDissolve) continue;

                childRend.material = dissolveMaterial;
            }
        }
    }

    /// <summary>
    /// Gets all child renderers including the renderer on this GameObject.
    /// </summary>
    /// <returns>Array of all child renderers.</returns>
    public Renderer[] GetAllChildRenderers()
    {
        List<Renderer> renderers = new List<Renderer>();

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer childRend in childRenderers)
        {
            if (!renderers.Contains(childRend))
            {
                renderers.Add(childRend);
            }
        }

        return renderers.ToArray();
    }
}
