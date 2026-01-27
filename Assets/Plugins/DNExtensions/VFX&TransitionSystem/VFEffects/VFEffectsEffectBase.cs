

using System;

namespace DNExtensions.Utilities.VFXManager
{
    [Serializable]
    public abstract class VFEffectsEffectBase
    {
        public abstract void OnPlayEffect(float sequenceDuration);
        
        public abstract void OnResetEffect();
    }
}