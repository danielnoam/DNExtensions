using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// A weighted chance-based collection that allows random selection of items based on percentage probabilities.
    /// Items can have their chances locked to prevent normalization, and the collection automatically
    /// maintains a total of 100% across all unlocked items.
    /// </summary>
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

        [SerializeField] private List<InternalChanceItem> internalItems = new();

        #region Public API

        /// <summary>Gets the number of items in the chance list.</summary>
        public int Count => internalItems.Count;

        /// <summary>Gets or sets the item at the specified index.</summary>
        public T this[int index]
        {
            get
            {
                ValidateIndex(index);
                return internalItems[index].item;
            }
            set
            {
                ValidateIndex(index);
                var entry = internalItems[index];
                entry.item = value;
                internalItems[index] = entry;
            }
        }

        /// <summary>Adds a new item to the chance list and normalizes.</summary>
        public void AddItem(T item, int chance = 10, bool isLocked = false)
        {
            internalItems.Add(new InternalChanceItem(item, chance, isLocked));
            NormalizeChances();
        }

        /// <summary>Removes the item at the specified index and normalizes.</summary>
        public void RemoveAt(int index)
        {
            ValidateIndex(index);
            internalItems.RemoveAt(index);
            NormalizeChances();
        }

        /// <summary>Removes the first occurrence of the specified item.</summary>
        public void Remove(T item)
        {
            int index = IndexOf(item);
            if (index != -1) RemoveAt(index);
        }

        /// <summary>Sets the chance percentage for the item at the specified index and normalizes.</summary>
        public void SetChance(int index, int newChance)
        {
            ValidateIndex(index);
            var entry = internalItems[index];
            entry.chance = Mathf.Clamp(newChance, 0, 100);
            internalItems[index] = entry;
            NormalizeChances();
        }

        /// <summary>Gets the chance percentage for the item at the specified index.</summary>
        public int GetChance(int index)
        {
            ValidateIndex(index);
            return internalItems[index].chance;
        }

        /// <summary>Sets the locked state for the item at the specified index and normalizes.</summary>
        public void SetLocked(int index, bool locked)
        {
            ValidateIndex(index);
            var entry = internalItems[index];
            entry.isLocked = locked;
            internalItems[index] = entry;
            NormalizeChances();
        }

        /// <summary>Gets the locked state for the item at the specified index.</summary>
        public bool IsLocked(int index)
        {
            ValidateIndex(index);
            return internalItems[index].isLocked;
        }

        /// <summary>Removes all items from the chance list.</summary>
        public void Clear() => internalItems.Clear();

        /// <summary>Returns the index of the first occurrence of the specified item, or -1 if not found.</summary>
        public int IndexOf(T item)
        {
            for (int i = 0; i < internalItems.Count; i++)
                if (EqualityComparer<T>.Default.Equals(internalItems[i].item, item))
                    return i;
            return -1;
        }

        /// <summary>Returns all items as a List.</summary>
        public List<T> ToList()
        {
            var list = new List<T>(internalItems.Count);
            foreach (var entry in internalItems)
                list.Add(entry.item);
            return list;
        }

        /// <summary>
        /// Redistributes chance percentages among unlocked items to maintain a total of 100%.
        /// Locked items retain their values and are excluded from normalization.
        /// </summary>
        public void NormalizeChances()
        {
            if (internalItems.Count == 0) return;

            var unlockedIndices = new List<int>();
            int lockedTotal = 0;

            for (int i = 0; i < internalItems.Count; i++)
            {
                if (internalItems[i].isLocked)
                    lockedTotal += Mathf.Max(0, internalItems[i].chance);
                else
                    unlockedIndices.Add(i);
            }

            if (unlockedIndices.Count == 0) return;

            int remaining = Mathf.Max(0, 100 - lockedTotal);
            int unlockedTotal = 0;

            foreach (int i in unlockedIndices)
                unlockedTotal += Mathf.Max(0, internalItems[i].chance);

            if (unlockedTotal <= 0)
            {
                int equal = remaining / unlockedIndices.Count;
                int remainder = remaining % unlockedIndices.Count;

                for (int i = 0; i < unlockedIndices.Count; i++)
                {
                    var entry = internalItems[unlockedIndices[i]];
                    entry.chance = equal + (i < remainder ? 1 : 0);
                    internalItems[unlockedIndices[i]] = entry;
                }
            }
            else if (unlockedTotal != remaining)
            {
                int newTotal = 0;

                foreach (int i in unlockedIndices)
                {
                    var entry = internalItems[i];
                    entry.chance = Mathf.RoundToInt((entry.chance / (float)unlockedTotal) * remaining);
                    internalItems[i] = entry;
                    newTotal += entry.chance;
                }

                int difference = remaining - newTotal;
                if (difference != 0)
                {
                    unlockedIndices.Sort((a, b) => internalItems[b].chance.CompareTo(internalItems[a].chance));

                    for (int i = 0; i < Mathf.Abs(difference) && i < unlockedIndices.Count; i++)
                    {
                        int idx = unlockedIndices[i];
                        var entry = internalItems[idx];
                        if (difference > 0) entry.chance++;
                        else if (entry.chance > 0) entry.chance--;
                        internalItems[idx] = entry;
                    }
                }
            }

            for (int i = 0; i < internalItems.Count; i++)
            {
                if (internalItems[i].chance < 0)
                {
                    var entry = internalItems[i];
                    entry.chance = 0;
                    internalItems[i] = entry;
                }
            }
        }

        #endregion

        #region Random Selection

        /// <summary>
        /// Returns a random item based on weighted chance percentages.
        /// Items with 0% chance are excluded. Returns default(T) if no valid items exist.
        /// </summary>
        public T GetRandomItem()
        {
            if (internalItems.Count == 0) return default;

            float totalWeight = 0f;
            foreach (var entry in internalItems)
                if (entry.chance > 0) totalWeight += entry.chance;

            if (totalWeight <= 0f) return default;

            float roll = Random.Range(0f, totalWeight);
            float current = 0f;

            foreach (var entry in internalItems)
            {
                if (entry.chance <= 0) continue;
                current += entry.chance;
                if (roll <= current) return entry.item;
            }

            return default;
        }

        /// <summary>
        /// Returns multiple randomly selected items, allowing duplicates.
        /// Each selection is independent with the same probabilities.
        /// </summary>
        public T[] GetRandomItems(int count)
        {
            var results = new T[count];
            for (int i = 0; i < count; i++)
                results[i] = GetRandomItem();
            return results;
        }

        /// <summary>
        /// Returns multiple unique randomly selected items without duplicates.
        /// The returned count may be less than requested if there aren't enough valid items.
        /// </summary>
        public T[] GetUniqueRandomItems(int count)
        {
            if (count <= 0) return Array.Empty<T>();

            var available = new List<InternalChanceItem>();
            foreach (var entry in internalItems)
                if (entry.chance > 0) available.Add(entry);

            count = Mathf.Min(count, available.Count);
            var results = new T[count];

            for (int i = 0; i < count; i++)
            {
                if (available.Count == 0) break;

                float totalWeight = 0f;
                foreach (var entry in available)
                    totalWeight += entry.chance;

                if (totalWeight <= 0f)
                {
                    results[i] = available[0].item;
                    available.RemoveAt(0);
                    continue;
                }

                float roll = Random.Range(0f, totalWeight);
                float current = 0f;

                for (int j = 0; j < available.Count; j++)
                {
                    current += available[j].chance;
                    if (roll <= current)
                    {
                        results[i] = available[j].item;
                        available.RemoveAt(j);
                        break;
                    }
                }
            }

            return results;
        }

        #endregion

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= internalItems.Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for ChanceList with {internalItems.Count} items.");
        }
    }
}