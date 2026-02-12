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
            
            bool hasIssue = sceneAsset.objectReferenceValue == null || HasBuildIssue(scenePath.stringValue);
            
            float iconWidth = hasIssue ? 20f : 0f;
            float buttonWidth = 25f;
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - iconWidth, position.height);
            Rect iconRect = new Rect(position.x + EditorGUIUtility.labelWidth - iconWidth, position.y, iconWidth, position.height);
            Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - buttonWidth, position.height);
            Rect buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);
            
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

                if (hasIssue)
                {
                    DrawStatusIcon(iconRect, sceneName.stringValue, scenePath.stringValue, sceneAsset.objectReferenceValue == null);
                }
            }
            
            if (GUI.Button(buttonRect, new GUIContent("⚙", "Open Build Settings")))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
            
            EditorGUI.EndProperty();
        }

        private bool HasBuildIssue(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                return false;
            
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    return !scenes[i].enabled;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Draws a status icon next to the label with tooltip on hover and click to open build settings.
        /// </summary>
        private static void DrawStatusIcon(Rect rect, string sceneName, string scenePath, bool isEmpty)
        {
            if (isEmpty)
            {
                return;
            }
            
            bool sceneInBuild = false;
            bool sceneEnabled = false;
            
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    sceneInBuild = true;
                    sceneEnabled = scenes[i].enabled;
                    break;
                }
            }

            string icon;
            string tooltip;
            Color iconColor;

            if (!sceneInBuild)
            {
                icon = "✕";
                tooltip = $"Scene '{sceneName}' is not in build settings!\nAdd it to build settings to use at runtime.";
                iconColor = Color.red;
            }
            else if (!sceneEnabled)
            {
                icon = "⚠";
                tooltip = $"Scene '{sceneName}' is disabled in build settings!\nEnable it in build settings to use at runtime.";
                iconColor = new Color(1f, 0.6f, 0f);
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