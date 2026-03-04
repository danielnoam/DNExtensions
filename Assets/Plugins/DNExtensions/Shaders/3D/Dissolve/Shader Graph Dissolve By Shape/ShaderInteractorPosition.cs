using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sets global shader properties for a single dissolve interactor position and radius.
/// </summary>
[ExecuteInEditMode]
public class ShaderInteractorPosition : MonoBehaviour
{

    [Header("Marker Settings")]
    public float radius = 1f;

    private static readonly int Radius = Shader.PropertyToID("_Radius");
    private static readonly int Position = Shader.PropertyToID("_Position");

    private void Update()
    {
        // remove if you want multiple interactors, only for example of single interactor
        Shader.SetGlobalVector(Position, transform.position);
        Shader.SetGlobalFloat(Radius, radius);
    }

}