using System;
using DNExtensions;
using UnityEngine;

public class DitheringObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SubscribeTo subscribeTo = SubscribeTo.Player;
    [SerializeField,ReadOnly] private Transform target;
    private enum SubscribeTo { None, Player, Robot, RobotLight, }
    private bool SubscribeToNone => subscribeTo == SubscribeTo.None;
    private static readonly int PositionID = Shader.PropertyToID("_Dither_Object_Position");
    private Material _material;

    private void Awake()
    {
        Renderer rend = GetComponent<Renderer>();
        _material = rend.material;
        rend.material = new Material(_material);
        _material = rend.material;
    }

    private void Start()
    {
        if (SubscribeToNone) return;

        switch (subscribeTo)
        {
            case SubscribeTo.Robot:

                break;
            case SubscribeTo.RobotLight:

                break;
            case SubscribeTo.Player:

                break;
        }
    }
    
    private void OnEnable()
    {
        if (SubscribeToNone) return;
        
        switch (subscribeTo)
        {
            case SubscribeTo.Robot:

                break;
            case SubscribeTo.RobotLight:

                break;
            case SubscribeTo.Player:
                

                break;
        }
    }
    
    private void OnDisable()
    {
        target = null;
    }


    private void Update()
    {
        UpdateTargetPosition();
    }
    
    
    private void UpdateTargetPosition()
    {
        if (!target) return;
        _material.SetVector(PositionID, target.position);
    }
}
