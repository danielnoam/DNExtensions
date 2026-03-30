using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    internal static class FullscreenWindowSettingsProvider
    {
        private const string ProjectSettingsPath = "Project/DNExtensions/Fullscreen Window";

        [MenuItem("Tools/DNExtensions/Fullscreen Window Settings")]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(ProjectSettingsPath);
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(ProjectSettingsPath, SettingsScope.Project)
            {
                label = "Fullscreen Window",
                guiHandler = DrawSettings,
                keywords = new[] { "fullscreen", "window", "shortcut", "maximize" }
            };
        }

        private static void DrawSettings(string searchContext)
        {
            var settings = new SerializedObject(FullscreenWindowSettings.Instance);

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Press the configured shortcut to toggle the focused editor window between normal and fullscreen.",
                MessageType.Info);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(settings.FindProperty("enabled"));

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Shortcut", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(settings.FindProperty("key"), new GUIContent("Key"));
            EditorGUILayout.PropertyField(settings.FindProperty("modifierKey"), new GUIContent("Modifier"));

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Current Shortcut",
                FullscreenWindowSettings.Instance.GetShortcutLabel(), EditorStyles.helpBox);

            if (EditorGUI.EndChangeCheck())
            {
                settings.ApplyModifiedProperties();
                FullscreenWindowSettings.Instance.Save();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(150)))
            {
                FullscreenWindowSettings.Instance.ResetToDefaults();
            }
        }
    }
}