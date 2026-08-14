using System;
using System.Collections.Generic;
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
        private const string SceneViewFile = "sceneview.json";

        private static HierarchySettings _hierarchy;
        private static InspectorSettings _inspector;
        private static ProjectSettings _project;
        private static GameViewSettings _gameView;
        private static SceneViewSettings _sceneView;

        public static HierarchySettings Hierarchy => _hierarchy ??= Load<HierarchySettings>(HierarchyFile);
        public static InspectorSettings Inspector => _inspector ??= Load<InspectorSettings>(InspectorFile);
        public static ProjectSettings Project => _project ??= Load<ProjectSettings>(ProjectFile);
        public static GameViewSettings GameView => _gameView ??= Load<GameViewSettings>(GameViewFile);
        public static SceneViewSettings SceneView => _sceneView ??= Load<SceneViewSettings>(SceneViewFile);

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

        /// <summary>
        /// Repaints the Scene Views rather than the row-based windows the other modules notify, since
        /// those are the only surfaces these settings reach. Qualified because the property above
        /// shadows the type name inside this class.
        /// </summary>
        public static void SaveSceneView()
        {
            Save(SceneView, SceneViewFile);
            UnityEditor.SceneView.RepaintAll();
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
            _project = new ProjectSettings();
            SaveProject();
        }

        public static void ResetGameView()
        {
            _gameView = new GameViewSettings();
            SaveGameView();
        }

        public static void ResetSceneView()
        {
            _sceneView = new SceneViewSettings();
            SaveSceneView();
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
                    if (!string.IsNullOrWhiteSpace(json)) JsonUtility.FromJsonOverwrite(MigrateKeys(json, fileName), result);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Failed to read {fileName}, falling back to defaults: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Settings fields that were renamed after the suite had already written files, old name to
        /// new. JsonUtility matches on field name and silently ignores one it does not recognise, so
        /// without this a rename would quietly reset whatever the old key held. Keyed by file, since
        /// the same name can mean different things in two modules.
        ///
        /// Rewriting the text rather than reading both names keeps the settings classes clean of dead
        /// fields, and the entry stops mattering as soon as the file is saved again under the new key.
        /// </summary>
        private static readonly Dictionary<string, (string from, string to)[]> RenamedKeys =
            new Dictionary<string, (string, string)[]>
            {
                [HierarchyFile] = new[]
                {
                    ("treeDepthLinesEnabled", "treeLinesEnabled"),
                    ("treeDepthLineColor", "treeLineColor"),
                    ("treeDepthLineStyle", "treeLineStyle"),
                    ("componentIconSize", "componentStripIconSize")
                },
                [ProjectFile] = new[]
                {
                    ("showFileExtensions", "showFileExtensionsEnabled"),

                    // Two toggles became one. The folder half carries over and the object half is
                    // dropped, since a file that had them set differently has no single answer.
                    ("folderDropCreatesTabEnabled", "dropOnTabsEnabled")
                }
            };

        /// <summary>
        /// The trailing colon is what makes this safe to run on an already-migrated file: once the key
        /// has been rewritten the old spelling no longer appears in key position, and a value that
        /// happens to contain the same words is never followed by one.
        /// </summary>
        private static string MigrateKeys(string json, string fileName)
        {
            if (!RenamedKeys.TryGetValue(fileName, out (string from, string to)[] renames)) return json;

            foreach ((string from, string to) in renames)
            {
                json = json.Replace($"\"{from}\":", $"\"{to}\":");
            }

            return json;
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
