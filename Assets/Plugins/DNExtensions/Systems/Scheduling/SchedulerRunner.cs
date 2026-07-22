using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.Systems.Scheduling
{
    /// <summary>
    /// Persistent host that drives all scheduled tasks from a single update loop. Bootstraps itself on play;
    /// there is no need to place it in a scene. Access it through <see cref="Scheduler"/>, not directly.
    /// Timer/condition tasks live in a contiguous struct array mutated in place (zero steady-state allocation);
    /// raw coroutines started via <see cref="AddCoroutine"/> live in a separate dictionary.
    /// </summary>
    internal sealed class SchedulerRunner : MonoBehaviour
    {
        private static SchedulerRunner _instance;
        private static bool _quitting;

        private ScheduledTask[] _active = new ScheduledTask[64];
        private int _count;
        private readonly Dictionary<int, int> _indexById = new Dictionary<int, int>(64);
        private readonly List<ScheduledTask> _pending = new List<ScheduledTask>(16);
        private readonly Dictionary<int, CoroutineEntry> _coroutines = new Dictionary<int, CoroutineEntry>(16);
        private readonly List<int> _coroutineRemovals = new List<int>(16);
        private bool _ticking;
        private int _nextId = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _quitting = false;
            EnsureInstance();
        }

        private static SchedulerRunner EnsureInstance()
        {
            if (_instance || _quitting) return _instance;

            var host = new GameObject("SchedulerRunner")
            {
                hideFlags = HideFlags.DontSave
            };
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<SchedulerRunner>();
            return _instance;
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void EnsureCapacity(int min)
        {
            if (_active.Length >= min) return;
            int capacity = _active.Length * 2;
            if (capacity < min) capacity = min;
            Array.Resize(ref _active, capacity);
        }

        private ScheduledHandle Register(ScheduledTask task)
        {
            if (_ticking)
            {
                _pending.Add(task);
            }
            else
            {
                EnsureCapacity(_count + 1);
                _indexById[task.id] = _count;
                _active[_count] = task;
                _count++;
            }

            return new ScheduledHandle(task.id);
        }

        internal static ScheduledHandle AddTimed(float duration, bool realtime, ScheduledTaskKind kind, int repeats, Action action, Action onComplete)
        {
            var runner = EnsureInstance();
            var task = new ScheduledTask
            {
                id = runner._nextId++,
                kind = kind,
                duration = duration,
                useRealtime = realtime,
                repeatsRemaining = repeats,
                action = action,
                onComplete = onComplete
            };
            return runner.Register(task);
        }

        internal static ScheduledHandle AddFrames(int frames, Action action)
        {
            var runner = EnsureInstance();
            var task = new ScheduledTask
            {
                id = runner._nextId++,
                kind = ScheduledTaskKind.WaitFrames,
                frameTarget = Mathf.Max(1, frames),
                action = action
            };
            return runner.Register(task);
        }

        internal static ScheduledHandle AddCondition(Func<bool> condition, bool waitWhile, Action action)
        {
            var runner = EnsureInstance();
            var task = new ScheduledTask
            {
                id = runner._nextId++,
                kind = waitWhile ? ScheduledTaskKind.WaitWhile : ScheduledTaskKind.WaitUntil,
                condition = condition,
                action = action
            };
            return runner.Register(task);
        }

        internal static ScheduledHandle AddCoroutine(IEnumerator routine)
        {
            var runner = EnsureInstance();
            var entry = new CoroutineEntry { id = runner._nextId++ };
            runner._coroutines[entry.id] = entry;
            entry.coroutine = runner.StartCoroutine(runner.DriveCoroutine(entry, routine));
            return new ScheduledHandle(entry.id);
        }

        internal static bool IsActive(int id)
        {
            if (!_instance) return false;
            var runner = _instance;

            if (runner._indexById.TryGetValue(id, out int index)) return !runner._active[index].completed;
            if (runner._coroutines.TryGetValue(id, out var entry)) return !entry.completed;

            int pending = runner.PendingIndex(id);
            return pending >= 0 && !runner._pending[pending].completed;
        }

        internal static void Cancel(int id)
        {
            if (!_instance) return;
            var runner = _instance;

            if (runner._indexById.TryGetValue(id, out int index))
            {
                runner._active[index].completed = true;
                return;
            }

            if (runner._coroutines.TryGetValue(id, out var entry))
            {
                entry.completed = true;
                if (entry.coroutine != null) runner.StopCoroutine(entry.coroutine);
                return;
            }

            int pending = runner.PendingIndex(id);
            if (pending >= 0)
            {
                var task = runner._pending[pending];
                task.completed = true;
                runner._pending[pending] = task;
            }
        }

        internal static void SetPaused(int id, bool paused)
        {
            if (!_instance) return;
            var runner = _instance;

            if (runner._indexById.TryGetValue(id, out int index))
            {
                runner._active[index].paused = paused;
                return;
            }

            if (runner._coroutines.TryGetValue(id, out var entry))
            {
                entry.paused = paused;
                return;
            }

            int pending = runner.PendingIndex(id);
            if (pending >= 0)
            {
                var task = runner._pending[pending];
                task.paused = paused;
                runner._pending[pending] = task;
            }
        }

        internal static void BindOwner(int id, UnityEngine.Object owner)
        {
            if (!_instance) return;
            var runner = _instance;

            if (runner._indexById.TryGetValue(id, out int index))
            {
                runner._active[index].owner = owner;
                runner._active[index].hasOwner = true;
                return;
            }

            if (runner._coroutines.TryGetValue(id, out var entry))
            {
                entry.owner = owner;
                entry.hasOwner = true;
                return;
            }

            int pending = runner.PendingIndex(id);
            if (pending >= 0)
            {
                var task = runner._pending[pending];
                task.owner = owner;
                task.hasOwner = true;
                runner._pending[pending] = task;
            }
        }

        private int PendingIndex(int id)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                if (_pending[i].id == id) return i;
            }
            return -1;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            float realtimeDelta = Time.unscaledDeltaTime;

            _ticking = true;
            int count = _count;
            for (int i = 0; i < count; i++)
            {
                Tick(ref _active[i], delta, realtimeDelta);
            }
            _ticking = false;

            CheckCoroutineOwners();
            Compact();
            FlushPending();
            CleanCoroutines();
        }

        private static void Tick(ref ScheduledTask task, float delta, float realtimeDelta)
        {
            if (task.completed) return;

            if (task.hasOwner && !task.owner)
            {
                task.completed = true;
                return;
            }

            if (task.paused) return;

            switch (task.kind)
            {
                case ScheduledTaskKind.Delay:
                    task.elapsed += task.useRealtime ? realtimeDelta : delta;
                    if (task.elapsed >= task.duration)
                    {
                        task.completed = true;
                        task.action?.Invoke();
                    }
                    break;

                case ScheduledTaskKind.Repeat:
                    task.elapsed += task.useRealtime ? realtimeDelta : delta;
                    if (task.elapsed >= task.duration)
                    {
                        task.elapsed = task.duration > 0f ? task.elapsed - task.duration : 0f;
                        task.action?.Invoke();
                        if (task.repeatsRemaining > 0)
                        {
                            task.repeatsRemaining--;
                            if (task.repeatsRemaining == 0)
                            {
                                task.onComplete?.Invoke();
                                task.completed = true;
                            }
                        }
                    }
                    break;

                case ScheduledTaskKind.WaitFrames:
                    task.frameElapsed++;
                    if (task.frameElapsed >= task.frameTarget)
                    {
                        task.completed = true;
                        task.action?.Invoke();
                    }
                    break;

                case ScheduledTaskKind.WaitUntil:
                    if (task.condition == null || task.condition.Invoke())
                    {
                        task.completed = true;
                        task.action?.Invoke();
                    }
                    break;

                case ScheduledTaskKind.WaitWhile:
                    if (task.condition == null || !task.condition.Invoke())
                    {
                        task.completed = true;
                        task.action?.Invoke();
                    }
                    break;
            }
        }

        private void CheckCoroutineOwners()
        {
            if (_coroutines.Count == 0) return;

            foreach (var entry in _coroutines.Values)
            {
                if (entry.completed || !entry.hasOwner || entry.owner) continue;
                entry.completed = true;
                if (entry.coroutine != null) StopCoroutine(entry.coroutine);
            }
        }

        private void Compact()
        {
            int i = 0;
            while (i < _count)
            {
                if (!_active[i].completed)
                {
                    i++;
                    continue;
                }

                int last = _count - 1;
                _indexById.Remove(_active[i].id);
                if (i != last)
                {
                    _active[i] = _active[last];
                    _indexById[_active[i].id] = i;
                }
                _active[last] = default;
                _count--;
            }
        }

        private void FlushPending()
        {
            if (_pending.Count == 0) return;

            for (int i = 0; i < _pending.Count; i++)
            {
                var task = _pending[i];
                if (task.completed) continue;
                EnsureCapacity(_count + 1);
                _indexById[task.id] = _count;
                _active[_count] = task;
                _count++;
            }
            _pending.Clear();
        }

        private void CleanCoroutines()
        {
            if (_coroutines.Count == 0) return;

            _coroutineRemovals.Clear();
            foreach (var pair in _coroutines)
            {
                if (pair.Value.completed) _coroutineRemovals.Add(pair.Key);
            }

            for (int i = 0; i < _coroutineRemovals.Count; i++)
            {
                _coroutines.Remove(_coroutineRemovals[i]);
            }
        }

        private IEnumerator DriveCoroutine(CoroutineEntry entry, IEnumerator routine)
        {
            while (true)
            {
                if (entry.completed) yield break;

                if (entry.hasOwner && !entry.owner)
                {
                    entry.completed = true;
                    yield break;
                }

                if (entry.paused)
                {
                    yield return null;
                    continue;
                }

                if (!routine.MoveNext())
                {
                    entry.completed = true;
                    yield break;
                }

                yield return routine.Current;
            }
        }
    }
}
