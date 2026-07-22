using UnityEngine;

namespace DNExtensions.Systems.Scheduling
{
    /// <summary>
    /// Lightweight reference to a scheduled task. Safe to keep, copy, and use after the task has finished:
    /// operations on a stale handle are silent no-ops because task ids are never reused within a session.
    /// </summary>
    public readonly struct ScheduledHandle
    {
        private readonly int _id;

        internal ScheduledHandle(int id)
        {
            _id = id;
        }

        public bool IsActive => SchedulerRunner.IsActive(_id);

        public void Cancel()
        {
            SchedulerRunner.Cancel(_id);
        }

        public void Pause()
        {
            SchedulerRunner.SetPaused(_id, true);
        }

        public void Resume()
        {
            SchedulerRunner.SetPaused(_id, false);
        }

        /// <summary>
        /// Ties the task to an owner. The task is cancelled automatically the frame after the owner is destroyed,
        /// preventing callbacks from firing on a dead object.
        /// </summary>
        public ScheduledHandle BindTo(Object owner)
        {
            SchedulerRunner.BindOwner(_id, owner);
            return this;
        }
    }
}
