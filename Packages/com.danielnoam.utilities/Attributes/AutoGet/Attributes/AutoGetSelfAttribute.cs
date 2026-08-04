using System;
using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Automatically gets a component reference from the same GameObject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoGetSelfAttribute : AutoGetAttribute
    {
        protected internal override IEnumerable<UnityEngine.Object> GetCandidates(
            MonoBehaviour behaviour, 
            Type fieldType)
        {
            return behaviour.GetComponents(fieldType);
        }
    }
}
