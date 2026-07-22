using System;
using System.Collections;

namespace DNExtensions.Systems.Scheduling
{
    /// <summary>
    /// Static entry point for delayed and repeating actions. All calls run on a single persistent update loop,
    /// return a <see cref="ScheduledHandle"/> for cancelling/pausing, and require no scene setup.
    /// </summary>
    public static class Scheduler
    {
        /// <summary>Invokes <paramref name="action"/> after <paramref name="seconds"/> of scaled time.</summary>
        public static ScheduledHandle Delay(float seconds, Action action)
        {
            return SchedulerRunner.AddTimed(seconds, false, ScheduledTaskKind.Delay, 0, action, null);
        }

        /// <summary>Invokes <paramref name="action"/> after <paramref name="seconds"/> of unscaled (real) time, ignoring Time.timeScale.</summary>
        public static ScheduledHandle DelayRealtime(float seconds, Action action)
        {
            return SchedulerRunner.AddTimed(seconds, true, ScheduledTaskKind.Delay, 0, action, null);
        }

        public static ScheduledHandle NextFrame(Action action)
        {
            return SchedulerRunner.AddFrames(1, action);
        }

        public static ScheduledHandle WaitFrames(int frames, Action action)
        {
            return SchedulerRunner.AddFrames(frames, action);
        }

        /// <summary>Invokes <paramref name="action"/> on the first frame <paramref name="condition"/> returns true.</summary>
        public static ScheduledHandle WaitUntil(Func<bool> condition, Action action)
        {
            return SchedulerRunner.AddCondition(condition, false, action);
        }

        /// <summary>Invokes <paramref name="action"/> on the first frame <paramref name="condition"/> returns false.</summary>
        public static ScheduledHandle WaitWhile(Func<bool> condition, Action action)
        {
            return SchedulerRunner.AddCondition(condition, true, action);
        }

        /// <summary>Invokes <paramref name="action"/> every <paramref name="interval"/> seconds forever, until cancelled.</summary>
        public static ScheduledHandle Repeat(float interval, Action action)
        {
            return SchedulerRunner.AddTimed(interval, false, ScheduledTaskKind.Repeat, -1, action, null);
        }

        /// <summary>Invokes <paramref name="action"/> every <paramref name="interval"/> seconds for <paramref name="count"/> times, then <paramref name="onComplete"/>.</summary>
        public static ScheduledHandle Repeat(float interval, int count, Action action, Action onComplete = null)
        {
            return SchedulerRunner.AddTimed(interval, false, ScheduledTaskKind.Repeat, count, action, onComplete);
        }

        /// <summary>Runs an arbitrary coroutine on the shared runner, with cancel/pause/owner-binding support via the returned handle.</summary>
        public static ScheduledHandle Run(IEnumerator routine)
        {
            return SchedulerRunner.AddCoroutine(routine);
        }
    }
}
