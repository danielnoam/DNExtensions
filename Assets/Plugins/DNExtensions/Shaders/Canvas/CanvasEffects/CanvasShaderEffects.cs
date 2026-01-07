using UnityEngine;
using UnityEngine.UI;

public class CanvasShaderEffects : MonoBehaviour
{
    [Header("Rainbow Settings")]
    [SerializeField] private bool enableRainbow = true;
    [SerializeField] private float rainbowSpeed = 30f;
    
    [Header("Dissolve Settings")]
    [SerializeField] private bool enableDissolve = true;
    [SerializeField] private float dissolveSpeed = 0.5f;
    [SerializeField] private bool pingPongDissolve = true;
    
    [Header("References")]
    [SerializeField] private Graphic[] graphics;
    
    private static readonly int RainbowRotation = Shader.PropertyToID("_Rainbow_Rotation");
    private static readonly int DissolveAmount = Shader.PropertyToID("_Dissolve_Amount");
    
    private Material[] materials;
    private float currentRotation;
    private float currentDissolve;

    private void Awake()
    {
        if (graphics == null || graphics.Length == 0)
        {
            return;
        }
        
        materials = new Material[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
        {
            materials[i] = graphics[i].material;
        }
    }

    private void Update()
    {
        if (enableRainbow)
        {
            currentRotation += rainbowSpeed * Time.deltaTime;
            currentRotation %= 360f;
            SetRainbowRotation(currentRotation);
        }

        if (enableDissolve)
        {
            if (pingPongDissolve)
            {
                currentDissolve = Mathf.PingPong(Time.time * dissolveSpeed, 1f);
            }
            else
            {
                currentDissolve += dissolveSpeed * Time.deltaTime;
                currentDissolve %= 1f;
            }
            SetDissolveAmount(currentDissolve);
        }
    }

    private void SetRainbowRotation(float rotation)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat(RainbowRotation, rotation);
        }
    }

    private void SetDissolveAmount(float amount)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetFloat(DissolveAmount, amount);
        }
    }
}