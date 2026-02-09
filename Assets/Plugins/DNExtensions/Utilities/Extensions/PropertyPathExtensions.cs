using System;
using Unity.Properties;

namespace DNExtensions.Utilities
{
    public static class PropertyPathExtensions
    {
        /// <summary>
        /// Converts a string representation of a property path into a PropertyPath object
        /// </summary>
        /// <param name="pathString">A string representing the property path. Can include dot-separated property names and array indices in square brackets</param>
        /// <returns>A PropertyPath object representing the parsed path</returns>
        /// <exception cref="ArgumentException">Thrown when the input string is null, empty, or whitespace</exception>
        /// <exception cref="FormatException">Thrown when the path contains an invalid array index or unmatched square brackets</exception>
        /// <example>
        /// Valid path strings:
        /// "propertyName"
        /// "parent.child"
        /// "array[0]"
        /// "parent.children[2].name"
        /// "matrix[0][1]"
        /// </example>
        public static PropertyPath ToPropertyPath(this string pathString)
        {
            if (string.IsNullOrWhiteSpace(pathString))
                throw new ArgumentException("Path string cannot be null, empty, or whitespace.", nameof(pathString));

            var path = default(PropertyPath);

            foreach (var part in pathString.Split('.'))
            {
                int bracketStart = part.IndexOf('[');

                // No array indices in this part
                if (bracketStart < 0)
                {
                    path = PropertyPath.AppendName(path, part);
                    continue;
                }

                // Append property name before the first bracket
                if (bracketStart > 0)
                {
                    path = PropertyPath.AppendName(path, part[..bracketStart]);
                }

                // Process all array indices in this part
                int bracketEnd;
                while ((bracketEnd = part.IndexOf(']', bracketStart)) >= 0)
                {
                    string indexStr = part[(bracketStart + 1)..bracketEnd];

                    if (!int.TryParse(indexStr, out var index))
                        throw new FormatException($"Invalid array index '{indexStr}' in path: {pathString}");

                    if (index < 0)
                        throw new FormatException($"Array index cannot be negative: {index} in path: {pathString}");

                    path = PropertyPath.AppendIndex(path, index);

                    // Look for next bracket
                    bracketStart = part.IndexOf('[', bracketEnd);
                    if (bracketStart < 0) break;
                }

                // Check for unmatched brackets
                if (bracketStart >= 0)
                    throw new FormatException($"Unmatched '[' in path part: {part}");
            }

            return path;
        }

        /// <summary>
        /// Tries to convert a string to a PropertyPath, returning false if the conversion fails
        /// </summary>
        /// <param name="pathString">The string to convert</param>
        /// <param name="propertyPath">The resulting PropertyPath if successful</param>
        /// <returns>True if conversion was successful, false otherwise</returns>
        public static bool TryToPropertyPath(this string pathString, out PropertyPath propertyPath)
        {
            propertyPath = default;

            try
            {
                propertyPath = pathString.ToPropertyPath();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a PropertyPath back to a string representation
        /// </summary>
        public static string ToPathString(this PropertyPath propertyPath)
        {
            return propertyPath.ToString();
        }

        /// <summary>
        /// Checks if a property path string is valid
        /// </summary>
        public static bool IsValidPropertyPath(this string pathString)
        {
            return pathString.TryToPropertyPath(out _);
        }
    }
}