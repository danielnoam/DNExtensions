using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Custom property drawer for <see cref="AudioLibraryIDAttribute"/> that displays a grouped dropdown
    /// of available audio IDs organized by category, with status icons for missing or invalid IDs.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioLibraryIDAttribute))]
    internal class AudioLibraryIDDrawer : PropertyDrawer
    {
        private enum FieldState { Valid, MissingID, NoIDs, NoLibrary }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, GUIContent.none, property);

            var settings = SOAudioLibrarySettings.Instance;
            var ids = new List<string>();
            var displayPaths = new List<string>();
            FieldState state;

            if (!settings)
            {
                state = FieldState.NoLibrary;
                if (!string.IsNullOrEmpty(property.stringValue))
                {
                    property.stringValue = string.Empty;
                }
            }
            else
            {
                foreach (var category in settings.AudioCategories)
                {
                    if (!category) continue;

                    string categoryLabel = string.IsNullOrEmpty(category.label) ? category.name : category.label;

                    foreach (var mapping in category.AudioMappings)
                    {
                        if (string.IsNullOrEmpty(mapping.id)) continue;

                        ids.Add(mapping.id);
                        displayPaths.Add($"{categoryLabel}/{mapping.id}");
                    }
                }

                if (ids.Count == 0)
                {
                    state = FieldState.NoIDs;
                    if (!string.IsNullOrEmpty(property.stringValue))
                    {
                        property.stringValue = string.Empty;
                    }
                }
                else if (string.IsNullOrEmpty(property.stringValue))
                {
                    state = FieldState.Valid;
                }
                else if (!ids.Contains(property.stringValue))
                {
                    state = FieldState.MissingID;
                }
                else
                {
                    state = FieldState.Valid;
                }
            }

            bool hasIssue = state != FieldState.Valid;
            float iconWidth = hasIssue ? 20f : 0f;

            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            var iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            var popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            if (hasIssue)
            {
                DrawStatusIcon(iconRect, state, property.stringValue);
            }

            ids.Insert(0, string.Empty);
            displayPaths.Insert(0, "None");

            int currentIndex = 0;

            if (state == FieldState.MissingID)
            {
                ids.Insert(1, property.stringValue);
                displayPaths.Insert(1, $"(Missing) {property.stringValue}");
                currentIndex = 1;
            }
            else if (!string.IsNullOrEmpty(property.stringValue))
            {
                currentIndex = ids.IndexOf(property.stringValue);
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(popupRect, Mathf.Max(currentIndex, 0), displayPaths.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = ids[selected];
            }

            EditorGUI.EndProperty();
        }

        private static void DrawStatusIcon(Rect rect, FieldState state, string audioID)
        {
            (string icon, string tooltip, Color color) = state switch
            {
                FieldState.NoLibrary => ("✕", "Audio Library asset not found.", Color.red),
                FieldState.NoIDs => ("⚠", "No Audio IDs defined in the library.", new Color(1f, 0.6f, 0f)),
                FieldState.MissingID => ("✕", $"Audio ID '{audioID}' not found in Audio Library.", Color.red),
                _ => default
            };

            if (icon == null) return;

            Color prev = GUI.color;
            GUI.color = color;
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