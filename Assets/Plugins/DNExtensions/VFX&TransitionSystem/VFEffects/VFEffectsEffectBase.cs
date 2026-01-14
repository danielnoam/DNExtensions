

using System;

namespace DNExtensions.VFXManager
{
    [Serializable]
    public abstract class VFEffectsEffectBase
    {
        public abstract void OnPlayEffect(float sequenceDuration);
        
        public abstract void OnResetEffect();
    }
}