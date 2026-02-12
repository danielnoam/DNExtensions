using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// A serializable struct that represents a range of integer values with utility methods.
    /// Provides a min-max slider interface in the Unity Inspector when used with the MinMaxRangeAttribute.
    /// </summary>
    [System.Serializable]
    public struct RangedInt
    {
        /// <summary>
        /// The minimum value of the range.
        /// </summary>
        public int minValue;
        
        /// <summary>
        /// The maximum value of the range.
        /// </summary>
        public int maxValue;

        /// <summary>
        /// Initializes a new instance of the RangedInt struct.
        /// </summary>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        public RangedInt(int min, int max)
        {
            minValue = min;
            maxValue = max;
        }

        /// <summary>
        /// Implicitly converts an int value to a RangedInt with range from -value to +value.
        /// </summary>
        /// <param name="value">The value to convert (range will be -value to +value).</param>
        /// <returns>A RangedInt with range from -value to +value.</returns>
        public static implicit operator RangedInt(int value)
        {
            return new RangedInt(-value, value);
        }

        /// <summary>
        /// Gets a random value within the range (inclusive of both minValue and maxValue).
        /// </summary>
        public int RandomValue => Random.Range(minValue, maxValue + 1);
        
        /// <summary>
        /// Gets the size of the range (maxValue - minValue).
        /// </summary>
        public int Range => maxValue - minValue;
        
        /// <summary>
        /// Gets the average (middle) value of the range as a float.
        /// </summary>
        public float Average => (minValue + maxValue) * 0.5f;
        
        /// <summary>
        /// Linearly interpolates between minValue and maxValue, returning the result as an integer.
        /// </summary>
        /// <param name="t">The interpolation parameter (0 returns minValue, 1 returns maxValue).</param>
        /// <returns>The interpolated value between minValue and maxValue, rounded to the nearest integer.</returns>
        public int Lerp(float t) => Mathf.RoundToInt(Mathf.Lerp(minValue, maxValue, t));
        
        /// <summary>
        /// Checks if the specified value is within the range (inclusive).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is within the range, false otherwise.</returns>
        public bool Contains(int value) => value >= minValue && value <= maxValue;
        
        /// <summary>
        /// Clamps the specified value to be within the range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The value clamped to be within minValue and maxValue.</returns>
        public int Clamp(int value) => Mathf.Clamp(value, minValue, maxValue);
        
        /// <summary>
        /// Returns a string representation of the range.
        /// </summary>
        /// <returns>A formatted string showing the range (min - max).</returns>
        public override string ToString() => $"({minValue} - {maxValue})";
    }
}