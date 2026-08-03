using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Loads and saves every module's settings as JSON under ProjectSettings/HelpfulEditor/.
    /// Mirrors the storage pattern used by Unity's own com.unity.settings-manager package.
    /// </summary>
    internal static class HelpfulEditorSettings
    {
        private const string SettingsFolder = "ProjectSettings/HelpfulEditor";
        private const string HierarchyFile = "hierarchy.json";
        private const string InspectorFile = "inspector.json";
        private const string ProjectFile = "project.json";
        private const string GameViewFile = "gameview.json";

        private static HierarchySettings _hierarchy;
        private static InspectorSettings _inspector;
        private static ProjectModuleSettings _project;
        private static GameViewSettings _gameView;

        public static HierarchySettings Hierarchy => _hierarchy ??= Load<HierarchySettings>(HierarchyFile);
        public static InspectorSettings Inspector => _inspector ??= Load<InspectorSettings>(InspectorFile);
        public static ProjectModuleSettings Project => _project ??= Load<ProjectModuleSettings>(ProjectFile);
        public static GameViewSettings GameView => _gameView ??= Load<GameViewSettings>(GameViewFile);

        public static void SaveHierarchy()
        {
            Save(Hierarchy, HierarchyFile);
            NotifyChanged();
        }

        public static void SaveInspector()
        {
            Save(Inspector, InspectorFile);
            NotifyChanged();
        }

        public static void SaveProject()
        {
            Save(Project, ProjectFile);
            NotifyChanged();
        }

        /// <summary>
        /// Guides are written here on every drag release, so this deliberately skips the window
        /// repaints the other modules trigger — the Game View overlay repaints itself.
        /// </summary>
        public static void SaveGameView()
        {
            Save(GameView, GameViewFile);
        }

        public static void ResetHierarchy()
        {
            _hierarchy = new HierarchySettings();
            SaveHierarchy();
        }

        public static void ResetInspector()
        {
            _inspector = new InspectorSettings();
            SaveInspector();
        }

        public static void ResetProject()
        {
            _project = new ProjectModuleSettings();
            SaveProject();
        }

        public static void ResetGameView()
        {
            _gameView = new GameViewSettings();
            SaveGameView();
        }

        private static void NotifyChanged()
        {
            EditorApplication.RepaintHierarchyWindow();
            EditorApplication.RepaintProjectWindow();
        }

        private static T Load<T>(string fileName) where T : class, new()
        {
            T result = new T();

            try
            {
                string path = Path.Combine(SettingsFolder, fileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json)) JsonUtility.FromJsonOverwrite(json, result);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Failed to read {fileName}, falling back to defaults: {e.Message}");
            }

            return result;
        }

        private static void Save<T>(T settings, string fileName) where T : class
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                File.WriteAllText(Path.Combine(SettingsFolder, fileName), JsonUtility.ToJson(settings, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Failed to write {fileName}: {e.Message}");
            }
        }
    }
}
