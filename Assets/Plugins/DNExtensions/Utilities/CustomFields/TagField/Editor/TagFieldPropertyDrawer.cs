using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DNExtensions.Utilities.CustomFields
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
            
            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
            
            int currentIndex = System.Array.IndexOf(allTags, tagName.stringValue);
            bool tagExists = currentIndex != -1;
            bool hasIssue = string.IsNullOrEmpty(tagName.stringValue) || !tagExists;
            
            float iconWidth = hasIssue ? 20f : 0f;
            float buttonWidth = 25f;
            
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

            EditorGUI.LabelField(labelRect, label);
            
            if (!tagExists && !string.IsNullOrEmpty(tagName.stringValue))
            {
                var tempTags = allTags.ToList();
                tempTags.Insert(0, $"{tagName.stringValue} (Missing)");
                allTags = tempTags.ToArray();
                currentIndex = 0;
            }
            else if (!tagExists)
            {
                currentIndex = 0;
            }
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, allTags);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!tagExists && newIndex == 0)
                {
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
            
            if (hasIssue)
            {
                DrawStatusIcon(iconRect, tagName.stringValue, tagExists);
            }
            
            if (GUI.Button(buttonRect, new GUIContent("⚙", "Open Tags and Layers settings")))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            
            EditorGUI.EndProperty();
        }


        private static void DrawStatusIcon(Rect rect, string tagName, bool tagExists)
        {
            string icon;
            string tooltip;
            Color iconColor;

            if (string.IsNullOrEmpty(tagName))
            {
                icon = "⚠";
                tooltip = "No tag selected";
                iconColor = new Color(1f, 0.6f, 0f);
            }
            else if (!tagExists)
            {
                icon = "✕";
                tooltip = $"Tag '{tagName}' does not exist!\nCreate it in Tags and Layers settings.";
                iconColor = Color.red;
            }
            else
            {
                return;
            }

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
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}