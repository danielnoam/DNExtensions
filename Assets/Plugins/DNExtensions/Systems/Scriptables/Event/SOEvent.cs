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
        private Action onEvent;

#if UNITY_EDITOR
        [SerializeField, ReadOnly] private int subscriberCount;
#endif

        
        /// <summary>
        /// Invokes the event.
        /// </summary>
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void Invoke() => onEvent?.Invoke();
        
        /// <summary>
        /// Unsubscribes an action from the event. The action will no longer be invoked when the event is invoked.
        /// </summary>
        /// <param name="action"></param>
        private void Subscribe(Action action)
        {
            onEvent += action;
            
#if UNITY_EDITOR
            subscriberCount = onEvent != null ? onEvent.GetInvocationList().Length : 0;
#endif
        }


        /// <summary>
        /// Unsubscribes an action from the event. The action will no longer be invoked when the event is invoked.
        /// </summary>
        /// <param name="action"></param>
        private void Unsubscribe(Action action)
        {
            onEvent -= action;
            
#if UNITY_EDITOR
            subscriberCount = onEvent != null ? onEvent.GetInvocationList().Length : 0;
#endif
        }


        /// <summary>
        /// Operator overloads for subscribing.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static SOEvent operator +(SOEvent e, Action action)
        {
            e.Subscribe(action);
            return e;
        }

        /// <summary>
        /// Operator overloads for unsubscribing actions to the event.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static SOEvent operator -(SOEvent e, Action action)
        {
            e.Unsubscribe(action);
            return e;
        }
    }
}