using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Attribute that marks a ScriptableObject as unique - only one instance should exist in the project.
    /// The asset processor will warn and offer to delete duplicates.
    /// </summary>
    public class UniqueSOAttribute : System.Attribute
    {
    }
}