using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Dispatches Unity Animation Events to UnityEvents
    /// </summary>
    [AddComponentMenu("DNExtensions/Animation Event Receiver")]
    public class AnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private List<AnimationEvent> animationEvents = new();
        
        public void OnAnimationEventTriggered(string eventName)
        {
            var eventToTrigger = animationEvents.Find(se => se.eventName == eventName);
            eventToTrigger?.onAnimationEvent?.Invoke();
        }
    }
}

