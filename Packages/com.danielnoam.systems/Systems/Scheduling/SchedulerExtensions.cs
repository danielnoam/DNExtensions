using System;
using System.Collections;
using UnityEngine;

namespace DNExtensions.Systems.Scheduling
{
    /// <summary>
    /// MonoBehaviour shortcuts that schedule an action already bound to the calling component, so the task
    /// is cancelled automatically if the component is destroyed.
    /// </summary>
    public static class SchedulerExtensions
    {
        public static ScheduledHandle Delay(this MonoBehaviour owner, float seconds, Action action)
        {
            return Scheduler.Delay(seconds, action).BindTo(owner);
        }

        public static ScheduledHandle DelayRealtime(this MonoBehaviour owner, float seconds, Action action)
        {
            return Scheduler.DelayRealtime(seconds, action).BindTo(owner);
        }

        public static ScheduledHandle NextFrame(this MonoBehaviour owner, Action action)
        {
            return Scheduler.NextFrame(action).BindTo(owner);
        }

        public static ScheduledHandle WaitFrames(this MonoBehaviour owner, int frames, Action action)
        {
            return Scheduler.WaitFrames(frames, action).BindTo(owner);
        }

        public static ScheduledHandle WaitUntil(this MonoBehaviour owner, Func<bool> condition, Action action)
        {
            return Scheduler.WaitUntil(condition, action).BindTo(owner);
        }

        public static ScheduledHandle WaitWhile(this MonoBehaviour owner, Func<bool> condition, Action action)
        {
            return Scheduler.WaitWhile(condition, action).BindTo(owner);
        }

        public static ScheduledHandle Repeat(this MonoBehaviour owner, float interval, Action action)
        {
            return Scheduler.Repeat(interval, action).BindTo(owner);
        }

        public static ScheduledHandle Repeat(this MonoBehaviour owner, float interval, int count, Action action, Action onComplete = null)
        {
            return Scheduler.Repeat(interval, count, action, onComplete).BindTo(owner);
        }

        public static ScheduledHandle Run(this MonoBehaviour owner, IEnumerator routine)
        {
            return Scheduler.Run(routine).BindTo(owner);
        }
    }
}
