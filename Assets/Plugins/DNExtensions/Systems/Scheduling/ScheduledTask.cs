using System;

namespace DNExtensions.Systems.Scheduling
{
    internal enum ScheduledTaskKind
    {
        Delay,
        Repeat,
        WaitUntil,
        WaitWhile,
        WaitFrames
    }

    /// <summary>
    /// Value-type entry for a timer/condition task. Stored contiguously in the runner's list and mutated in place,
    /// so scheduling a timer allocates nothing on the heap. The coroutine escape hatch uses <see cref="CoroutineEntry"/> instead.
    /// </summary>
    internal struct ScheduledTask
    {
        public int id;
        public ScheduledTaskKind kind;
        public bool useRealtime;
        public float duration;
        public float elapsed;
        public int frameTarget;
        public int frameElapsed;
        public int repeatsRemaining;
        public Action action;
        public Func<bool> condition;
        public Action onComplete;
        public UnityEngine.Object owner;
        public bool hasOwner;
        public bool paused;
        public bool completed;
    }

    /// <summary>
    /// Reference-type state for a raw coroutine started through the runner. A class (not a struct) because the driving
    /// coroutine holds it and must observe external mutations to <see cref="paused"/>, <see cref="owner"/>, and <see cref="completed"/>.
    /// </summary>
    internal sealed class CoroutineEntry
    {
        public int id;
        public UnityEngine.Object owner;
        public bool hasOwner;
        public bool paused;
        public bool completed;
        public UnityEngine.Coroutine coroutine;
    }
}
