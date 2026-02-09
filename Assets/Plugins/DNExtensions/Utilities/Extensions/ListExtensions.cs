using System;
using System.Collections.Generic;
using System.Linq;

namespace DNExtensions.Utilities
{
    public static class ListExtensions
    {
        private static Random _rng;

        #region Null and Empty Checks

        /// <summary>
        /// Determines whether a collection is null or has no elements
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>
        /// Determines whether a collection is null or has no elements
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        #endregion

        #region Cloning

        /// <summary>
        /// Creates a shallow copy of the list
        /// </summary>
        public static List<T> Clone<T>(this IList<T> list)
        {
            if (list == null) return null;
            return new List<T>(list);
        }

        #endregion

        #region Element Manipulation

        /// <summary>
        /// Swaps two elements in the list at the specified indices
        /// </summary>
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            if (list == null) return;
            if (indexA < 0 || indexA >= list.Count || indexB < 0 || indexB >= list.Count)
            {
                throw new ArgumentOutOfRangeException("Index out of range");
            }

            (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
        }

        /// <summary>
        /// Removes the first element from the list and returns it
        /// </summary>
        public static T PopFirst<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Cannot pop from empty list");

            T item = list[0];
            list.RemoveAt(0);
            return item;
        }

        /// <summary>
        /// Removes the last element from the list and returns it
        /// </summary>
        public static T PopLast<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Cannot pop from empty list");

            int lastIndex = list.Count - 1;
            T item = list[lastIndex];
            list.RemoveAt(lastIndex);
            return item;
        }

        #endregion

        #region Randomization

        /// <summary>
        /// Shuffles the list in-place using Fisher-Yates algorithm
        /// </summary>
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            if (list == null || list.Count <= 1) return list;

            _rng ??= new Random();

            int count = list.Count;
            while (count > 1)
            {
                --count;
                int index = _rng.Next(count + 1);
                (list[index], list[count]) = (list[count], list[index]);
            }

            return list;
        }

        /// <summary>
        /// Returns a random element from the list
        /// </summary>
        public static T Random<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Cannot get random element from empty list");

            _rng ??= new Random();
            return list[_rng.Next(list.Count)];
        }

        /// <summary>
        /// Returns a random element from the list, or default if empty
        /// </summary>
        public static T RandomOrDefault<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;

            _rng ??= new Random();
            return list[_rng.Next(list.Count)];
        }

        #endregion

        #region Filtering and Selection

        /// <summary>
        /// Filters a collection based on a predicate
        /// </summary>
        public static List<T> Filter<T>(this IList<T> source, Predicate<T> predicate)
        {
            if (source == null) return new List<T>();

            List<T> result = new List<T>();
            foreach (T item in source)
            {
                if (predicate(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the first element, or default if empty
        /// </summary>
        public static T FirstOrDefault<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[0];
        }

        /// <summary>
        /// Returns the last element, or default if empty
        /// </summary>
        public static T LastOrDefault<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[list.Count - 1];
        }

        #endregion

        #region Safe Access

        /// <summary>
        /// Gets element at index, or default if index is out of range
        /// </summary>
        public static T GetOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }

        /// <summary>
        /// Tries to get element at index
        /// </summary>
        public static bool TryGet<T>(this IList<T> list, int index, out T value)
        {
            if (list != null && index >= 0 && index < list.Count)
            {
                value = list[index];
                return true;
            }

            value = default;
            return false;
        }

        #endregion

        #region Adding and Removing

        /// <summary>
        /// Adds an item to the list if it doesn't already exist
        /// </summary>
        public static bool AddUnique<T>(this IList<T> list, T item)
        {
            if (list == null) return false;
            if (list.Contains(item)) return false;

            list.Add(item);
            return true;
        }

        /// <summary>
        /// Adds a range of items, only adding those that don't already exist
        /// </summary>
        public static void AddRangeUnique<T>(this IList<T> list, IEnumerable<T> items)
        {
            if (list == null || items == null) return;

            foreach (T item in items)
            {
                list.AddUnique(item);
            }
        }

        /// <summary>
        /// Removes all items that match the predicate
        /// </summary>
        public static int RemoveAll<T>(this IList<T> list, Predicate<T> predicate)
        {
            if (list == null) return 0;

            int count = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Foreach Extensions

        /// <summary>
        /// Executes an action for each element in the list
        /// </summary>
        public static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            if (list == null || action == null) return;

            foreach (T item in list)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes an action for each element with its index
        /// </summary>
        public static void ForEach<T>(this IList<T> list, Action<T, int> action)
        {
            if (list == null || action == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                action(list[i], i);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Returns a string representation of the list elements
        /// </summary>
        public static string ToStringElements<T>(this IList<T> list, string separator = ", ")
        {
            if (list == null || list.Count == 0)
                return string.Empty;

            return string.Join(separator, list);
        }

        /// <summary>
        /// Checks if the index is valid for this list
        /// </summary>
        public static bool IsValidIndex<T>(this IList<T> list, int index)
        {
            return list != null && index >= 0 && index < list.Count;
        }

        #endregion
    }
}