using UnityEngine;

namespace DNExtensions.Utilities
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Adds a Vector2 to this Vector2
        /// </summary>
        public static Vector2 Add(this Vector2 vector, Vector2 addition)
        {
            return vector + addition;
        }

        /// <summary>
        /// Adds values to individual components of the Vector2
        /// </summary>
        public static Vector2 Add(this Vector2 vector, float x = 0f, float y = 0f)
        {
            return new Vector2(vector.x + x, vector.y + y);
        }

        /// <summary>
        /// Sets any x y values of a Vector2 (null values are unchanged)
        /// </summary>
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
        {
            return new Vector2(x ?? vector.x, y ?? vector.y);
        }

        /// <summary>
        /// Removes a Vector2 from this Vector2 (subtraction)
        /// </summary>
        public static Vector2 Remove(this Vector2 vector, Vector2 subtraction)
        {
            return vector - subtraction;
        }

        /// <summary>
        /// Removes values from individual components of the Vector2
        /// </summary>
        public static Vector2 Remove(this Vector2 vector, float x = 0f, float y = 0f)
        {
            return new Vector2(vector.x - x, vector.y - y);
        }

        /// <summary>
        /// Adds a value to the X component only
        /// </summary>
        public static Vector2 AddX(this Vector2 vector, float value)
        {
            return new Vector2(vector.x + value, vector.y);
        }

        /// <summary>
        /// Adds a value to the Y component only
        /// </summary>
        public static Vector2 AddY(this Vector2 vector, float value)
        {
            return new Vector2(vector.x, vector.y + value);
        }

        /// <summary>
        /// Removes a value from the X component only
        /// </summary>
        public static Vector2 RemoveX(this Vector2 vector, float value)
        {
            return new Vector2(vector.x - value, vector.y);
        }

        /// <summary>
        /// Removes a value from the Y component only
        /// </summary>
        public static Vector2 RemoveY(this Vector2 vector, float value)
        {
            return new Vector2(vector.x, vector.y - value);
        }

        /// <summary>
        /// Sets the X component to a specific value
        /// </summary>
        public static Vector2 SetX(this Vector2 vector, float value)
        {
            return new Vector2(value, vector.y);
        }

        /// <summary>
        /// Sets the Y component to a specific value
        /// </summary>
        public static Vector2 SetY(this Vector2 vector, float value)
        {
            return new Vector2(vector.x, value);
        }

        /// <summary>
        /// Multiplies each component by corresponding values
        /// </summary>
        public static Vector2 Multiply(this Vector2 vector, float x = 1f, float y = 1f)
        {
            return new Vector2(vector.x * x, vector.y * y);
        }

        /// <summary>
        /// Divides each component by corresponding values
        /// </summary>
        public static Vector2 Divide(this Vector2 vector, float x = 1f, float y = 1f)
        {
            return new Vector2(
                x != 0f ? vector.x / x : 0f,
                y != 0f ? vector.y / y : 0f
            );
        }

        /// <summary>
        /// Clamps all components between min and max values
        /// </summary>
        public static Vector2 Clamp(this Vector2 vector, float min, float max)
        {
            return new Vector2(
                Mathf.Clamp(vector.x, min, max),
                Mathf.Clamp(vector.y, min, max)
            );
        }

        /// <summary>
        /// Clamps each component between corresponding min and max values
        /// </summary>
        public static Vector2 Clamp(this Vector2 vector, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.Clamp(vector.x, min.x, max.x),
                Mathf.Clamp(vector.y, min.y, max.y)
            );
        }

        /// <summary>
        /// Rounds all components to the nearest integer
        /// </summary>
        public static Vector2 Round(this Vector2 vector)
        {
            return new Vector2(
                Mathf.Round(vector.x),
                Mathf.Round(vector.y)
            );
        }

        /// <summary>
        /// Gets the absolute value of all components
        /// </summary>
        public static Vector2 Abs(this Vector2 vector)
        {
            return new Vector2(
                Mathf.Abs(vector.x),
                Mathf.Abs(vector.y)
            );
        }

        /// <summary>
        /// Converts Vector2 to Vector3 with Z set to zero
        /// </summary>
        public static Vector3 ToVector3(this Vector2 vector)
        {
            return new Vector3(vector.x, vector.y, 0f);
        }

        /// <summary>
        /// Converts Vector2 to Vector3 with the specified Z value
        /// </summary>
        public static Vector3 ToVector3(this Vector2 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        /// <summary>
        /// Converts Vector2 to Vector3 inserting the Z value at the specified axis
        /// </summary>
        /// <param name="vector">The Vector2 to convert</param>
        /// <param name="z">The Z value to insert</param>
        /// <param name="insertAxis">The axis to insert Z into (0=X, 1=Y, 2=Z)</param>
        public static Vector3 ToVector3(this Vector2 vector, float z, int insertAxis)
        {
            switch (insertAxis)
            {
                case 0: return new Vector3(z, vector.x, vector.y);
                case 1: return new Vector3(vector.x, z, vector.y);
                case 2: return new Vector3(vector.x, vector.y, z);
                default: return new Vector3(vector.x, vector.y, z);
            }
        }

        /// <summary>
        /// Checks if all components are approximately equal to zero
        /// </summary>
        public static bool IsApproximatelyZero(this Vector2 vector, float tolerance = 0.01f)
        {
            return Mathf.Abs(vector.x) < tolerance && 
                   Mathf.Abs(vector.y) < tolerance;
        }

        /// <summary>
        /// Returns whether the current Vector2 is in a given range from another Vector2
        /// </summary>
        /// <param name="current">The current Vector2 position</param>
        /// <param name="target">The Vector2 position to compare against</param>
        /// <param name="range">The range value to compare against</param>
        /// <returns>True if the current Vector2 is in the given range from the target Vector2</returns>
        public static bool InRangeOf(this Vector2 current, Vector2 target, float range)
        {
            return (current - target).sqrMagnitude <= range * range;
        }

        /// <summary>
        /// Gets the component with the largest absolute value
        /// </summary>
        public static float GetLargestComponent(this Vector2 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);

            return absX >= absY ? vector.x : vector.y;
        }

        /// <summary>
        /// Gets the component with the smallest absolute value
        /// </summary>
        public static float GetSmallestComponent(this Vector2 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);

            return absX <= absY ? vector.x : vector.y;
        }

        /// <summary>
        /// Rotates the vector by the specified angle in degrees
        /// </summary>
        public static Vector2 Rotate(this Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        /// <summary>
        /// Gets the angle of this vector in degrees
        /// </summary>
        public static float GetAngle(this Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Creates a Vector2 from an angle in degrees
        /// </summary>
        public static Vector2 FromAngle(float degrees, float magnitude = 1f)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * magnitude;
        }

        /// <summary>
        /// Computes a random point in an annulus (ring-shaped area) around the origin point
        /// </summary>
        /// <param name="origin">The center point of the annulus</param>
        /// <param name="minRadius">Minimum radius of the annulus</param>
        /// <param name="maxRadius">Maximum radius of the annulus</param>
        /// <returns>A random Vector2 point within the specified annulus</returns>
        public static Vector2 RandomPointInAnnulus(this Vector2 origin, float minRadius, float maxRadius)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            float minRadiusSquared = minRadius * minRadius;
            float maxRadiusSquared = maxRadius * maxRadius;
            float distance = Mathf.Sqrt(Random.value * (maxRadiusSquared - minRadiusSquared) + minRadiusSquared);

            Vector2 position = direction * distance;
            return origin + position;
        }
    }
}