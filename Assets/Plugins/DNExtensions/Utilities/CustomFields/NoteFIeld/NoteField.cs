using UnityEngine;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// A serializable field that displays text with optional editing capability.
    /// Double-click to edit when editable. Text expands automatically as needed.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyScript : MonoBehaviour
    /// {
    ///     [SerializeField] private NoteField readOnlyNote = new NoteField("This is read-only", isEditable: false);
    ///     [SerializeField] private NoteField editableNote = new NoteField("Double-click to edit", isEditable: true);
    /// }
    /// </code>
    /// </example>
    [System.Serializable]
    public class NoteField
    {
        [SerializeField] private string note;
        [SerializeField] private bool isEditable;

        /// <summary>
        /// Creates a new NoteField with the specified configuration.
        /// </summary>
        /// <param name="initialText">The initial text content</param>
        /// <param name="isEditable">Whether the field can be edited via double-click (default: true)</param>
        public NoteField(string initialText = "", bool isEditable = true)
        {
            this.note = initialText;
            this.isEditable = isEditable;
        }

        /// <summary>
        /// Gets or sets the note text content.
        /// </summary>
        public string Note
        {
            get => note;
            set => note = value;
        }

        /// <summary>
        /// Gets whether this field is editable.
        /// </summary>
        public bool IsEditable => isEditable;

        /// <summary>
        /// Implicit conversion operator that allows NoteField to be used directly as a string.
        /// </summary>
        public static implicit operator string(NoteField noteField)
        {
            return noteField?.note ?? "";
        }
    }
}