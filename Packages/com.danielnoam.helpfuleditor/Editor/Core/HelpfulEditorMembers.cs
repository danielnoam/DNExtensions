using System;
using System.Collections.Generic;
using System.Reflection;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Reflection lookups cached per type.
    ///
    /// A member's name resolves to the same thing for the lifetime of a type, but the lookup itself
    /// is a walk of the member table with string comparison, continuing up the base types on a miss.
    /// Doing that once is nothing; doing it per component per repaint is the shape of every
    /// reflection performance problem this suite has had.
    ///
    /// Misses are cached too — "this type does not have that member" is an answer worth keeping, and
    /// on the paths that call these it is usually the common one.
    /// </summary>
    internal static class HelpfulEditorMembers
    {
        private static readonly Dictionary<(Type type, string name, BindingFlags flags), PropertyInfo> Properties =
            new Dictionary<(Type, string, BindingFlags), PropertyInfo>();

        private static readonly Dictionary<(Type type, Type attribute), Attribute[]> Attributes =
            new Dictionary<(Type, Type), Attribute[]>();

        public static PropertyInfo Property(Type type, string name, BindingFlags flags)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;

            (Type, string, BindingFlags) key = (type, name, flags);

            if (Properties.TryGetValue(key, out PropertyInfo cached)) return cached;

            PropertyInfo property = null;

            try
            {
                property = type.GetProperty(name, flags);
            }
            catch (AmbiguousMatchException)
            {
                // A property redeclared down the chain. Nothing here needs to pick between them, and
                // guessing would be worse than reporting none.
            }

            Properties[key] = property;
            return property;
        }

        /// <summary>Attributes of a type, including inherited ones. Empty rather than null when there are none.</summary>
        public static T[] AttributesOf<T>(Type type) where T : Attribute
        {
            if (type == null) return Array.Empty<T>();

            (Type, Type) key = (type, typeof(T));

            if (Attributes.TryGetValue(key, out Attribute[] cached)) return (T[])cached;

            T[] found = (T[])type.GetCustomAttributes(typeof(T), true);

            Attributes[key] = found;
            return found;
        }
    }
}
