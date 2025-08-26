using UnityEngine;

public class PulseEffect : MonoBehaviour
{

    [Header("Pulse Settings")]
    [SerializeField] private bool enablePulse = false;
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private float pulseCooldown = 1f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool pulseX = true;
    [SerializeField] private bool pulseY = true;
    [SerializeField] private bool pulseZ = true;
    private Vector3 initialScale;
    private float pulseTimer;



    private void Start()
    {

        initialScale = transform.localScale;
        pulseTimer = 0f;
    }

    private void FixedUpdate()
    {

        
        Vector3 pulseScale = CalculatePulseScale();
        transform.localScale = Vector3.Scale(initialScale, pulseScale);
    }




    private Vector3 CalculatePulseScale()
    {
        if (!enablePulse)
            return Vector3.one;

        pulseTimer += Time.fixedDeltaTime;
        
        if (pulseTimer >= pulseCooldown)
        {
            pulseTimer = 0f;
        }

        float progress = pulseTimer / pulseCooldown;
        float pulseValue = pulseCurve.Evaluate(progress) * pulseAmount;

        return new Vector3(
            pulseX ? 1f + pulseValue : 1f,
            pulseY ? 1f + pulseValue : 1f,
            pulseZ ? 1f + pulseValue : 1f
        );
    }
}