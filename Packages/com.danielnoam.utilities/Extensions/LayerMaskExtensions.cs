using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// Creates a LayerMask containing only the given layer index
        /// </summary>
        public static LayerMask CreateFromLayer(this int layer)
        {
            return 1 << layer;
        }

        /// <summary>
        /// Checks whether the GameObject's layer is contained in the mask
        /// </summary>
        public static bool IsInLayerMask(this GameObject gameObject, LayerMask mask)
        {
            if (!gameObject) return false;
            return (mask.value & (1 << gameObject.layer)) != 0;
        }
    }
}
