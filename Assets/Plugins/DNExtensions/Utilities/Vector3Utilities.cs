using System;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Utility functions for Vector3 operations
    /// </summary>
    public static class Vector3Utilities
    {
        /// <summary>
        /// Adds a Vector3 to this Vector3
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="addition">The vector to add</param>
        /// <returns>A new Vector3 with the addition applied</returns>
        public static Vector3 Add(this Vector3 vector, Vector3 addition)
        {
            return vector + addition;
        }

        /// <summary>
        /// Adds values to individual components of the Vector3
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="x">Value to add to an X component</param>
        /// <param name="y">Value to add to a Y component</param>
        /// <param name="z">Value to add to a Z component</param>
        /// <returns>A new Vector3 with the additions applied</returns>
        public static Vector3 Add(this Vector3 vector, float x = 0f, float y = 0f, float z = 0f)
        {
            return new Vector3(vector.x + x, vector.y + y, vector.z + z);
        }

        /// <summary>
        /// Removes a Vector3 from this Vector3 (subtraction)
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="subtraction">The vector to remove</param>
        /// <returns>A new Vector3 with the subtraction applied</returns>
        public static Vector3 Remove(this Vector3 vector, Vector3 subtraction)
        {
            return vector - subtraction;
        }

        /// <summary>
        /// Removes values from individual components of the Vector3
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="x">Value to remove from X component</param>
        /// <param name="y">Value to remove from Y component</param>
        /// <param name="z">Value to remove from Z component</param>
        /// <returns>A new Vector3 with the subtractions applied</returns>
        public static Vector3 Remove(this Vector3 vector, float x = 0f, float y = 0f, float z = 0f)
        {
            return new Vector3(vector.x - x, vector.y - y, vector.z - z);
        }

        /// <summary>
        /// Adds a value to the X component only
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The value to add to X</param>
        /// <returns>A new Vector3 with modified X component</returns>
        public static Vector3 AddX(this Vector3 vector, float value)
        {
            return new Vector3(vector.x + value, vector.y, vector.z);
        }

        /// <summary>
        /// Adds a value to the Y component only
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The value to add to Y</param>
        /// <returns>A new Vector3 with modified Y component</returns>
        public static Vector3 AddY(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y + value, vector.z);
        }

        /// <summary>
        /// Adds a value to the Z component only
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The value to add to Z</param>
        /// <returns>A new Vector3 with modified Z component</returns>
        public static Vector3 AddZ(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y, vector.z + value);
        }

        /// <summary>
        /// Removes a value from the X component only
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The value to remove from X</param>
        /// <returns>A new Vector3 with modified X component</returns>
        public static Vector3 RemoveX(this Vector3 vector, float value)
        {
            return new Vector3(vector.x - value, vector.y, vector.z);
        }

        /// <summary>
        /// Removes a value from the Y component only
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The value to remove from Y</param>
        /// <returns>A new Vector3 with modified Y component</returns>
        public static Vector3 RemoveY(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y - value, vector.z);
        }

        /// <summary>
        /// Removes a value from the Z component only
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The value to remove from Z</param>
        /// <returns>A new Vector3 with modified Z component</returns>
        public static Vector3 RemoveZ(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y, vector.z - value);
        }

        /// <summary>
        /// Sets the X component to a specific value
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The new X value</param>
        /// <returns>A new Vector3 with modified X component</returns>
        public static Vector3 SetX(this Vector3 vector, float value)
        {
            return new Vector3(value, vector.y, vector.z);
        }

        /// <summary>
        /// Sets the Y component to a specific value
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The new Y value</param>
        /// <returns>A new Vector3 with modified Y component</returns>
        public static Vector3 SetY(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, value, vector.z);
        }

        /// <summary>
        /// Sets the Z component to a specific value
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="value">The new Z value</param>
        /// <returns>A new Vector3 with modified Z component</returns>
        public static Vector3 SetZ(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y, value);
        }

        /// <summary>
        /// Multiplies each component by corresponding values
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="x">Multiplier for X component</param>
        /// <param name="y">Multiplier for Y component</param>
        /// <param name="z">Multiplier for Z component</param>
        /// <returns>A new Vector3 with multiplied components</returns>
        public static Vector3 Multiply(this Vector3 vector, float x = 1f, float y = 1f, float z = 1f)
        {
            return new Vector3(vector.x * x, vector.y * y, vector.z * z);
        }

        /// <summary>
        /// Divides each component by corresponding values
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="x">Divisor for an X component</param>
        /// <param name="y">Divisor for a Y component</param>
        /// <param name="z">Divisor for a Z component</param>
        /// <returns>A new Vector3 with divided components</returns>
        public static Vector3 Divide(this Vector3 vector, float x = 1f, float y = 1f, float z = 1f)
        {
            return new Vector3(
                x != 0f ? vector.x / x : 0f,
                y != 0f ? vector.y / y : 0f,
                z != 0f ? vector.z / z : 0f
            );
        }

        /// <summary>
        /// Clamps all components between min and max values
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="min">Minimum value for all components</param>
        /// <param name="max">Maximum value for all components</param>
        /// <returns>A new Vector3 with clamped components</returns>
        public static Vector3 Clamp(this Vector3 vector, float min, float max)
        {
            return new Vector3(
                Mathf.Clamp(vector.x, min, max),
                Mathf.Clamp(vector.y, min, max),
                Mathf.Clamp(vector.z, min, max)
            );
        }

        /// <summary>
        /// Clamps each component between corresponding min and max values
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <param name="min">Minimum values for each component</param>
        /// <param name="max">Maximum values for each component</param>
        /// <returns>A new Vector3 with clamped components</returns>
        public static Vector3 Clamp(this Vector3 vector, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y),
                Mathf.Clamp(vector.z, min.z, max.z)
            );
        }

        /// <summary>
        /// Rounds all components to the nearest integer
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <returns>A new Vector3 with rounded components</returns>
        public static Vector3 Round(this Vector3 vector)
        {
            return new Vector3(
                Mathf.Round(vector.x),
                Mathf.Round(vector.y),
                Mathf.Round(vector.z)
            );
        }

        /// <summary>
        /// Gets the absolute value of all components
        /// </summary>
        /// <param name="vector">The original vector</param>
        /// <returns>A new Vector3 with absolute values</returns>
        public static Vector3 Abs(this Vector3 vector)
        {
            return new Vector3(
                Mathf.Abs(vector.x),
                Mathf.Abs(vector.y),
                Mathf.Abs(vector.z)
            );
        }

        /// <summary>
        /// Converts Vector3 to Vector2 by dropping the Z component
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <returns>A Vector2 with X and Y components</returns>
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        /// <summary>
        /// Converts Vector3 to Vector2 by dropping the specified component
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <param name="dropAxis">The axis to drop (0=X, 1=Y, 2=Z)</param>
        /// <returns>A Vector2 with the remaining components</returns>
        public static Vector2 ToVector2(this Vector3 vector, int dropAxis)
        {
            switch (dropAxis)
            {
                case 0: return new Vector2(vector.y, vector.z); // Drop X
                case 1: return new Vector2(vector.x, vector.z); // Drop Y
                case 2: return new Vector2(vector.x, vector.y); // Drop Z
                default: return new Vector2(vector.x, vector.y); // Default: drop Z
            }
        }

        /// <summary>
        /// Checks if all components are approximately equal to zero
        /// </summary>
        /// <param name="vector">The vector to check</param>
        /// <param name="tolerance">The tolerance for comparison</param>
        /// <returns>True if all components are approximately zero</returns>
        public static bool IsApproximatelyZero(this Vector3 vector, float tolerance = 0.01f)
        {
            return Mathf.Abs(vector.x) < tolerance && 
                   Mathf.Abs(vector.y) < tolerance && 
                   Mathf.Abs(vector.z) < tolerance;
        }

        /// <summary>
        /// Gets the component with the largest absolute value
        /// </summary>
        /// <param name="vector">The vector to analyze</param>
        /// <returns>The component with the largest absolute value</returns>
        public static float GetLargestComponent(this Vector3 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if (absX >= absY && absX >= absZ) return vector.x;
            if (absY >= absZ) return vector.y;
            return vector.z;
        }

        /// <summary>
        /// Gets the component with the smallest absolute value
        /// </summary>
        /// <param name="vector">The vector to analyze</param>
        /// <returns>The component with the smallest absolute value</returns>
        public static float GetSmallestComponent(this Vector3 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if (absX <= absY && absX <= absZ) return vector.x;
            if (absY <= absZ) return vector.y;
            return vector.z;
        }
    }
}