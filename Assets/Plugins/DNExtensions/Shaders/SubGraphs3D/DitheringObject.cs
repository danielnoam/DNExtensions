using System;
using DNExtensions.Utilities;
using UnityEngine;

public class DitheringObject : MonoBehaviour
{

    [InfoBox("Set a target to update object position in shader")]
    [Header("Settings")]
    [SerializeField] private Transform target;

    
    
    private static readonly int PositionID = Shader.PropertyToID("_Dither_Object_Position");
    private Material _material;

    private void Awake()
    {
        Renderer rand = GetComponent<Renderer>();
        rand.material = new Material(rand.material);
        _material = rand.material;
    }
    


    private void Update()
    {
        UpdateTargetPosition();
    }
    
    
    private void UpdateTargetPosition()
    {
        if (!target || !_material) return;
        
        _material.SetVector(PositionID, target.position);
    }
}
