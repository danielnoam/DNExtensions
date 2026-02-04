using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using DNExtensions.ControllerRumbleSystem;
using PrimeTween;
using UnityEngine;

public class Test : MonoBehaviour
{
    
    [SerializeField] private ControllerRumbleSource rumbleSource;
    [SerializeField] private ControllerRumbleEffectSettings rumbleEffect;

    [SerializeField] private bool playContinuousOnStart;

    private Sequence _shakeSequence;
    
    private void Awake()
    {
        rumbleSource = gameObject.GetOrAddComponent<ControllerRumbleSource>();

    }

    private void Start()
    {
        if (playContinuousOnStart)
        {
            rumbleSource.StartContinuousRumble(rumbleEffect.lowFrequency, rumbleEffect.highFrequency);
            StartContinuousShake();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !_shakeSequence.isAlive)
        {
            PlayRumbleEffect();
        }
    }

    [Button]
    private void PlayRumbleEffect()
    {
        rumbleSource?.Rumble(rumbleEffect);
        
        if (_shakeSequence.isAlive) _shakeSequence.Stop();
        _shakeSequence = Sequence.Create()
            .Group(Tween.PunchLocalPosition(transform, Vector3.one * 0.2f, rumbleEffect.duration));

    }
    
    
    private void StartContinuousShake()
    {
        if (_shakeSequence.isAlive) _shakeSequence.Stop();

        _shakeSequence = Sequence.Create(cycles: -1)
            .Chain(Tween.ShakeLocalPosition(transform, Vector3.one * 0.4f, duration: 0.1f, frequency: 3));
    }
    
    private void StopContinuousShake()
    {
        if (_shakeSequence.isAlive) _shakeSequence.Stop();
        transform.localPosition = Vector3.zero;
    }

}
