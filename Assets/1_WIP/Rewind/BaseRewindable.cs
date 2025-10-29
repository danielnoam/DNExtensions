using System;
using UnityEngine;

namespace DNExtensions.Rewind
{
    public abstract class BaseRewindable : MonoBehaviour
    {
        
        
        
        
        
        protected virtual void Start()
        {
            RewindManager.Instance.AddRewindable(this);
        }
        
        protected virtual void OnDestroy()
        {
            RewindManager.Instance.RemoveRewindable(this);
        }
        
        
        
        public abstract void Record(float frame);
        public abstract void Rewind(float frame);
        
    }
}