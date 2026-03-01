using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [CustomPropertyDrawer(typeof(AudioLibraryIDAttribute))]
    public class AudioLibraryIDDrawer : PropertyDrawer
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

            List<string> ids = new();
            FieldState state;

            if (!settings)
            {
                state = FieldState.NoLibrary;
                if (!string.IsNullOrEmpty(property.stringValue))
                    property.stringValue = string.Empty;
            }
            else
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

                if (ids.Count == 0)
                {
                    state = FieldState.NoIDs;
                    if (!string.IsNullOrEmpty(property.stringValue))
                        property.stringValue = string.Empty;
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

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, label);

            if (hasIssue) DrawStatusIcon(iconRect, state, property.stringValue);

            ids.Insert(0, "None");

            int currentIndex = string.IsNullOrEmpty(property.stringValue) ? 0 : ids.IndexOf(property.stringValue);

            if (state == FieldState.MissingID)
            {
                ids.Insert(1, $"{property.stringValue} (Missing)");
                currentIndex = 1;
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(popupRect, Mathf.Max(currentIndex, 0), ids.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (selected == 0)
                {
                    property.stringValue = string.Empty;
                }
                else
                {
                    int adjustedIndex = state == FieldState.MissingID ? selected - 1 : selected;
                    if (adjustedIndex >= 0 && adjustedIndex < ids.Count)
                        property.stringValue = ids[adjustedIndex];
                }
            }

            EditorGUI.EndProperty();
        }

        private static void DrawStatusIcon(Rect rect, FieldState state, string audioID)
        {
            string icon;
            string tooltip;
            Color iconColor;

            switch (state)
            {
                case FieldState.NoLibrary:
                    icon = "✕";
                    tooltip = "Audio Library asset not found.";
                    iconColor = Color.red;
                    break;
                case FieldState.NoIDs:
                    icon = "⚠";
                    tooltip = "No Audio IDs defined in the library.";
                    iconColor = new Color(1f, 0.6f, 0f);
                    break;
                case FieldState.MissingID:
                    icon = "✕";
                    tooltip = $"Audio ID '{audioID}' not found in Audio Library.";
                    iconColor = Color.red;
                    break;
                default:
                    return;
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