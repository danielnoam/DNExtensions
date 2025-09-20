#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DNExtensions
{
    /// <summary>
    /// Custom property drawer that provides a dropdown interface for selecting tags in the Unity Inspector.
    /// Shows tag validation status and includes a convenience button to access tag manager.
    /// </summary>
    [CustomPropertyDrawer(typeof(TagField))]
    public class TagFieldPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Renders the custom GUI for the TagField in the Unity Inspector.
        /// Creates a dropdown populated with available tags, validation status, and settings button.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for</param>
        /// <param name="label">The label of this property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            
            SerializedProperty tagName = property.FindPropertyRelative("tagName");
            
            // Reserve space for status icon and settings button
            float iconWidth = 20f;
            float buttonWidth = 25f;
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            // Get all tags
            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
            
            // Find current selection
            int currentIndex = System.Array.IndexOf(allTags, tagName.stringValue);
            bool tagExists = currentIndex != -1;
            
            // If tag doesn't exist, add it as a missing option
            if (!tagExists && !string.IsNullOrEmpty(tagName.stringValue))
            {
                var tempTags = allTags.ToList();
                tempTags.Insert(0, $"{tagName.stringValue} (Missing)");
                allTags = tempTags.ToArray();
                currentIndex = 0;
            }
            else if (!tagExists)
            {
                currentIndex = 0; // Default to first tag (usually "Untagged")
            }
            
            // Draw popup
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, allTags);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!tagExists && newIndex == 0)
                {
                    // User selected the missing option, don't change anything
                }
                else
                {
                    int adjustedIndex = (!tagExists && !string.IsNullOrEmpty(tagName.stringValue)) ? 
                                       newIndex - 1 : newIndex;
                    
                    if (adjustedIndex >= 0 && adjustedIndex < UnityEditorInternal.InternalEditorUtility.tags.Length)
                    {
                        tagName.stringValue = UnityEditorInternal.InternalEditorUtility.tags[adjustedIndex];
                    }
                }
            }
            
            // Draw status icon
            DrawStatusIcon(iconRect, tagName.stringValue, tagExists);
            
            // Draw settings button
            if (GUI.Button(buttonRect, new GUIContent("⚙", "Open Tags and Layers settings")))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws a status icon indicating whether the tag exists and is valid.
        /// Uses the same visual style as SortingLayerField for consistency.
        /// </summary>
        /// <param name="rect">The rectangle to draw the icon in</param>
        /// <param name="tagName">The name of the tag</param>
        /// <param name="tagExists">Whether the tag exists in the project</param>
        private void DrawStatusIcon(Rect rect, string tagName, bool tagExists)
        {
            string icon;
            string tooltip;
            Color iconColor;

            if (string.IsNullOrEmpty(tagName))
            {
                icon = "⚠";
                tooltip = "No tag selected";
                iconColor = new Color(1f, 0.6f, 0f); // Orange
            }
            else if (!tagExists)
            {
                icon = "✕";
                tooltip = $"Tag '{tagName}' does not exist!\nCreate it in Tags and Layers settings.";
                iconColor = Color.red;
            }
            else
            {
                icon = "✓";
                tooltip = $"Tag '{tagName}' is valid";
                iconColor = new Color(0f, 0.6f, 0f); // Green
            }

            // Draw icon with color and tooltip
            Color originalColor = GUI.color;
            GUI.color = iconColor;
            
            GUIContent iconContent = new GUIContent(icon, tooltip);
            GUIStyle iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            
            EditorGUI.LabelField(rect, iconContent, iconStyle);
            GUI.color = originalColor;
        }

        /// <summary>
        /// Returns the height of the property in the inspector.
        /// </summary>
        /// <param name="property">The property being drawn</param>
        /// <param name="label">The label of the property</param>
        /// <returns>The height in pixels</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif