using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using DNExtensions.Utilities.Button;

public class UIbar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float trailDelay = 0.1f;
    [SerializeField] private float valueChangeDuration = 0.25f;
    [SerializeField] private float trailChangeDuration = 2f;
    [SerializeField] private float maxValue = 100;
    
    [Header("References")]
    [SerializeField] private Image bar;
    [SerializeField] private Image barTrail;
    
    
    private float _currentValue;
    private Sequence _sequence;

    private void Awake()
    {
        _currentValue = maxValue;
        if (bar) bar.fillAmount = 1;
        if (barTrail) barTrail.fillAmount = 1;
    }


    [Button]
    private void DrainBar()
    {
        if (_currentValue <= 0 || !bar || !barTrail) return;
        _sequence.Stop();
        
        _currentValue -= 10;
        float targetFillAmount = _currentValue / maxValue;

        _sequence = Sequence.Create(useUnscaledTime: true, sequenceEase: Ease.InOutSine)
            .Group(Tween.UIFillAmount(bar, targetFillAmount, duration: valueChangeDuration))
            .ChainDelay(trailDelay)
            .Chain(Tween.UIFillAmount(barTrail, targetFillAmount, duration: trailChangeDuration));
    }
    
    [Button]
    private void FillBar()
    {
        if (_currentValue >= maxValue || !bar || !barTrail) return;
        _sequence.Stop();
        
        _currentValue += 10;
        float targetFillAmount = _currentValue / maxValue;

        _sequence = Sequence.Create(useUnscaledTime: true, sequenceEase: Ease.InOutSine)
            .Group(Tween.UIFillAmount(barTrail, targetFillAmount, duration: valueChangeDuration))
            .ChainDelay(trailDelay)
            .Chain(Tween.UIFillAmount(bar, targetFillAmount, duration: trailChangeDuration));
    }
}
