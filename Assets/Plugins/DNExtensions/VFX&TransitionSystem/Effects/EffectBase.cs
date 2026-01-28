using System;
using DNExtensions.Utilities.RangedValues;
using UnityEngine;

namespace DNExtensions.Utilities.VFXManager
{

    [Serializable]
    public abstract class EffectBase
    {
        [Tooltip("The duration of the effect, percentage from the sequence duration.")]
        [SerializeField, MinMaxRange(0f, 1f)]
        protected RangedFloat duration = new RangedFloat(0f, 1f);

        private bool _initialized;
        private bool _failed;

        public void PlayEffect(float sequenceDuration)
        {
            if (_failed) return;

            if (!_initialized)
            {
                if (!Initialize())
                {
                    _failed = true;
                    return;
                }

                _initialized = true;
            }

            OnPlayEffect(sequenceDuration);
        }

        public void ResetEffect()
        {
            if (_failed) return;
            OnResetEffect();
        }


        protected float GetStartDelay(float sequenceDuration) => sequenceDuration * duration.minValue;

        protected float GetEffectDuration(float sequenceDuration) =>
            sequenceDuration * duration.maxValue - (sequenceDuration * duration.minValue);

        protected virtual bool Initialize() => true;
        protected abstract void OnPlayEffect(float sequenceDuration);
        protected abstract void OnResetEffect();
    }
}