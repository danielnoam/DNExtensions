using System;
using UnityEngine;

namespace DNExtensions.Utilities.Rewind
{
    
    
    [DisallowMultipleComponent]
    public abstract class Rewindable : MonoBehaviour
    {
        
        
        
        
        
        protected virtual void Start()
        {
            RewindManager.Instance.AddRewindable(this);
        }
        
        protected virtual void OnDestroy()
        {
            RewindManager.Instance.RemoveRewindable(this);
        }
        

        public abstract void Record(int frame);
        public abstract void Rewind(int frame);
        public abstract void ClearFutureFrames(int frame);

    }
}