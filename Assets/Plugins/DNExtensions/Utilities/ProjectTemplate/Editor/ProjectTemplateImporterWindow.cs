using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace DNExtensions.Utilities
{
    internal class ProjectTemplateImporterWindow : EditorWindow
    {
        enum Tab { Assets, Packages, ProjectSettings }
        enum State { PickFile, Ready }

        State _state = State.PickFile;
        Tab _activeTab;

        // Loaded template info
        string _tempDir;
        string _packageRoot;
        string _projectData;
        string _templateName;
        string _templateDescription;

        // Assets tab
        TreeViewState<int> _treeState;
        AssetTreeView _treeView;
        SearchField _searchField;

        // Packages tab
        PackagesView _packagesView;

        // Project Settings tab
        readonly Dictionary<string, bool> _settingsToggles = new();
        Vector2 _settingsScroll;

        [MenuItem("Tools/DNExtensions/Templates/Import Template into Project")]
        static void Open()
        {
            var window = GetWindow<ProjectTemplateImporterWindow>("Import Template");
            window.minSize = new Vector2(420, 520);
        }

        void OnDestroy() => Cleanup();

        void Cleanup()
        {
            if (!string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
                _tempDir = null;
            }
        }

        void OnGUI()
        {
            if (_state == State.PickFile)
                DrawPickFile();
            else
                DrawImporter();
        }

        void DrawPickFile()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
                {
                    EditorGUILayout.LabelField("Import Template into Project", EditorStyles.boldLabel);
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("Select a .tgz template file to begin.", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(12);
                    if (GUILayout.Button("Browse for Template...", GUILayout.Height(30)))
                        BrowseAndLoad();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        async void BrowseAndLoad()
        {
            string path = EditorUtility.OpenFilePanel("Select Template", "", "tgz");
            if (string.IsNullOrEmpty(path)) return;

            EditorUtility.DisplayProgressBar("Importing Template", "Extracting...", 0.3f);
            var (success, packageRoot, tempDir) = await TemplateImporter.Extract(path);
            EditorUtility.ClearProgressBar();

            if (!success)
            {
                EditorUtility.DisplayDialog("Error", "Failed to extract template. Ensure tar is available.", "OK");
                return;
            }

            _packageRoot = packageRoot;
            _tempDir = tempDir;
            _projectData = Path.Combine(packageRoot, "ProjectData~");

            (_templateName, _templateDescription) = TemplateImporter.ReadPackageJson(packageRoot);

            string projectData = Path.Combine(packageRoot, "ProjectData~");
            string assetsPath = Path.Combine(projectData, "Assets");
            if (Directory.Exists(assetsPath))
            {
                _treeState = new TreeViewState<int>();
                _treeView = new AssetTreeView(_treeState, assetsPath);
                _treeView.Reload();
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
            }

            _packagesView = new PackagesView();
            _packagesView.Load(Path.Combine(projectData, "Packages"));

            _settingsToggles.Clear();
            string settingsPath = Path.Combine(projectData, "ProjectSettings");
            foreach (var (file, _) in TemplateExporterData.ToggleableSettings)
                _settingsToggles[file] = File.Exists(Path.Combine(settingsPath, file));

            _state = State.Ready;
            Repaint();
        }

        void DrawImporter()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("◀ Back", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Cleanup();
                    _state = State.PickFile;
                    return;
                }
                GUILayout.Space(6);
                GUILayout.Label(_templateName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }

            if (!string.IsNullOrEmpty(_templateDescription))
            {
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);
                    EditorGUILayout.LabelField(_templateDescription, EditorStyles.wordWrappedMiniLabel);
                    GUILayout.Space(8);
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawTabButton(Tab.Assets, "Assets");
                DrawTabButton(Tab.Packages, "Packages");
                DrawTabButton(Tab.ProjectSettings, "Project Settings");
                GUILayout.FlexibleSpace();
            }

            switch (_activeTab)
            {
                case Tab.Assets:          DrawAssetsTab(); break;
                case Tab.Packages:        DrawPackagesTab(); break;
                case Tab.ProjectSettings: DrawProjectSettingsTab(); break;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (_activeTab == Tab.Assets && _treeView != null)
                {
                    int count = _treeView.GetCheckedFilePaths().Count;
                    GUILayout.Label($"{count} file{(count != 1 ? "s" : "")} selected", EditorStyles.miniLabel);
                    GUILayout.Space(8);
                }
                else if (_activeTab == Tab.Packages && _packagesView != null)
                {
                    GUILayout.Label($"{_packagesView.SelectedCount}/{_packagesView.TotalCount} packages selected", EditorStyles.miniLabel);
                    GUILayout.Space(8);
                }

                if (GUILayout.Button("Import", GUILayout.Width(80), GUILayout.Height(24)))
                    RunImport();

                GUILayout.Space(6);
            }
            EditorGUILayout.Space(6);
        }

        void DrawTabButton(Tab tab, string label)
        {
            var prev = GUI.backgroundColor;
            if (_activeTab == tab) GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Toggle(_activeTab == tab, label, EditorStyles.toolbarButton, GUILayout.Width(110)) && _activeTab != tab)
                _activeTab = tab;
            GUI.backgroundColor = prev;
        }

        void DrawAssetsTab()
        {
            if (_treeView == null)
            {
                EditorGUILayout.HelpBox("This template contains no Assets folder.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(36)))
                    _treeView.SetAllChecked(true);
                if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    _treeView.SetAllChecked(false);
                GUILayout.FlexibleSpace();
                _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString, GUILayout.MinWidth(160));
            }

            var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView.OnGUI(treeRect);
        }

        void DrawPackagesTab()
        {
            if (_packagesView == null)
            {
                EditorGUILayout.HelpBox("This template contains no packages.", MessageType.Info);
                return;
            }
            _packagesView.Draw();
        }

        void DrawProjectSettingsTab()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(36)))
                    SetAllSettings(true);
                if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    SetAllSettings(false);
                GUILayout.FlexibleSpace();
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_settingsScroll);
            _settingsScroll = scroll.scrollPosition;

            EditorGUILayout.Space(4);
            foreach (var (file, label) in TemplateExporterData.ToggleableSettings)
            {
                bool available = File.Exists(Path.Combine(_projectData, "ProjectSettings", file));
                using (new EditorGUI.DisabledScope(!available))
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);
                    _settingsToggles[file] = EditorGUILayout.ToggleLeft(
                        available ? label : $"{label}  (not in template)",
                        _settingsToggles[file]);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Registry packages are merged into manifest.json — existing versions are not overwritten.\n" +
                "Embedded packages are copied into your Packages folder.",
                MessageType.None);
        }

        void SetAllSettings(bool value)
        {
            foreach (var (file, _) in TemplateExporterData.ToggleableSettings)
            {
                bool available = File.Exists(Path.Combine(_projectData, "ProjectSettings", file));
                if (available) _settingsToggles[file] = value;
            }
        }

        HashSet<string> BuildSettingsImportSet()
        {
            var set = new HashSet<string>();
            foreach (var (file, _) in TemplateExporterData.ToggleableSettings)
                if (_settingsToggles.TryGetValue(file, out bool val) && val)
                    set.Add(file);
            return set;
        }

        async void RunImport()
        {
            var selectedFiles = _treeView != null
                ? _treeView.GetCheckedFilePaths()
                : new List<string>();

            bool success = await TemplateImporter.Import(
                _packageRoot,
                selectedFiles,
                BuildSettingsImportSet(),
                _packagesView?.GetSelectedRegistry() ?? new Dictionary<string, string>(),
                _packagesView?.GetSelectedEmbeddedPaths() ?? new List<string>());

            if (success)
            {
                EditorUtility.DisplayDialog("Done", $"Template \"{_templateName}\" imported successfully.", "OK");
                Cleanup();
                _state = State.PickFile;
                Repaint();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Import failed. Check the console for details.", "OK");
            }
        }
    }
}