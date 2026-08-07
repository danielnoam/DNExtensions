using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// Everything under a point in the Scene View, front to back, resolved the way Unity's own
    /// picking would resolve it.
    ///
    /// Unity gained a public HandleUtility.PickAllObjects in 6000.3, which is what its own selection
    /// menu is built on. Older editors have no such call, so the list is rebuilt by picking
    /// repeatedly and adding each hit to the ignore set until nothing is left — the same approach
    /// UnityEditor.SceneViewPicking takes internally, through an overload that has been public since
    /// well before 2022.3. The gate is deliberately 6000.3 rather than the first 6000.x: that is the
    /// earliest version the call was confirmed present in, and guessing lower would trade a working
    /// fallback for a compile error.
    /// </summary>
    internal static class SceneViewPicking
    {
        // Every iteration of the fallback must consume one candidate, so the loop ends on its own.
        // This is what stops it spinning if a pick ever fails to be excluded by the ignore list.
        private const int IterationLimit = 256;

#if UNITY_6000_3_OR_NEWER
        private static readonly List<Object> NativeResults = new List<Object>();
#else
        private static readonly List<GameObject> Ignore = new List<GameObject>();
#endif

        /// <summary>Fills the buffer with the GameObjects under the position, nearest first.</summary>
        public static void Gather(Vector2 guiPosition, int maxResults, List<GameObject> results)
        {
            results.Clear();
            if (maxResults <= 0) return;

#if UNITY_6000_3_OR_NEWER
            NativeResults.Clear();
            HandleUtility.PickAllObjects(guiPosition, NativeResults);

            foreach (Object candidate in NativeResults)
            {
                if (results.Count >= maxResults) break;

                // Not every entry is guaranteed to be a GameObject, and the same object can be
                // reported by more than one of its own renderers.
                if (candidate is GameObject gameObject && !results.Contains(gameObject)) results.Add(gameObject);
            }
#else
            Ignore.Clear();

            for (int i = 0; i < IterationLimit && results.Count < maxResults; i++)
            {
                GameObject hit = HandleUtility.PickGameObject(guiPosition, false, Ignore.ToArray(), null, out int _);
                if (!hit) break;

                Ignore.Add(hit);
                if (!results.Contains(hit)) results.Add(hit);
            }
#endif
        }
    }
}
