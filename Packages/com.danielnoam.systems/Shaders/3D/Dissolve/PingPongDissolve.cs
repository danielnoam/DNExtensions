using System;
using System.Collections;
using System.Collections.Generic;
using DNExtensions.Utilities.Button;
using UnityEngine;

/// <summary>
/// Animates a dissolve effect by ping-ponging the cutoff height in a sine wave pattern.
/// </summary>
public class PingPongDissolve : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Speed of the dissolve animation")]
    [SerializeField] private float speed = 0.25f;
    [Tooltip("Total height range for the dissolve effect")]
    [SerializeField] private float objectHeight = 1.0f;

    [Header("References")]
    [Tooltip("Renderers to apply the dissolve effect to")]
    [SerializeField] private Renderer[] renderers;

    private static readonly int CutoffHeight = Shader.PropertyToID("_CutoffHeight");

    private Material[] materials;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        materials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
        }
    }

    private void Update()
    {
        var time = Time.time * Mathf.PI * speed;
        float height = Mathf.Sin(time) * (objectHeight / 2.0f);
        SetHeight(height);
    }

    private void SetHeight(float height)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat(CutoffHeight, height);
        }
    }

    [Button(ButtonPlayMode.Both)]
    private void GetAllRenderersInChildren()
    {
        renderers = Array.Empty<Renderer>();
        renderers = GetComponentsInChildren<Renderer>();
    }
}