using System;

namespace DNExtensions.Utilities.SerializableSelector
{
    /// <summary>
    /// Marks a type as only being allowed once in a SerializeReference list.
    /// Duplicate instances will be grayed out in the dropdown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SerializableSelectorAllowOnceAttribute : Attribute
    {
    }
}