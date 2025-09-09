using System;
using DNExtensions.Button;
using PrimeTween;
using UnityEngine;




namespace DNExtensions.VFXManager
{
    
    [CreateAssetMenu(fileName = "VFX Sequence", menuName = "Scriptable Objects/New VFX Sequence")]
    public class SOVFEffectsSequence : ScriptableObject
    {
        [Header("Sequence Settings")]
        [SerializeField, Min(0f)] private float sequenceDuration = 1f;
        [SerializeField, Tooltip("Playing this sequence will not reset the effects of the previous sequence")] private bool sequenceIsAdditive;
        [SerializeField, Tooltip("After the sequences completes, reset all the effect to default ")] private bool resetEffectsOnComplete = true;
        [SerializeReference] private VFEffectsEffectBase[] effects;


        private Sequence _sequence;
        
        public bool SequenceIsAdditive => sequenceIsAdditive;
        
        public event Action OnSequencePlay;
        public event Action OnSequenceComplete;


        [Button]
        public float PlaySequence()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            foreach (var effect in effects)
            {
                effect?.OnPlayEffect(sequenceDuration);
            }
            OnSequencePlay?.Invoke();
            
            _sequence = Sequence.Create()
                .ChainDelay(sequenceDuration)
                .OnComplete(() =>
                {
                    OnSequenceComplete?.Invoke();
                    if (resetEffectsOnComplete) ResetSequenceEffects();
                });


            return sequenceDuration;
        }
        
        

        [Button]
        public void ResetSequenceEffects()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            foreach (var effect in effects)
            {
                effect?.OnResetEffect();
            }
        }
    }
    
    
    
    
}