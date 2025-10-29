using System;
using System.Collections.Generic;
using UnityEngine;


namespace DNExtensions.Rewind
{
    public class RewindManager : MonoBehaviour
    {

        public static RewindManager Instance;
        
        
        
        [Header("Settings")]
        [SerializeField] private RewindState rewindState = RewindState.Idle;

        
        
        private readonly List<float> _recordedFrames = new List<float>();
        private readonly List<BaseRewindable> _rewindables  = new List<BaseRewindable>();
        private enum RewindState { Idle, Recording, Rewinding }


        
        
        
        
        
        private void Awake()
        {
            if (!Instance || Instance == this)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Rewind(float frame)
        {
            foreach (var rewindable in _rewindables)
            {
                rewindable.Rewind(frame);
            }
            
        }
        
        private void Record(float frame)
        {
            foreach (var rewindable in _rewindables)
            {
                rewindable.Record(frame);
            }
        }


        public void AddRewindable(BaseRewindable baseRewindable)
        {
            if (!_rewindables.Contains(baseRewindable))
            {
                _rewindables.Add(baseRewindable);
            }
        }
        
        public void RemoveRewindable(BaseRewindable baseRewindable)
        {
            if (_rewindables.Contains(baseRewindable))
            {
                _rewindables.Remove(baseRewindable);
            }
        }
        
        
        
        
        
    }

}

