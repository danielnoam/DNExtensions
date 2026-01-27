using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// A weighted chance-based collection that allows random selection of items based on percentage probabilities.
    /// Items can have their chances locked to prevent normalization, and the collection automatically 
    /// maintains a total of 100% across all unlocked items.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the chance list</typeparam>
    [Serializable]
    public class ChanceList<T>
    {
        /// <summary>
        /// Internal structure representing an item with its associated chance and lock state
        /// </summary>
        [Serializable]
        private struct InternalChanceItem
        {
            /// <summary>
            /// The item stored in this chance entry
            /// </summary>
            public T item;
            
            /// <summary>
            /// The percentage chance (0-100) for this item to be selected
            /// </summary>
            [Range(0, 100)] public int chance;
            
            /// <summary>
            /// Whether this item's chance is locked from automatic normalization
            /// </summary>
            public bool isLocked;

            /// <summary>
            /// Initializes a new chance item with the specified parameters
            /// </summary>
            /// <param name="item">The item to store</param>
            /// <param name="chance">The percentage chance (0-100) for selection</param>
            /// <param name="isLocked">Whether the chance should be locked from normalization</param>
            public InternalChanceItem(T item, int chance = 10, bool isLocked = false)
            {
                this.item = item;
                this.chance = chance;
                this.isLocked = isLocked;
            }
        }

        /// <summary>
        /// The internal array storing all chance items
        /// </summary>
        [SerializeField] private InternalChanceItem[] internalItems = Array.Empty<InternalChanceItem>();

        #region Public API

        /// <summary>
        /// Gets the number of items currently in the chance list
        /// </summary>
        /// <value>The total count of items in the collection</value>
        public int Count => internalItems.Length;

        /// <summary>
        /// Gets or sets the item at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set</param>
        /// <returns>The item at the specified index</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside the valid range</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= internalItems.Length)
                    throw new IndexOutOfRangeException(
                        $"Index {index} is out of range for ChanceList with {internalItems.Length} items");
                return internalItems[index].item;
            }
            set
            {
                if (index < 0 || index >= internalItems.Length)
                    throw new IndexOutOfRangeException(
                        $"Index {index} is out of range for ChanceList with {internalItems.Length} items");
                internalItems[index].item = value;
            }
        }

        /// <summary>
        /// Adds a new item to the chance list with the specified parameters
        /// </summary>
        /// <param name="item">The item to add to the collection</param>
        /// <param name="chance">The initial percentage chance (0-100) for this item</param>
        /// <param name="isLocked">Whether this item's chance should be locked from normalization</param>
        /// <remarks>
        /// After adding the item, NormalizeChances() is automatically called to maintain 
        /// a total of 100% across all unlocked items.
        /// </remarks>
        public void AddItem(T item, int chance = 10, bool isLocked = false)
        {
            var newArray = new InternalChanceItem[internalItems.Length + 1];
            Array.Copy(internalItems, newArray, internalItems.Length);
            newArray[internalItems.Length] = new InternalChanceItem(item, chance, isLocked);
            internalItems = newArray;
            NormalizeChances();
        }

        /// <summary>
        /// Removes the item at the specified index from the chance list
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside the valid range</exception>
        /// <remarks>
        /// After removing the item, NormalizeChances() is automatically called to redistribute 
        /// percentages among remaining unlocked items.
        /// </remarks>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            var newArray = new InternalChanceItem[internalItems.Length - 1];
            Array.Copy(internalItems, 0, newArray, 0, index);
            Array.Copy(internalItems, index + 1, newArray, index, internalItems.Length - index - 1);
            internalItems = newArray;
            NormalizeChances();
        }

        /// <summary>
        /// Sets the chance percentage for the item at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the item</param>
        /// <param name="newChance">The new chance percentage (will be clamped to 0-100)</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside the valid range</exception>
        /// <remarks>
        /// The chance value is automatically clamped to the range 0-100. After setting the chance,
        /// NormalizeChances() is called to maintain proper percentage distribution.
        /// </remarks>
        public void SetChance(int index, int newChance)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            internalItems[index].chance = Mathf.Clamp(newChance, 0, 100);
            NormalizeChances();
        }

        /// <summary>
        /// Gets the current chance percentage for the item at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the item</param>
        /// <returns>The chance percentage (0-100) for the specified item</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside the valid range</exception>
        public int GetChance(int index)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            return internalItems[index].chance;
        }

        /// <summary>
        /// Sets the locked state for the item at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the item</param>
        /// <param name="locked">True to lock the item's chance from normalization, false to unlock</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside the valid range</exception>
        /// <remarks>
        /// Locked items maintain their current chance value during normalization operations.
        /// After changing the lock state, NormalizeChances() is called to redistribute 
        /// percentages among unlocked items.
        /// </remarks>
        public void SetLocked(int index, bool locked)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            internalItems[index].isLocked = locked;
            NormalizeChances();
        }

        /// <summary>
        /// Gets the locked state for the item at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the item</param>
        /// <returns>True if the item's chance is locked from normalization, false otherwise</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside the valid range</exception>
        public bool IsLocked(int index)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            return internalItems[index].isLocked;
        }

        /// <summary>
        /// Removes all items from the chance list, resetting it to an empty state
        /// </summary>
        public void Clear()
        {
            internalItems = Array.Empty<InternalChanceItem>();
        }

        /// <summary>
        /// Manually normalizes all chance values to ensure they total 100%
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method redistributes percentage values among unlocked items to maintain a total of 100%.
        /// Locked items retain their current chance values and are excluded from normalization.
        /// </para>
        /// <para>
        /// The normalization process:
        /// 1. Calculates the total percentage used by locked items
        /// 2. Distributes the remaining percentage (100% - locked total) among unlocked items
        /// 3. If unlocked items have zero total, distributes remaining percentage equally
        /// 4. Handles rounding errors by adjusting values to ensure exact 100% total
        /// 5. Ensures no negative values exist after normalization
        /// </para>
        /// </remarks>
        public void NormalizeChances()
        {
            if (internalItems.Length == 0) return;

            // Separate locked and unlocked entries
            var unlockedIndices = new List<int>();
            int lockedTotal = 0;

            for (int i = 0; i < internalItems.Length; i++)
            {
                if (internalItems[i].isLocked)
                {
                    lockedTotal += Mathf.Max(0, internalItems[i].chance);
                }
                else
                {
                    unlockedIndices.Add(i);
                }
            }

            // If all entries are locked, don't normalize
            if (unlockedIndices.Count == 0) return;

            // Calculate the remaining percentage for unlocked entries
            int remainingPercentage = Mathf.Max(0, 100 - lockedTotal);

            // Calculate the total of unlocked chances
            int unlockedTotal = 0;
            foreach (int index in unlockedIndices)
            {
                unlockedTotal += Mathf.Max(0, internalItems[index].chance);
            }

            // If the unlocked total is 0, set equal chances for unlocked entries
            if (unlockedTotal <= 0)
            {
                int equalChance = remainingPercentage / unlockedIndices.Count;
                int remainder = remainingPercentage % unlockedIndices.Count;

                for (int i = 0; i < unlockedIndices.Count; i++)
                {
                    int index = unlockedIndices[i];
                    internalItems[index].chance = equalChance + (i < remainder ? 1 : 0);
                }
            }
            // If the unlocked total doesn't match the remaining percentage, normalize unlocked entries
            else if (unlockedTotal != remainingPercentage)
            {
                int newTotal = 0;

                // First pass: calculate normalized values for unlocked entries only
                foreach (int index in unlockedIndices)
                {
                    int normalizedChance =
                        Mathf.RoundToInt((internalItems[index].chance / (float)unlockedTotal) * remainingPercentage);
                    internalItems[index].chance = normalizedChance;
                    newTotal += normalizedChance;
                }

                // Second pass: adjust for rounding errors to ensure unlocked total = remainingPercentage
                int difference = remainingPercentage - newTotal;
                if (difference != 0 && unlockedIndices.Count > 0)
                {
                    // Sort unlocked indices by current chance value (descending) to adjust larger values first
                    unlockedIndices.Sort((a, b) => internalItems[b].chance.CompareTo(internalItems[a].chance));

                    // Distribute the difference, ensuring no negative values
                    for (int i = 0; i < Mathf.Abs(difference) && i < unlockedIndices.Count; i++)
                    {
                        int index = unlockedIndices[i];
                        if (difference > 0)
                        {
                            internalItems[index].chance += 1;
                        }
                        else if (internalItems[index].chance > 0) // Only subtract if we won't go negative
                        {
                            internalItems[index].chance -= 1;
                        }
                    }
                }
            }

            // Final safety check: ensure no negative values in all entries
            for (int i = 0; i < internalItems.Length; i++)
            {
                if (internalItems[i].chance <= 0)
                {
                    internalItems[i].chance = 0;
                }
            }
        }
        
        /// <summary>
        /// Returns the ChanceList as a List
        /// </summary>
        public List<T> ToList()
        {
            var items = new List<T>();
            foreach (var item in internalItems)
            {
                items.Add(item.item);
            }
            return items;
        }

        /// <summary>
        /// Removes the first occurrence of the specified item from the chance list
        /// </summary>
        /// <param name="item">The item to remove from the collection</param>
        public void Remove(T item)
        {
            int index = -1;
            for (int i = 0; i < internalItems.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(internalItems[i].item, item))
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                RemoveAt(index);
            }
        }
        
        
        /// <summary>
        /// Returns the index of the first occurrence of the specified item in the chance list
        /// </summary>
        /// <param name="item">The item to locate in the collection</param>
        /// <returns>
        /// The zero-based index of the first occurrence of the item, or -1 if not found
        /// </returns>
        public int IndexOf(T item)
        {
            for (int i = 0; i < internalItems.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(internalItems[i].item, item))
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion Public API

        #region Random Selection

        /// <summary>
        /// Selects and returns a random item based on the weighted chance percentages
        /// </summary>
        /// <returns>
        /// A randomly selected item from the collection, or default(T) if no valid items exist
        /// or the collection is empty
        /// </returns>
        /// <remarks>
        /// <para>
        /// Items with higher chance percentages are more likely to be selected. Items with 0% chance
        /// are excluded from selection. The method can return default(T) which allows for "nothing" 
        /// results when that's the intended behavior.
        /// </para>
        /// <para>
        /// The selection algorithm:
        /// 1. Filters items to only include those with chance > 0
        /// 2. Calculates total weight from all valid items
        /// 3. Generates a random value within the total weight range
        /// 4. Iterates through items, accumulating weights until the random value is reached
        /// </para>
        /// </remarks>
        public T GetRandomItem()
        {
            if (internalItems.Length == 0) return default(T);

            // Include ALL entries (even null items for "nothing")
            var validItems = new List<InternalChanceItem>();
            foreach (var item in internalItems)
            {
                if (item.chance > 0) // Only need chance > 0, item can be null
                {
                    validItems.Add(item);
                }
            }

            if (validItems.Count == 0) return default(T);

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var item in validItems)
            {
                totalWeight += item.chance;
            }

            if (totalWeight <= 0f) return validItems[0].item; // Could be default(T)

            // Select random item based on weights
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var item in validItems)
            {
                currentWeight += item.chance;
                if (randomValue <= currentWeight)
                {
                    return item.item; // Could return default(T) for "nothing"
                }
            }

            // Fallback
            return validItems[0].item;
        }

        /// <summary>
        /// Gets multiple random items from the collection, allowing duplicates
        /// </summary>
        /// <param name="count">The number of items to randomly select</param>
        /// <returns>An array containing the randomly selected items</returns>
        /// <remarks>
        /// Each selection is independent, so the same item can be selected multiple times.
        /// This is useful for scenarios like loot drops where you want multiple rolls
        /// with the same probabilities.
        /// </remarks>
        public T[] GetRandomItems(int count)
        {
            var results = new T[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = GetRandomItem();
            }

            return results;
        }

        /// <summary>
        /// Gets multiple unique random items from the collection without duplicates
        /// </summary>
        /// <param name="count">The number of unique items to select (will be limited by available items)</param>
        /// <returns>An array containing unique randomly selected items</returns>
        /// <remarks>
        /// <para>
        /// Items are removed from the selection pool after being chosen, ensuring no duplicates.
        /// The actual number of items returned may be less than requested if there aren't 
        /// enough valid items in the collection.
        /// </para>
        /// <para>
        /// This method is useful for scenarios like drawing cards from a deck or selecting
        /// unique rewards where repetition should be avoided.
        /// </para>
        /// </remarks>
        public T[] GetUniqueRandomItems(int count)
        {
            if (count <= 0) return Array.Empty<T>();

            var availableItems = new List<InternalChanceItem>();
            foreach (var item in internalItems)
            {
                if (item.chance > 0)
                {
                    availableItems.Add(item);
                }
            }

            count = Mathf.Min(count, availableItems.Count);
            var results = new T[count];

            for (int i = 0; i < count; i++)
            {
                if (availableItems.Count == 0) break;

                // Calculate the total weight of remaining items
                float totalWeight = 0f;
                foreach (var item in availableItems)
                {
                    totalWeight += item.chance;
                }

                if (totalWeight <= 0f)
                {
                    results[i] = availableItems[0].item;
                    availableItems.RemoveAt(0);
                    continue;
                }

                // Select random item
                float randomValue = Random.Range(0f, totalWeight);
                float currentWeight = 0f;

                for (int j = 0; j < availableItems.Count; j++)
                {
                    currentWeight += availableItems[j].chance;
                    if (randomValue <= currentWeight)
                    {
                        results[i] = availableItems[j].item;
                        availableItems.RemoveAt(j);
                        break;
                    }
                }
            }

            return results;
        }

        #endregion Random Selection
    }
}