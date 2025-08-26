using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RotateEffect : MonoBehaviour
{
    [System.Serializable]
    public class AxisRotationSettings
    {
        public bool continuousRotation = false;
        public bool reverseDirection = false;
        public float oscillationAmount = 0f;
        public float oscillationSpeed = 0f;
        public float rotationSpeed = 0f;
    }


    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private bool resetRotationOnDisable = false;
    [SerializeField] private AxisRotationSettings xRotation = new AxisRotationSettings();
    [SerializeField] private AxisRotationSettings yRotation = new AxisRotationSettings();
    [SerializeField] private AxisRotationSettings zRotation = new AxisRotationSettings();

    
    private Quaternion initialRotation;
    private Vector3 rotationTime;
    private float pulseTimer;
    private Vector3 continuousRotationAngles;

    private void Start()
    {
        initialRotation = transform.rotation;
        
        rotationTime = new Vector3(
            Random.value * Mathf.PI * 2,
            Random.value * Mathf.PI * 2,
            Random.value * Mathf.PI * 2
        );
        
        continuousRotationAngles = Vector3.zero;
    }

    private void FixedUpdate()
    {
        RotationEffect();
        
    }

    private void OnDisable()
    {
        if (resetRotationOnDisable) ResetRotation();
    }


    private void RotationEffect()
    {
        if (!enableRotation) return;

        Vector3 rotationChange = new Vector3(
            CalculateAxisRotation(ref rotationTime.x, xRotation),
            CalculateAxisRotation(ref rotationTime.y, yRotation),
            CalculateAxisRotation(ref rotationTime.z, zRotation)
        );

        if (xRotation.continuousRotation) continuousRotationAngles.x += rotationChange.x;
        if (yRotation.continuousRotation) continuousRotationAngles.y += rotationChange.y;
        if (zRotation.continuousRotation) continuousRotationAngles.z += rotationChange.z;

        Vector3 finalRotation = new Vector3(
            xRotation.continuousRotation ? continuousRotationAngles.x : rotationChange.x,
            yRotation.continuousRotation ? continuousRotationAngles.y : rotationChange.y,
            zRotation.continuousRotation ? continuousRotationAngles.z : rotationChange.z
        );

        transform.rotation = initialRotation * Quaternion.Euler(finalRotation);
    }

    private float CalculateAxisRotation(ref float rotationTime, AxisRotationSettings settings)
    {
        if (settings.continuousRotation)
        {
            float direction = settings.reverseDirection ? -1f : 1f;
            return settings.rotationSpeed * Time.fixedDeltaTime * direction;
        }
        else if (settings.oscillationAmount != 0)
        {
            rotationTime += settings.oscillationSpeed * Time.fixedDeltaTime;
            return Mathf.Sin(rotationTime) * settings.oscillationAmount;
        }
        return 0f;
    }

    private void ResetRotation()
    {
        transform.rotation = initialRotation;
        rotationTime = Vector3.zero;
        continuousRotationAngles = Vector3.zero;
        
    }

}