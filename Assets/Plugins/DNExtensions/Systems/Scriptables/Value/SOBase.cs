using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    public abstract class SOBase : ScriptableObject
    {
#pragma warning disable 0414
        [Tooltip("If true, the custom drawer will show the value field.")]
        [SerializeField] protected bool allowEditingInReference = true;
#pragma warning restore 0414
    }
}