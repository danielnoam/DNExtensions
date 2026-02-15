using System;
using DNExtensions.Utilities.Button;
using DNExtensions.Utilities.SerializableSelector;
using PrimeTween;
using UnityEngine;




namespace DNExtensions.Systems.VFXManager
{
    
    [CreateAssetMenu(fileName = "Sequence", menuName = "Scriptable Objects/Effect Sequence")]
    public class EffectSequence : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField, Min(0f)] private float sequenceDuration = 1f;
        [SerializeField, Tooltip("Playing this sequence will not reset the effects of the previous sequence")] 
        private bool sequenceIsAdditive;
        [SerializeField, Tooltip("After the sequences completes, reset all the effect to default ")] 
        private bool resetEffectsOnComplete = true;
        [SerializeReference, SerializableSelector] private EffectBase[] effects;


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
                effect?.PlayEffect(sequenceDuration);
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
                effect?.ResetEffect();
            }
        }
    }
    
    
    
    
}