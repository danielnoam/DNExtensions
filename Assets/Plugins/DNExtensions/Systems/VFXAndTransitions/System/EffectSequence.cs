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
        [SerializeField, Min(0f)] private float duration = 1f;
        [SerializeField, Tooltip("Playing this sequence will not reset the effects of the previous sequence")] private bool isAdditive;
        [SerializeField, Tooltip("After the sequences completes, reset all the effect to default ")] private bool resetEffectsOnComplete = true;
        [SerializeReference, SerializableSelector] private EffectBase[] effects;
        
        private Sequence _sequence;
        
        public bool IsAdditive => isAdditive;
        


        [Button]
        public float PlaySequence()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            foreach (var effect in effects)
            {
                effect?.PlayEffect(duration);
            }
            
            _sequence = Sequence.Create()
                .ChainDelay(duration)
                .OnComplete(() =>
                {
                    if (resetEffectsOnComplete) ResetSequenceEffects();
                });


            return duration;
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