#if UNITY_EDITOR
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Generic asset information for selector popups.
    /// </summary>
    /// <typeparam name="T">Type of Unity asset (GameObject, ScriptableObject, etc)</typeparam>
    public struct AssetInfo<T> where T : Object
    {
        public T Asset;
        public string DisplayName;
        public string Path;
    }
}
#endif