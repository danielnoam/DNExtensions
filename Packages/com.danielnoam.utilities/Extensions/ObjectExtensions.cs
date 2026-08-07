using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Returns the object, or a real null if it has been destroyed, so null-coalescing operators behave correctly
        /// </summary>
        public static T OrNull<T>(this T obj) where T : Object
        {
            return obj ? obj : null;
        }
    }
}
