using UnityEngine;

public class BreathEffect : MonoBehaviour
{
    

    [Header("Breathing Settings")]
    [SerializeField] private bool enableBreathing = false;
    [SerializeField] private float breatheAmount = 0.2f;
    [SerializeField] private float breatheSpeed = 1f;
    [SerializeField] private AnimationCurve breatheCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool breatheX = true;
    [SerializeField] private bool breatheY = true;
    [SerializeField] private bool breatheZ = true;
    private Vector3 initialScale;
    private float breatheTime;


    private void Start()
    {

        initialScale = transform.localScale;
        breatheTime = Random.value * Mathf.PI * 2;

    }

    private void FixedUpdate()
    {

        Vector3 breatheScale = CalculateBreatheScale();
        transform.localScale = Vector3.Scale(initialScale, breatheScale);
    }
    

    private Vector3 CalculateBreatheScale()
    {
        if (!enableBreathing)
            return Vector3.one;

        breatheTime += breatheSpeed * Time.fixedDeltaTime;
        
        float normalizedBreatheCycle = (Mathf.Sin(breatheTime) + 1f) * 0.5f;
        float breatheValue = breatheCurve.Evaluate(normalizedBreatheCycle) * breatheAmount;

        return new Vector3(
            breatheX ? 1f + breatheValue : 1f,
            breatheY ? 1f + breatheValue : 1f,
            breatheZ ? 1f + breatheValue : 1f
        );
    }
}