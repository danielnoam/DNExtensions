using UnityEngine;

namespace DNExtensions
{
    /// <summary>
    /// A serializable field that provides Unity Inspector support for selecting tags.
    /// Use this as a field type to get a dropdown selector with tag validation in the inspector.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyScript : MonoBehaviour
    /// {
    ///     [SerializeField] private TagField targetTag;
    ///     
    ///     void Start()
    ///     {
    ///         GameObject obj = GameObject.FindWithTag(targetTag);
    ///     }
    /// }
    /// </code>
    /// </example>
    [System.Serializable]
    public class TagField
    {
        /// <summary>
        /// The name of the selected tag.
        /// </summary>
        [SerializeField]
        private string tagName = "Untagged";

        /// <summary>
        /// Gets the name of the currently selected tag.
        /// </summary>
        public string TagName => tagName;

        /// <summary>
        /// Validates that the stored tag still exists in the project.
        /// In runtime, this simply checks if the tag name is not empty.
        /// In editor, this validates against the actual tag list.
        /// </summary>
        /// <returns>True if the tag exists and is valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(tagName)) return false;
            
#if UNITY_EDITOR
            // In editor, check against actual tag list
            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (string tag in allTags)
            {
                if (tag == tagName)
                {
                    return true;
                }
            }
            return false;
#else
            // In runtime, we can't validate against the tag list, so just check if it's not empty
            // Unity will handle invalid tags at runtime
            return true;
#endif
        }

        /// <summary>
        /// Sets the tag by name.
        /// In runtime, this simply sets the tag name.
        /// In editor, this validates against the actual tag list.
        /// </summary>
        /// <param name="name">The name of the tag to set</param>
        /// <returns>True if the tag was found and set, false otherwise</returns>
        public bool SetTag(string name)
        {
#if UNITY_EDITOR
            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (string tag in allTags)
            {
                if (tag == name)
                {
                    tagName = name;
                    return true;
                }
            }
            return false;
#else
            // In runtime, just set the tag name
            tagName = name;
            return true;
#endif
        }

        /// <summary>
        /// Implicit conversion operator that allows TagField to be used directly as a string.
        /// Enables seamless integration with Unity's tag-based APIs.
        /// </summary>
        /// <param name="tagField">The TagField to convert</param>
        /// <returns>The tag name as a string</returns>
        public static implicit operator string(TagField tagField)
        {
            return tagField?.tagName ?? "";
        }

        /// <summary>
        /// Explicit conversion from string to TagField.
        /// </summary>
        /// <param name="tagName">The tag name to convert</param>
        /// <returns>A new TagField with the specified tag</returns>
        public static explicit operator TagField(string tagName)
        {
            TagField field = new TagField();
            field.SetTag(tagName);
            return field;
        }

        /// <summary>
        /// Checks if the tag matches the provided string.
        /// </summary>
        /// <param name="tag">The tag string to compare against</param>
        /// <returns>True if the tags match, false otherwise</returns>
        public bool Matches(string tag)
        {
            return string.Equals(tagName, tag, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the tag name as a string.
        /// </summary>
        /// <returns>The tag name</returns>
        public override string ToString()
        {
            return tagName ?? "";
        }

        /// <summary>
        /// Checks equality with another TagField.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is TagField other)
            {
                return string.Equals(tagName, other.tagName, System.StringComparison.Ordinal);
            }
            if (obj is string str)
            {
                return string.Equals(tagName, str, System.StringComparison.Ordinal);
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code for the tag name.
        /// </summary>
        /// <returns>Hash code of the tag name</returns>
        public override int GetHashCode()
        {
            return tagName?.GetHashCode() ?? 0;
        }
    }
}