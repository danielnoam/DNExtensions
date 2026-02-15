
using System.Collections.Generic;
using DNExtensions.Utilities;
using DNExtensions.Utilities.Button;
using UnityEngine;


namespace DNExtensions.Systems.Rewind
{
    
    [DisallowMultipleComponent]
    public class RewindManager : MonoBehaviour
    {
        public static RewindManager Instance;
        
        [Header("Settings")]
        [Tooltip("The duration between frames while rewinding")]
        [SerializeField, Range(0.5f, 3f)] private float rewindSpeed = 1f;

        [Separator("Debug")]
        [SerializeField, ReadOnly] private RewindState rewindState = RewindState.Idle;
        [SerializeField, ReadOnly] private int rewindFrame;
        [SerializeField, ReadOnly] private float rewindAccumulator;


        private readonly List<Rewindable> _rewindables  = new List<Rewindable>();

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
            
            rewindFrame = 0;
            rewindAccumulator = 0f;
        }

        private void Update()
        {
            switch (rewindState)
            {
                case RewindState.Idle:
                    break;
                case RewindState.Recording:
                    
                    rewindFrame++;
                    Record(rewindFrame);
                    
                    
                    break;
                case RewindState.Rewinding:
                    
                    rewindAccumulator += rewindSpeed;
                    int framesToRewind = Mathf.FloorToInt(rewindAccumulator);
                    rewindAccumulator -= framesToRewind;
    
                    rewindFrame = Mathf.Max(0, rewindFrame - framesToRewind);
    
                    Rewind(rewindFrame);
    
                    if (rewindFrame == 0)
                    {
                        StartRecording();
                    }
                    break;
            }
            
            
            
            if  (Input.GetKeyUp(KeyCode.Alpha1))
            {
                StartRecording();
            }
            else if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                StartRewinding();
            }
            else if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                Idle();
            }
        }

        private void Rewind(int frame)
        {
            foreach (var rewindable in _rewindables)
            {
                rewindable.Rewind(frame);
            }
            
        }
        
        private void Record(int frame)
        {
            foreach (var rewindable in _rewindables)
            {
                rewindable.Record(frame);
            }
        }
        
        private void ClearFutureFrames(int frame)
        {
            foreach (var rewindable in _rewindables)
            {
                rewindable.ClearFutureFrames(frame);
            }
        }


        public void AddRewindable(Rewindable rewindable)
        {
            if (!_rewindables.Contains(rewindable))
            {
                _rewindables.Add(rewindable);
            }
        }
        
        public void RemoveRewindable(Rewindable rewindable)
        {
            if (_rewindables.Contains(rewindable))
            {
                _rewindables.Remove(rewindable);
            }
        }
        
        
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void StartRecording()
        {
            if (rewindState == RewindState.Recording) return;
            
            if (rewindState == RewindState.Rewinding)
            {
                ClearFutureFrames(rewindFrame);
            }
            else if (rewindState == RewindState.Idle)
            {
                rewindFrame = 0;
            }
            rewindState = RewindState.Recording;
            rewindAccumulator = 0f;
        }
        
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void StartRewinding()
        {
            if (rewindState == RewindState.Rewinding) return;
            
            if (rewindState != RewindState.Rewinding)
            {
                ClearFutureFrames(rewindFrame);
            }
            rewindState = RewindState.Rewinding;
        }
        
        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void Idle()
        {
            rewindState = RewindState.Idle;
        }
    }

}