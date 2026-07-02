using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.Utilities.ProjectWindowShortcuts
{
    public class ProjectWindowShortcutsSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/DNExtensions_ProjectWindowShortcutsSettings.asset";

        [Tooltip("Ctrl+Shift+Click an asset in the Project window opens its Properties preview window")]
        [SerializeField] private bool previewShortcutEnabled = true;
        [Tooltip("Ctrl+R while the Project window is focused reveals the selected asset(s) in Explorer/Finder instead of refreshing")]
        [SerializeField] private bool revealShortcutEnabled = true;

        public bool PreviewShortcutEnabled => previewShortcutEnabled;
        public bool RevealShortcutEnabled => revealShortcutEnabled;

        private static ProjectWindowShortcutsSettings _instance;

        internal static ProjectWindowShortcutsSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreate();
                }
                return _instance;
            }
        }

        private static ProjectWindowShortcutsSettings LoadOrCreate()
        {
            var settings = CreateInstance<ProjectWindowShortcutsSettings>();

            if (File.Exists(SettingsPath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            }

            return settings;
        }

        internal void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, SettingsPath, true);
        }

        public void ResetToDefaults()
        {
            previewShortcutEnabled = true;
            revealShortcutEnabled = true;
            Save();
        }
    }

    static class ProjectWindowShortcutsSettingsProvider
    {
        private const string MenuPath = "Tools/DNExtensions/Project Window Shortcuts Settings";
        private const int MenuPriority = 1000;

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DNExtensions/Project Window Shortcuts");
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/DNExtensions/Project Window Shortcuts", SettingsScope.Project)
            {
                label = "Project Window Shortcuts",
                guiHandler = (searchContext) =>
                {
                    var settings = new SerializedObject(ProjectWindowShortcutsSettings.Instance);

                    EditorGUILayout.Space(10);

                    EditorGUILayout.HelpBox(
                        "Usage:\n" +
                        "• Ctrl+Shift+Click an asset in the Project window to open its Properties preview\n" +
                        "• Ctrl+R while the Project window is focused reveals the selection in Explorer/Finder",
                        MessageType.Info
                    );

                    EditorGUILayout.Space(10);

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(settings.FindProperty("previewShortcutEnabled"),
                        new GUIContent("Preview Shortcut", "Ctrl+Shift+Click opens the Properties preview window for the asset"));

                    EditorGUILayout.PropertyField(settings.FindProperty("revealShortcutEnabled"),
                        new GUIContent("Reveal Shortcut", "Ctrl+R reveals the selected asset(s) in Explorer/Finder instead of refreshing"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ApplyModifiedProperties();
                        ProjectWindowShortcutsSettings.Instance.Save();
                    }

                    EditorGUILayout.Space(5);

                    if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Reset Settings",
                            "Reset all Project Window Shortcuts settings to their default values?",
                            "Reset",
                            "Cancel"))
                        {
                            ProjectWindowShortcutsSettings.Instance.ResetToDefaults();
                        }
                    }
                },

                keywords = new[] { "Project", "Window", "Shortcuts", "Preview", "Reveal", "Explorer", "Finder", "DNExtensions" }
            };

            return provider;
        }
    }
}
