using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Adds a Vector3 to this Vector3
        /// </summary>
        public static Vector3 Add(this Vector3 vector, Vector3 addition)
        {
            return vector + addition;
        }

        /// <summary>
        /// Adds values to individual components of the Vector3
        /// </summary>
        public static Vector3 Add(this Vector3 vector, float x = 0f, float y = 0f, float z = 0f)
        {
            return new Vector3(vector.x + x, vector.y + y, vector.z + z);
        }

        /// <summary>
        /// Sets any x y z values of a Vector3 (null values are unchanged)
        /// </summary>
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

        /// <summary>
        /// Removes a Vector3 from this Vector3 (subtraction)
        /// </summary>
        public static Vector3 Remove(this Vector3 vector, Vector3 subtraction)
        {
            return vector - subtraction;
        }

        /// <summary>
        /// Removes values from individual components of the Vector3
        /// </summary>
        public static Vector3 Remove(this Vector3 vector, float x = 0f, float y = 0f, float z = 0f)
        {
            return new Vector3(vector.x - x, vector.y - y, vector.z - z);
        }

        /// <summary>
        /// Adds a value to the X component only
        /// </summary>
        public static Vector3 AddX(this Vector3 vector, float value)
        {
            return new Vector3(vector.x + value, vector.y, vector.z);
        }

        /// <summary>
        /// Adds a value to the Y component only
        /// </summary>
        public static Vector3 AddY(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y + value, vector.z);
        }

        /// <summary>
        /// Adds a value to the Z component only
        /// </summary>
        public static Vector3 AddZ(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y, vector.z + value);
        }

        /// <summary>
        /// Removes a value from the X component only
        /// </summary>
        public static Vector3 RemoveX(this Vector3 vector, float value)
        {
            return new Vector3(vector.x - value, vector.y, vector.z);
        }

        /// <summary>
        /// Removes a value from the Y component only
        /// </summary>
        public static Vector3 RemoveY(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y - value, vector.z);
        }

        /// <summary>
        /// Removes a value from the Z component only
        /// </summary>
        public static Vector3 RemoveZ(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y, vector.z - value);
        }

        /// <summary>
        /// Sets the X component to a specific value
        /// </summary>
        public static Vector3 SetX(this Vector3 vector, float value)
        {
            return new Vector3(value, vector.y, vector.z);
        }

        /// <summary>
        /// Sets the Y component to a specific value
        /// </summary>
        public static Vector3 SetY(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, value, vector.z);
        }

        /// <summary>
        /// Sets the Z component to a specific value
        /// </summary>
        public static Vector3 SetZ(this Vector3 vector, float value)
        {
            return new Vector3(vector.x, vector.y, value);
        }

        /// <summary>
        /// Multiplies each component by corresponding values
        /// </summary>
        public static Vector3 Multiply(this Vector3 vector, float x = 1f, float y = 1f, float z = 1f)
        {
            return new Vector3(vector.x * x, vector.y * y, vector.z * z);
        }

        /// <summary>
        /// Divides each component by corresponding values
        /// </summary>
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
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        /// <summary>
        /// Converts Vector3 to Vector2 by dropping the specified component
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <param name="dropAxis">The axis to drop (0=X, 1=Y, 2=Z)</param>
        public static Vector2 ToVector2(this Vector3 vector, int dropAxis)
        {
            switch (dropAxis)
            {
                case 0: return new Vector2(vector.y, vector.z);
                case 1: return new Vector2(vector.x, vector.z);
                case 2: return new Vector2(vector.x, vector.y);
                default: return new Vector2(vector.x, vector.y);
            }
        }

        /// <summary>
        /// Checks if all components are approximately equal to zero
        /// </summary>
        public static bool IsApproximatelyZero(this Vector3 vector, float tolerance = 0.01f)
        {
            return Mathf.Abs(vector.x) < tolerance && 
                   Mathf.Abs(vector.y) < tolerance && 
                   Mathf.Abs(vector.z) < tolerance;
        }

        /// <summary>
        /// Returns whether the current Vector3 is in a given range from another Vector3
        /// </summary>
        /// <param name="current">The current Vector3 position</param>
        /// <param name="target">The Vector3 position to compare against</param>
        /// <param name="range">The range value to compare against</param>
        /// <returns>True if the current Vector3 is in the given range from the target Vector3</returns>
        public static bool InRangeOf(this Vector3 current, Vector3 target, float range)
        {
            return (current - target).sqrMagnitude <= range * range;
        }

        /// <summary>
        /// Gets the component with the largest absolute value
        /// </summary>
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
        public static float GetSmallestComponent(this Vector3 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if (absX <= absY && absX <= absZ) return vector.x;
            if (absY <= absZ) return vector.y;
            return vector.z;
        }

        /// <summary>
        /// Computes a random point in an annulus (ring-shaped area) around the origin point
        /// </summary>
        /// <param name="origin">The center point of the annulus</param>
        /// <param name="minRadius">Minimum radius of the annulus</param>
        /// <param name="maxRadius">Maximum radius of the annulus</param>
        /// <returns>A random Vector3 point within the specified annulus (Y component matches origin)</returns>
        public static Vector3 RandomPointInAnnulus(this Vector3 origin, float minRadius, float maxRadius)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float minRadiusSquared = minRadius * minRadius;
            float maxRadiusSquared = maxRadius * maxRadius;
            float distance = Mathf.Sqrt(Random.value * (maxRadiusSquared - minRadiusSquared) + minRadiusSquared);

            Vector2 position = direction * distance;
            return new Vector3(origin.x + position.x, origin.y, origin.z + position.y);
        }
    }
}