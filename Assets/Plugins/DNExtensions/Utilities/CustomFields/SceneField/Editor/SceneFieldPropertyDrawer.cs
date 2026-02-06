
using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities.CustomFields
{
    /// <summary>
    /// Custom property drawer for SceneField that displays build index and validation errors.
    /// Shows scene reference field with build status information and consistent icon styling.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneField))]
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            SerializedProperty sceneAsset = property.FindPropertyRelative("sceneAsset");
            SerializedProperty sceneName = property.FindPropertyRelative("sceneName");
            SerializedProperty scenePath = property.FindPropertyRelative("scenePath");
            
            // Reserve space for status icon and settings button
            float iconWidth = 20f;
            float buttonWidth = 25f;
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);
            
            // Draw label
            EditorGUI.LabelField(labelRect, label);
            
            if (sceneAsset != null)
            {
                EditorGUI.BeginChangeCheck();
                sceneAsset.objectReferenceValue = EditorGUI.ObjectField(fieldRect, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
                
                if (EditorGUI.EndChangeCheck())
                {
                    if (sceneAsset.objectReferenceValue)
                    {
                        SceneAsset scene = sceneAsset.objectReferenceValue as SceneAsset;
                        if (scene)
                        {
                            sceneName.stringValue = scene.name;
                            scenePath.stringValue = AssetDatabase.GetAssetPath(scene);
                        }
                    }
                    else
                    {
                        sceneName.stringValue = "";
                        scenePath.stringValue = "";
                    }
                }

                // Draw status icon next to label
                if (sceneAsset.objectReferenceValue && !string.IsNullOrEmpty(scenePath.stringValue))
                {
                    DrawStatusIcon(iconRect, sceneName.stringValue, scenePath.stringValue);
                }
                else if (sceneAsset.objectReferenceValue == null)
                {
                    DrawEmptyStatusIcon(iconRect);
                }
            }
            
            // Draw settings button
            if (GUI.Button(buttonRect, new GUIContent("⚙", "Open Build Settings")))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
            
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws a status icon next to the label with tooltip on hover and click to open build settings.
        /// Uses the same visual style as SortingLayerField and TagField for consistency.
        /// </summary>
        private void DrawStatusIcon(Rect rect, string sceneName, string scenePath)
        {
            // Check build settings status
            bool sceneInBuild = false;
            bool sceneEnabled = false;
            int buildIndex = -1;
            
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    sceneInBuild = true;
                    sceneEnabled = scenes[i].enabled;
                    if (sceneEnabled)
                    {
                        buildIndex = i;
                    }
                    break;
                }
            }

            // Determine icon and tooltip
            string icon;
            string tooltip;
            Color iconColor;

            if (!sceneInBuild)
            {
                icon = "✕";
                tooltip = $"Scene '{sceneName}' is not in build settings!\nAdd it to build settings to use at runtime.\n\nClick to open Build Settings.";
                iconColor = Color.red;
            }
            else if (!sceneEnabled)
            {
                icon = "⚠";
                tooltip = $"Scene '{sceneName}' is disabled in build settings!\nEnable it in build settings to use at runtime.\n\nClick to open Build Settings.";
                iconColor = new Color(1f, 0.6f, 0f); // Orange
            }
            else
            {
                icon = "✓";
                tooltip = $"Scene '{sceneName}' is ready!\nBuild Index: {buildIndex}\n\nClick to open Build Settings.";
                iconColor = new Color(0f, 0.6f, 0f); // Green
            }

            // Handle click event
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0) // Left mouse button
                {
                    // Open Build Settings window using the original working method
                    EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
                    Event.current.Use();
                }
            }

            // Change cursor to pointer when hovering
            if (rect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
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
        /// Draws a warning icon when no scene is assigned.
        /// </summary>
        private void DrawEmptyStatusIcon(Rect rect)
        {
            string icon = "⚠";
            string tooltip = "No scene assigned";
            Color iconColor = new Color(1f, 0.6f, 0f); // Orange

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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}