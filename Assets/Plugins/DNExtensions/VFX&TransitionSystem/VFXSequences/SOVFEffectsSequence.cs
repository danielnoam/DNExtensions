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
        [SerializeField] private bool resetEffectsOnComplete = true;
        [SerializeReference] private VFEffectsEffectBase[] effects;


        private Sequence _sequence;


        [Button]
        public float PlayEffects()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            foreach (var effect in effects)
            {
                effect?.OnPlayEffect(sequenceDuration);
            }
            
            _sequence = Sequence.Create()
                .ChainDelay(sequenceDuration)
                .OnComplete(() => { if (resetEffectsOnComplete) ResetEffects(); });


            return sequenceDuration;
        }
        
        

        [Button]
        public void ResetEffects()
        {
            if (_sequence.isAlive) _sequence.Stop();
            
            foreach (var effect in effects)
            {
                effect?.OnResetEffect();
            }
        }
    }
    
    
    
    
}