using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// A ScriptableObject that represents an event. It can be invoked to notify all subscribers.
    /// </summary>
    [CreateAssetMenu(fileName = "New Event", menuName = "Scriptables/Event")]
    public class SOEvent : ScriptableObject
    {
        private Action _onEvent;

#if UNITY_EDITOR
        [SerializeField, ReadOnly] private int subscriberCount;
#endif

        /// <summary>
        /// Invokes the event.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void Invoke() => _onEvent?.Invoke();

        /// <summary>
        /// Subscribes an action to the event.
        /// </summary>
        private void Subscribe(Action action)
        {
            _onEvent += action;
            
#if UNITY_EDITOR
            subscriberCount = _onEvent != null ? _onEvent.GetInvocationList().Length : 0;
#endif
        }

        /// <summary>
        /// Unsubscribes an action from the event.
        /// </summary>
        private void Unsubscribe(Action action)
        {
            _onEvent -= action;
            
#if UNITY_EDITOR
            subscriberCount = _onEvent != null ? _onEvent.GetInvocationList().Length : 0;
#endif
        }

        /// <summary>
        /// Operator overload for subscribing actions to the event.
        /// </summary>
        public static SOEvent operator +(SOEvent e, Action action)
        {
            e.Subscribe(action);
            return e;
        }

        /// <summary>
        /// Operator overload for unsubscribing actions from the event.
        /// </summary>
        public static SOEvent operator -(SOEvent e, Action action)
        {
            e.Unsubscribe(action);
            return e;
        }
    }
}