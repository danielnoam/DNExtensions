using UnityEngine;

#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace DNExtensions.Utilities
{
    public static class NumberExtensions
    {
        #region Percentage and Ratios
        
        /// <summary>
        /// Calculates what percentage the part is of the whole
        /// </summary>
        public static float PercentageOf(this int part, int whole)
        {
            if (whole == 0) return 0f;
            return (float)part / whole;
        }

        /// <summary>
        /// Calculates what percentage the part is of the whole
        /// </summary>
        public static float PercentageOf(this float part, float whole)
        {
            if (Mathf.Approximately(whole, 0f)) return 0f;
            return part / whole;
        }

        #endregion

        #region Float Comparison

        /// <summary>
        /// Checks if two floats are approximately equal
        /// </summary>
        public static bool Approx(this float f1, float f2)
        {
            return Mathf.Approximately(f1, f2);
        }

        #endregion

        #region Integer Checks

        /// <summary>
        /// Checks if the integer is odd
        /// </summary>
        public static bool IsOdd(this int value)
        {
            return value % 2 == 1;
        }

        /// <summary>
        /// Checks if the integer is even
        /// </summary>
        public static bool IsEven(this int value)
        {
            return value % 2 == 0;
        }

        #endregion

        #region Clamping (AtLeast/AtMost)

        /// <summary>
        /// Ensures the value is at least the minimum
        /// </summary>
        public static int AtLeast(this int value, int min)
        {
            return Mathf.Max(value, min);
        }

        /// <summary>
        /// Ensures the value is at most the maximum
        /// </summary>
        public static int AtMost(this int value, int max)
        {
            return Mathf.Min(value, max);
        }

        /// <summary>
        /// Ensures the value is at least the minimum
        /// </summary>
        public static float AtLeast(this float value, float min)
        {
            return Mathf.Max(value, min);
        }

        /// <summary>
        /// Ensures the value is at most the maximum
        /// </summary>
        public static float AtMost(this float value, float max)
        {
            return Mathf.Min(value, max);
        }

        /// <summary>
        /// Ensures the value is at least the minimum
        /// </summary>
        public static double AtLeast(this double value, double min)
        {
            return value > min ? value : min;
        }

        /// <summary>
        /// Ensures the value is at most the maximum
        /// </summary>
        public static double AtMost(this double value, double max)
        {
            return value < max ? value : max;
        }

#if UNITY_MATHEMATICS
        /// <summary>
        /// Ensures the value is at least the minimum (Unity Mathematics half)
        /// </summary>
        public static half AtLeast(this half value, half min)
        {
            return value > min ? value : min;
        }

        /// <summary>
        /// Ensures the value is at most the maximum (Unity Mathematics half)
        /// </summary>
        public static half AtMost(this half value, half max)
        {
            return value < max ? value : max;
        }
#endif

        #endregion

        #region Clamping (Between)

        /// <summary>
        /// Clamps the value between min and max
        /// </summary>
        public static int Clamp(this int value, int min, int max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Clamps the value between min and max
        /// </summary>
        public static float Clamp(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Clamps the value between min and max
        /// </summary>
        public static double Clamp(this double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        #endregion

        #region Remapping

        /// <summary>
        /// Remaps a value from one range to another
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }

        /// <summary>
        /// Remaps a value from one range to another
        /// </summary>
        public static double Remap(this double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            double t = (value - fromMin) / (fromMax - fromMin);
            return toMin + t * (toMax - toMin);
        }

        #endregion

        #region Rounding

        /// <summary>
        /// Rounds the float to the nearest integer
        /// </summary>
        public static int RoundToInt(this float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Rounds the float to the nearest multiple of the step
        /// </summary>
        public static float RoundToNearest(this float value, float step)
        {
            return Mathf.Round(value / step) * step;
        }

        /// <summary>
        /// Floors the float to an integer
        /// </summary>
        public static int FloorToInt(this float value)
        {
            return Mathf.FloorToInt(value);
        }

        /// <summary>
        /// Ceils the float to an integer
        /// </summary>
        public static int CeilToInt(this float value)
        {
            return Mathf.CeilToInt(value);
        }

        #endregion

        #region Sign and Absolute

        /// <summary>
        /// Gets the absolute value
        /// </summary>
        public static int Abs(this int value)
        {
            return Mathf.Abs(value);
        }

        /// <summary>
        /// Gets the absolute value
        /// </summary>
        public static float Abs(this float value)
        {
            return Mathf.Abs(value);
        }

        /// <summary>
        /// Gets the sign of the value (-1, 0, or 1)
        /// </summary>
        public static int Sign(this int value)
        {
            if (value > 0) return 1;
            if (value < 0) return -1;
            return 0;
        }

        /// <summary>
        /// Gets the sign of the value (-1, 0, or 1)
        /// </summary>
        public static float Sign(this float value)
        {
            return Mathf.Sign(value);
        }

        #endregion

        #region Squares and Powers

        /// <summary>
        /// Returns the square of the value (value * value)
        /// </summary>
        public static int Squared(this int value)
        {
            return value * value;
        }

        /// <summary>
        /// Returns the square of the value (value * value)
        /// </summary>
        public static float Squared(this float value)
        {
            return value * value;
        }

        /// <summary>
        /// Returns the value raised to the specified power
        /// </summary>
        public static float Pow(this float value, float power)
        {
            return Mathf.Pow(value, power);
        }

        #endregion
    }
}