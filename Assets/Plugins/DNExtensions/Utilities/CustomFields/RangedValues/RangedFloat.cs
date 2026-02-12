using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// A serializable struct that represents a range of float values with utility methods.
    /// Provides a min-max slider interface in the Unity Inspector when used with the MinMaxRangeAttribute.
    /// </summary>
    [System.Serializable]
    public struct RangedFloat
    {
        /// <summary>
        /// The minimum value of the range.
        /// </summary>
        public float minValue;
        
        /// <summary>
        /// The maximum value of the range.
        /// </summary>
        public float maxValue;

        /// <summary>
        /// Initializes a new instance of the RangedFloat struct.
        /// </summary>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        public RangedFloat(float min, float max)
        {
            minValue = min;
            maxValue = max;
        }

        /// <summary>
        /// Implicitly converts a float value to a RangedFloat with range from -value to +value.
        /// </summary>
        /// <param name="value">The value to convert (range will be -value to +value).</param>
        /// <returns>A RangedFloat with range from -value to +value.</returns>
        public static implicit operator RangedFloat(float value)
        {
            return new RangedFloat(-value, value);
        }
        
        /// <summary>
        /// Gets a random value within the range (inclusive of minValue, exclusive of maxValue).
        /// </summary>
        public float RandomValue => Random.Range(minValue, maxValue);
        
        /// <summary>
        /// Gets the size of the range (maxValue - minValue).
        /// </summary>
        public float Range => maxValue - minValue;
        
        /// <summary>
        /// Gets the average (middle) value of the range.
        /// </summary>
        public float Average => (minValue + maxValue) * 0.5f;
        
        /// <summary>
        /// Linearly interpolates between minValue and maxValue.
        /// </summary>
        /// <param name="t">The interpolation parameter (0 returns minValue, 1 returns maxValue).</param>
        /// <returns>The interpolated value between minValue and maxValue.</returns>
        public float Lerp(float t) => Mathf.Lerp(minValue, maxValue, t);
        
        /// <summary>
        /// Checks if the specified value is within the range (inclusive).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is within the range, false otherwise.</returns>
        public bool Contains(float value) => value >= minValue && value <= maxValue;
        
        /// <summary>
        /// Clamps the specified value to be within the range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The value clamped to be within minValue and maxValue.</returns>
        public float Clamp(float value) => Mathf.Clamp(value, minValue, maxValue);
        
        /// <summary>
        /// Returns a string representation of the range.
        /// </summary>
        /// <returns>A formatted string showing the range (min - max).</returns>
        public override string ToString() => $"({minValue:F2} - {maxValue:F2})";
    }
}