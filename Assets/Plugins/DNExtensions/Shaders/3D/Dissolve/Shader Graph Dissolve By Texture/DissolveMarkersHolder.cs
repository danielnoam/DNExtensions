using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DissolveMarkersHolder : MonoBehaviour
{


    [Header("General Dissolve Marker Settings")]
    [Range(0, 1)] public float shapeCutoff;
    [Range(0, 1)] public float shapeSmoothness = 0.1f;

    
    private DissolveMarkerPosition[] _interactors;
    private Vector4[] _positions;
    private float[] _radiuses;
    private Vector4[] _boxBounds;
    private Vector4[] _rotations;
    private float[] _textureIndex;
    private static readonly int ShaderInteractorsPositions = Shader.PropertyToID("_ShaderInteractorsPositions");
    private static readonly int ShaderInteractorsRadiuses = Shader.PropertyToID("_ShaderInteractorsRadiuses");
    private static readonly int ShaderInteractorsIndices = Shader.PropertyToID("_ShaderInteractorsIndices");
    private static readonly int ShaderInteractorRotation = Shader.PropertyToID("_ShaderInteractorRotation");
    private static readonly int ShaderInteractorsBoxBounds = Shader.PropertyToID("_ShaderInteractorsBoxBounds");
    private static readonly int ShapeCutoff = Shader.PropertyToID("_ShapeCutoff");
    private static readonly int ShapeSmoothness = Shader.PropertyToID("_ShapeSmoothness");
    
    private void Start()
    {
        FindInteractors();
    }
    private void OnEnable()
    {
        FindInteractors();
    }


    private void FindInteractors()
    {
        _interactors = FindObjectsByType<DissolveMarkerPosition>(FindObjectsSortMode.None);
        _positions = new Vector4[100];
        _radiuses = new float[100];
        _boxBounds = new Vector4[100];
        _rotations = new Vector4[100];
        _textureIndex = new float[100];
    }


    private void Update()
    {
        FindInteractors();
        for (int i = 0; i < _interactors.Length; i++)
        {
            _positions[i] = _interactors[i].transform.position;
            _radiuses[i] = _interactors[i].radius;
            _boxBounds[i] = _interactors[i].transform.localScale;
            _rotations[i] = _interactors[i].transform.eulerAngles;
            _textureIndex[i] = _interactors[i].textureIndex;
        }
        Shader.SetGlobalVectorArray(ShaderInteractorsPositions, _positions);
        Shader.SetGlobalFloatArray(ShaderInteractorsRadiuses, _radiuses);
        Shader.SetGlobalFloatArray(ShaderInteractorsIndices, _textureIndex);
        Shader.SetGlobalVectorArray(ShaderInteractorsBoxBounds, _boxBounds);
        Shader.SetGlobalVectorArray(ShaderInteractorRotation, _rotations);

        Shader.SetGlobalFloat(ShapeCutoff, shapeCutoff);
        Shader.SetGlobalFloat(ShapeSmoothness, shapeSmoothness);

    }
}