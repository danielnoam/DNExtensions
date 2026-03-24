using System;
using UnityEngine.Events;

namespace DNExtensions.Utilities
{
    [Serializable]
    public class AnimationEvent
    {
        public string eventName;
        public UnityEvent onAnimationEvent;
    }
}

