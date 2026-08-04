using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Base class for conditional inspector attributes.
    /// Supports bool, int, float, string, enum, and object reference fields, properties, and parameterless methods returning bool.
    /// For object references, pass null as the value to check if the reference is unassigned.
    /// </summary>
    public abstract class IfAttribute : PropertyAttribute
    {
        public readonly string VariableName;
        public readonly object VariableValue;

        /// <param name="boolName">Name of a bool field, property, or parameterless method returning bool.</param>
        protected IfAttribute(string boolName)
        {
            VariableName = boolName;
            VariableValue = true;
        }

        /// <param name="variableName">Name of the field, property, or parameterless method to evaluate.</param>
        /// <param name="variableValue">Value to compare against. For object references, use null to check if unassigned.</param>
        protected IfAttribute(string variableName, object variableValue)
        {
            VariableName = variableName;
            VariableValue = variableValue;
        }
    }

    /// <summary>Shows the field only when the condition is true.</summary>
    public class ShowIfAttribute : IfAttribute
    {
        /// <param name="boolName">Name of a bool field, property, or parameterless method returning bool.</param>
        public ShowIfAttribute(string boolName) : base(boolName) { }

        /// <param name="variableName">Name of the field, property, or parameterless method to evaluate.</param>
        /// <param name="variableValue">Value to compare against.</param>
        public ShowIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    /// <summary>Hides the field when the condition is true.</summary>
    public class HideIfAttribute : IfAttribute
    {
        /// <param name="boolName">Name of a bool field, property, or parameterless method returning bool.</param>
        public HideIfAttribute(string boolName) : base(boolName) { }

        /// <param name="variableName">Name of the field, property, or parameterless method to evaluate.</param>
        /// <param name="variableValue">Value to compare against.</param>
        public HideIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    /// <summary>Enables the field only when the condition is true.</summary>
    public class EnableIfAttribute : IfAttribute
    {
        /// <param name="boolName">Name of a bool field, property, or parameterless method returning bool.</param>
        public EnableIfAttribute(string boolName) : base(boolName) { }

        /// <param name="variableName">Name of the field, property, or parameterless method to evaluate.</param>
        /// <param name="variableValue">Value to compare against.</param>
        public EnableIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }

    /// <summary>Disables the field when the condition is true.</summary>
    public class DisableIfAttribute : IfAttribute
    {
        /// <param name="boolName">Name of a bool field, property, or parameterless method returning bool.</param>
        public DisableIfAttribute(string boolName) : base(boolName) { }

        /// <param name="variableName">Name of the field, property, or parameterless method to evaluate.</param>
        /// <param name="variableValue">Value to compare against.</param>
        public DisableIfAttribute(string variableName, object variableValue) : base(variableName, variableValue) { }
    }
}