using System;
using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Type-safe reference to an Animator trigger with editor validation.
    /// Provides a dropdown of available triggers from a specified AnimatorController.
    /// </summary>
    [Serializable]
    public struct AnimatorTriggerField
    {
        [SerializeField] private string controllerNameOrPath;
        [SerializeField] private string triggerName;

        /// <summary>
        /// Gets the name of the selected trigger.
        /// </summary>
        public string TriggerName => triggerName;
        
        /// <summary>
        /// Gets whether a trigger has been assigned.
        /// </summary>
        public bool IsAssigned => !string.IsNullOrEmpty(triggerName);

        /// <summary>
        /// Creates a new AnimatorTriggerField with the specified controller reference.
        /// </summary>
        /// <param name="controllerNameOrPath">Name or path to the AnimatorController asset</param>
        /// <param name="triggerName">Optional trigger name to set initially</param>
        public AnimatorTriggerField(string controllerNameOrPath, string triggerName = "")
        {
            this.controllerNameOrPath = controllerNameOrPath;
            this.triggerName = triggerName;
        }

        /// <summary>
        /// Implicitly converts the field to its trigger name string.
        /// </summary>
        public static implicit operator string(AnimatorTriggerField field) => field.triggerName;
    }
}