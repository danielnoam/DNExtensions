using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Attribute to define the minimum and maximum range limits for RangedFloat and RangedInt sliders in the Inspector.
    /// </summary>
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        /// <summary>
        /// Gets the minimum value for the range slider.
        /// </summary>
        public float Min { get; private set; }
        
        /// <summary>
        /// Gets the maximum value for the range slider.
        /// </summary>
        public float Max { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MinMaxRangeAttribute class.
        /// </summary>
        /// <param name="min">The minimum value for the range slider.</param>
        /// <param name="max">The maximum value for the range slider.</param>
        public MinMaxRangeAttribute(float min, float max)
        {
            Min = Mathf.Min(min, max);
            Max = Mathf.Max(min, max);
        }
    }
}