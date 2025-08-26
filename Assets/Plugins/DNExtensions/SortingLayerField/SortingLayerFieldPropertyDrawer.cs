
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;


namespace DNExtensions
{
    
    /// <summary>
    /// Custom property drawer that provides a dropdown interface for selecting sorting layers in the Unity Inspector.
    /// Shows layer validation status and includes a convenience button to access project settings.
    /// </summary>
    [CustomPropertyDrawer(typeof(SortingLayerField))]
    public class SortingLayerFieldPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Renders the custom GUI for the SortingLayerField in the Unity Inspector.
        /// Creates a dropdown populated with available sorting layers, validation status, and settings button.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for</param>
        /// <param name="label">The label of this property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            
            SerializedProperty layerName = property.FindPropertyRelative("layerName");
            SerializedProperty layerID = property.FindPropertyRelative("layerID");
            
            // Reserve space for status icon and settings button
            float iconWidth = 20f;
            float buttonWidth = 25f;
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

            // Draw label
            EditorGUI.LabelField(labelRect, label);

            // Get all sorting layers
            string[] sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();
            int[] layerIDs = SortingLayer.layers.Select(l => l.id).ToArray();
            
            // Find current selection
            int currentIndex = System.Array.IndexOf(sortingLayerNames, layerName.stringValue);
            bool layerExists = currentIndex != -1;
            
            // If layer doesn't exist, add it as a missing option
            if (!layerExists && !string.IsNullOrEmpty(layerName.stringValue))
            {
                var tempNames = sortingLayerNames.ToList();
                tempNames.Insert(0, $"{layerName.stringValue} (Missing)");
                sortingLayerNames = tempNames.ToArray();
                currentIndex = 0;
            }
            else if (!layerExists)
            {
                currentIndex = 0; // Default to first layer
            }
            
            // Draw popup
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, sortingLayerNames);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!layerExists && newIndex == 0)
                {
                    // User selected the missing option, don't change anything
                }
                else
                {
                    int adjustedIndex = (!layerExists && !string.IsNullOrEmpty(layerName.stringValue)) ? newIndex - 1 : newIndex;
                    if (adjustedIndex >= 0 && adjustedIndex < layerIDs.Length)
                    {
                        layerName.stringValue = SortingLayer.layers[adjustedIndex].name;
                        layerID.intValue = layerIDs[adjustedIndex];
                    }
                }
            }

            // Draw status icon
            DrawStatusIcon(iconRect, layerName.stringValue, layerExists);

            // Settings button
            if (GUI.Button(buttonRect, new GUIContent("⚙", "Open Tags and Layers settings")))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws a status icon indicating whether the sorting layer exists and is valid.
        /// </summary>
        /// <param name="rect">The rectangle to draw the icon in</param>
        /// <param name="layerName">The name of the sorting layer</param>
        /// <param name="layerExists">Whether the layer exists in the project</param>
        private void DrawStatusIcon(Rect rect, string layerName, bool layerExists)
        {
            string icon;
            string tooltip;
            Color iconColor;

            if (string.IsNullOrEmpty(layerName))
            {
                icon = "⚠";
                tooltip = "No sorting layer selected";
                iconColor = new Color(1f, 0.6f, 0f); // Orange
            }
            else if (!layerExists)
            {
                icon = "✕";
                tooltip = $"Sorting layer '{layerName}' does not exist!\nCreate it in Tags and Layers settings.";
                iconColor = Color.red;
            }
            else
            {
                icon = "✓";
                tooltip = $"Sorting layer '{layerName}' is valid";
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
    }

}

#endif