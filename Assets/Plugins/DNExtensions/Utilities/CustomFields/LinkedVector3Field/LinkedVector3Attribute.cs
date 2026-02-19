using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Draws a Vector3 field with copy, paste, reset buttons and an optional proportional lock.
    /// </summary>
    /// <example>
    /// <code>
    /// [LinkedVector3]
    /// public Vector3 offset;
    ///
    /// [LinkedVector3(showLock: true, resetX: 1f, resetY: 1f, resetZ: 1f)]
    /// public Vector3 scale;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public class LinkedVector3Attribute : PropertyAttribute
    {
        public bool ShowLock { get; }
        public float ResetX  { get; }
        public float ResetY  { get; }
        public float ResetZ  { get; }

        public LinkedVector3Attribute(bool showLock = false, float resetX = 0f, float resetY = 0f, float resetZ = 0f)
        {
            ShowLock = showLock;
            ResetX   = resetX;
            ResetY   = resetY;
            ResetZ   = resetZ;
        }
    }
}