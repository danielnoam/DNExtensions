using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomPropertyDrawer(typeof(AudioIDAttribute))]
    public class AudioIDDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var settings = SOAudioLibrarySettings.Instance;

            List<string> ids = new();
            if (settings)
            {
                foreach (var category in settings.AudioCategories)
                {
                    if (!category) continue;
                    foreach (var mapping in category.AudioMappings)
                    {
                        if (!string.IsNullOrEmpty(mapping.id))
                            ids.Add(mapping.id);
                    }
                }
            }

            int currentIndex = ids.IndexOf(property.stringValue);
            bool idExists = currentIndex != -1;
            bool hasIssue = !idExists;

            float iconWidth = hasIssue ? 20f : 0f;

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            if (!idExists && !string.IsNullOrEmpty(property.stringValue))
            {
                ids.Insert(0, $"{property.stringValue} (Missing)");
                currentIndex = 0;
            }
            else if (!idExists)
            {
                if (ids.Count > 0) currentIndex = 0;
            }

            if (hasIssue)
                DrawStatusIcon(iconRect, property.stringValue, idExists);

            if (ids.Count == 0)
            {
                EditorGUI.LabelField(popupRect, "No Audio IDs found in library", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                int selected = EditorGUI.Popup(popupRect, currentIndex, ids.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    if (idExists || selected != 0)
                    {
                        int adjustedIndex = (!idExists && !string.IsNullOrEmpty(property.stringValue)) ? selected - 1 : selected;
                        if (adjustedIndex >= 0 && adjustedIndex < ids.Count)
                            property.stringValue = ids[adjustedIndex];
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static void DrawStatusIcon(Rect rect, string audioID, bool idExists)
        {
            string icon;
            string tooltip;
            Color iconColor;

            if (string.IsNullOrEmpty(audioID))
            {
                icon = "⚠";
                tooltip = "No Audio ID selected";
                iconColor = new Color(1f, 0.6f, 0f);
            }
            else
            {
                icon = "✕";
                tooltip = $"Audio ID '{audioID}' not found in Audio Library.";
                iconColor = Color.red;
            }

            Color prev = GUI.color;
            GUI.color = iconColor;

            EditorGUI.LabelField(rect, new GUIContent(icon, tooltip), new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            });

            GUI.color = prev;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}