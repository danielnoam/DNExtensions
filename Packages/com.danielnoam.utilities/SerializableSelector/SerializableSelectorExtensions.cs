using System;
using UnityEngine;

namespace DNExtensions.Utilities.SerializableSelector
{
    /// <summary>
    /// Runtime utilities for working with [SerializeReference] fields
    /// </summary>
    public static class SerializableSelectorExtensions
    {
        /// <summary>
        /// Get the actual runtime type of serialized reference
        /// </summary>
        /// <returns>The concrete type, or null if field is null</returns>
        public static Type GetReferenceType(this object obj)
        {
            return obj?.GetType();
        }
        
        /// <summary>
        /// Check if a serialized reference is of a specific type
        /// </summary>
        public static bool IsReferenceType<T>(this object obj) where T : class
        {
            return obj is T;
        }
        
        /// <summary>
        /// Deep copy a serialized reference using JSON serialization
        /// </summary>
        /// <returns>A new instance with copied values, or null if source is null</returns>
        public static T CopyReference<T>(this T source) where T : class
        {
            if (source == null) return null;
            
            try
            {
                string json = JsonUtility.ToJson(source);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy reference of type {typeof(T).Name}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Deep copy a serialized reference (non-generic version)
        /// </summary>
        public static object CopyReference(this object source)
        {
            if (source == null) return null;
            
            try
            {
                Type sourceType = source.GetType();
                string json = JsonUtility.ToJson(source);
                return JsonUtility.FromJson(json, sourceType);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to copy reference: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Paste values from source to target (both must be same type)
        /// </summary>
        public static void PasteReference<T>(this T target, T source) where T : class
        {
            if (target == null || source == null) return;
            
            try
            {
                string json = JsonUtility.ToJson(source);
                JsonUtility.FromJsonOverwrite(json, target);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to paste reference: {e.Message}");
            }
        }
    }
}