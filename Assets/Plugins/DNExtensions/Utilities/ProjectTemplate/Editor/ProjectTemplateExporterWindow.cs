using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace DNExtensions.Utilities
{
    internal class ProjectTemplateExporterWindow : EditorWindow
    {
        enum Tab { Info, Assets, Packages, ProjectSettings }

        Tab _activeTab;

        // Assets tab
        TreeViewState<int> _treeState;
        AssetTreeView _treeView;
        SearchField _searchField;

        // Packages tab
        PackagesView _packagesView;

        // Project Settings tab
        readonly Dictionary<string, bool> _settingsToggles = new();
        Vector2 _settingsScroll;

        // Info tab
        string _templateName;
        string _templateId;
        string _templateDescription;

        [MenuItem("Tools/DNExtensions/Templates/Export Project as Template")]
        static void Open()
        {
            var window = GetWindow<ProjectTemplateExporterWindow>("Export as Template");
            window.minSize = new Vector2(420, 520);
            window.Init();
        }

        void Init()
        {
            _treeState = new TreeViewState<int>();
            _treeView = new AssetTreeView(_treeState, Application.dataPath);
            _treeView.Reload();
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;

            _packagesView = new PackagesView();
            _packagesView.Load(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages"));

            _settingsToggles.Clear();
            foreach (var (file, _) in TemplateExporterData.ToggleableSettings)
                _settingsToggles[file] = true;

            _templateName = Path.GetFileName(Directory.GetParent(Application.dataPath).FullName);
            _templateId = "com.yourcompany.template." + _templateName.ToLower().Replace(" ", "-");
            _templateDescription = "";
        }

        void OnGUI()
        {
            if (_treeView == null) Init();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                DrawTabButton(Tab.Info, "Info");
                DrawTabButton(Tab.Assets, "Assets");
                DrawTabButton(Tab.Packages, "Packages");
                DrawTabButton(Tab.ProjectSettings, "Project Settings");
                GUILayout.FlexibleSpace();
            }

            switch (_activeTab)
            {
                case Tab.Info:            DrawInfoTab(); break;
                case Tab.Assets:          DrawAssetsTab(); break;
                case Tab.Packages:        DrawPackagesTab(); break;
                case Tab.ProjectSettings: DrawProjectSettingsTab(); break;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (_activeTab == Tab.Assets)
                {
                    int count = _treeView.GetCheckedFilePaths().Count;
                    GUILayout.Label($"{count} file{(count != 1 ? "s" : "")} selected", EditorStyles.miniLabel);
                    GUILayout.Space(8);
                }
                else if (_activeTab == Tab.Packages)
                {
                    GUILayout.Label($"{_packagesView.SelectedCount}/{_packagesView.TotalCount} packages selected", EditorStyles.miniLabel);
                    GUILayout.Space(8);
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_templateName) || string.IsNullOrWhiteSpace(_templateId)))
                {
                    if (GUILayout.Button("Export", GUILayout.Width(80), GUILayout.Height(24)))
                        RunExport();
                }
                GUILayout.Space(6);
            }

            EditorGUILayout.Space(6);
        }

        void DrawTabButton(Tab tab, string label)
        {
            var prev = GUI.backgroundColor;
            if (_activeTab == tab) GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Toggle(_activeTab == tab, label, EditorStyles.toolbarButton, GUILayout.Width(100)) && _activeTab != tab)
                _activeTab = tab;
            GUI.backgroundColor = prev;
        }

        void DrawInfoTab()
        {
            EditorGUILayout.Space(12);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Display Name", EditorStyles.boldLabel);
                    _templateName = EditorGUILayout.TextField(_templateName);

                    EditorGUILayout.Space(8);

                    EditorGUILayout.LabelField("Package ID", EditorStyles.boldLabel);
                    _templateId = EditorGUILayout.TextField(_templateId);
                    EditorGUILayout.LabelField("e.g. com.yourcompany.template.my-starter", EditorStyles.miniLabel);

                    EditorGUILayout.Space(8);

                    EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
                    _templateDescription = EditorGUILayout.TextArea(_templateDescription,
                        GUILayout.Height(100), GUILayout.ExpandWidth(true));

                    EditorGUILayout.Space(8);
                    EditorGUILayout.HelpBox(
                        "Package ID must follow reverse-domain format (e.g. com.yourcompany.template.name) for Unity Hub to recognize the template.",
                        MessageType.Info);
                }
                GUILayout.Space(12);
            }
        }

        void DrawAssetsTab()
        {
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);
                    _settingsToggles[file] = EditorGUILayout.ToggleLeft(label, _settingsToggles[file]);
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "ProjectVersion.txt and machine-specific settings are always excluded.",
                MessageType.None);
        }

        void SetAllSettings(bool value)
        {
            foreach (var (file, _) in TemplateExporterData.ToggleableSettings)
                _settingsToggles[file] = value;
        }

        HashSet<string> BuildSettingsExcludeSet()
        {
            var exclude = new HashSet<string>(TemplateExporterData.AlwaysExcluded);
            foreach (var (file, _) in TemplateExporterData.ToggleableSettings)
                if (!_settingsToggles[file])
                    exclude.Add(file);
            return exclude;
        }

        async void RunExport()
        {
            string savePath = EditorUtility.SaveFilePanel(
                "Export Project as Template",
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                _templateName.ToLower().Replace(" ", "-") + "-template",
                "tgz");

            if (string.IsNullOrEmpty(savePath)) return;

            bool success = await TemplateExporter.Export(
                _templateName,
                _templateId,
                _templateDescription,
                savePath,
                _treeView.GetCheckedFilePaths(),
                BuildSettingsExcludeSet(),
                _packagesView.GetSelectedRegistry(),
                _packagesView.GetSelectedEmbeddedPaths());

            if (success)
                EditorUtility.DisplayDialog("Done", $"Template saved to:\n{savePath}", "OK");
            else
                EditorUtility.DisplayDialog("Failed", "tar command failed. Ensure tar is available on your system.", "OK");
        }
    }
}