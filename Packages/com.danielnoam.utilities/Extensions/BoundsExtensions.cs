using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class BoundsExtensions
    {
        /// <summary>
        /// Returns a copy of the bounds grown to contain the other bounds, leaving the original untouched
        /// </summary>
        public static Bounds ExpandToInclude(this Bounds bounds, Bounds other)
        {
            bounds.Encapsulate(other);
            return bounds;
        }
    }
}
