using System;
using DNExtensions.Utilities;
using UnityEngine;

namespace DNExtensions.Systems.VFXManager
{
    [Serializable]
    public abstract class EffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)] protected RangedFloat duration = new RangedFloat(0f, 1f);
        
        protected float GetStartDelay(float sequenceDuration) => sequenceDuration * duration.minValue;
        protected float GetEffectDuration(float sequenceDuration) => sequenceDuration * duration.maxValue - sequenceDuration * duration.minValue;

        protected virtual bool Initialize() => true;
        protected abstract void OnPlayEffect(float sequenceDuration);
        protected abstract void OnResetEffect();
        
        public void PlayEffect(float sequenceDuration)
        {
            if (!Initialize()) return;
            OnPlayEffect(sequenceDuration);
        }

        public void ResetEffect()
        {
            OnResetEffect();
        }
    }
}