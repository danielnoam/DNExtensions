using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PingPongDissolve : MonoBehaviour
{

    
    [Header("Settings")]
    [SerializeField] private float speed = 0.25f;
    [SerializeField] private float objectHeight = 1.0f;
    
    [Header("References")]
    [SerializeField] private Material material;
    
    
    private static readonly int CutoffHeight = Shader.PropertyToID("_CutoffHeight");



    private void Awake()
    {
        if (!material) material = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        var time = Time.time * Mathf.PI * speed;

        float height = 0;
        height += Mathf.Sin(time) * (objectHeight / 2.0f);
        SetHeight(height);
    }

    private void SetHeight(float height)
    {
        material.SetFloat(CutoffHeight, height);
    }
}
