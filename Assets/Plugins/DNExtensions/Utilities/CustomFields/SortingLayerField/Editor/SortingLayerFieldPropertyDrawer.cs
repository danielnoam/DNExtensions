using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DNExtensions.Utilities.CustomFields
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
            
            string[] sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();
            int[] layerIDs = SortingLayer.layers.Select(l => l.id).ToArray();
            
            int currentIndex = System.Array.IndexOf(sortingLayerNames, layerName.stringValue);
            bool layerExists = currentIndex != -1;
            bool hasIssue = string.IsNullOrEmpty(layerName.stringValue) || !layerExists;
            
            float iconWidth = hasIssue ? 20f : 0f;
            float buttonWidth = 25f;
            
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

            EditorGUI.LabelField(labelRect, label);
            
            if (!layerExists && !string.IsNullOrEmpty(layerName.stringValue))
            {
                var tempNames = sortingLayerNames.ToList();
                tempNames.Insert(0, $"{layerName.stringValue} (Missing)");
                sortingLayerNames = tempNames.ToArray();
                currentIndex = 0;
            }
            else if (!layerExists)
            {
                currentIndex = 0;
            }
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, sortingLayerNames);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!layerExists && newIndex == 0)
                {
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

            if (hasIssue)
            {
                DrawStatusIcon(iconRect, layerName.stringValue, layerExists);
            }

            if (GUI.Button(buttonRect, new GUIContent("⚙", "Open Tags and Layers settings")))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
            }
            
            EditorGUI.EndProperty();
        }
        
        private static void DrawStatusIcon(Rect rect, string layerName, bool layerExists)
        {
            string icon;
            string tooltip;
            Color iconColor;

            if (string.IsNullOrEmpty(layerName))
            {
                icon = "⚠";
                tooltip = "No sorting layer selected";
                iconColor = new Color(1f, 0.6f, 0f);
            }
            else if (!layerExists)
            {
                icon = "✕";
                tooltip = $"Sorting layer '{layerName}' does not exist!\nCreate it in Tags and Layers settings.";
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
    }
}