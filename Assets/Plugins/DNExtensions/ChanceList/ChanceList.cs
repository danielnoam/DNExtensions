using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;


namespace DNExtensions
{
    [Serializable]

    public class ChanceList<T>
    {
        [Serializable]
        private struct InternalChanceItem
        {
            public T item;
            [Range(0, 100)] public int chance;
            public bool isLocked;

            public InternalChanceItem(T item, int chance = 10, bool isLocked = false)
            {
                this.item = item;
                this.chance = chance;
                this.isLocked = isLocked;
            }
        }

        [SerializeField] private InternalChanceItem[] internalItems = Array.Empty<InternalChanceItem>();

        #region Public API

        /// <summary>
        /// Number of items in the chance list
        /// </summary>
        public int Count => internalItems.Length;

        /// <summary>
        /// Get or set an item at the specified index
        /// </summary>
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
        /// Add a new item to the chance list
        /// </summary>
        public void AddItem(T item, int chance = 10, bool isLocked = false)
        {
            var newArray = new InternalChanceItem[internalItems.Length + 1];
            Array.Copy(internalItems, newArray, internalItems.Length);
            newArray[internalItems.Length] = new InternalChanceItem(item, chance, isLocked);
            internalItems = newArray;
            NormalizeChances();
        }

        /// <summary>
        /// Remove an item at the specified index
        /// </summary>
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
        /// Set the chance value for an item at the specified index
        /// </summary>
        public void SetChance(int index, int newChance)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            internalItems[index].chance = Mathf.Clamp(newChance, 0, 100);
            NormalizeChances();
        }

        /// <summary>
        /// Get the chance value for an item at the specified index
        /// </summary>
        public int GetChance(int index)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            return internalItems[index].chance;
        }

        /// <summary>
        /// Set the locked state for an item at the specified index
        /// </summary>
        public void SetLocked(int index, bool locked)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            internalItems[index].isLocked = locked;
            NormalizeChances();
        }

        /// <summary>
        /// Get the locked state for an item at the specified index
        /// </summary>
        public bool IsLocked(int index)
        {
            if (index < 0 || index >= internalItems.Length)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for ChanceList with {internalItems.Length} items");

            return internalItems[index].isLocked;
        }

        /// <summary>
        /// Clear all items from the chance list
        /// </summary>
        public void Clear()
        {
            internalItems = Array.Empty<InternalChanceItem>();
        }

        /// <summary>
        /// Manually normalize all chance values to ensure they total 100%
        /// </summary>
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

        #endregion Public API

        
        #region Random Selection

        /// <summary>
        /// Get a random item based on the weighted chances. Returns default(T) if no valid items exist.
        /// </summary>
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
        /// Get multiple random items (with possible duplicates based on chances)
        /// </summary>
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
        /// Get multiple unique random items (no duplicates, items are removed from selection pool)
        /// </summary>
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