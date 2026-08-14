using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Creates and tracks symlinked folders under Assets/. The tracked list is the same one the
    /// Project window badges read, so a folder linked here is watched from the moment it exists.
    /// </summary>
    internal class LinkedAssetsWindow : EditorWindow
    {
        private const string DefaultName = "NewLinkedAssets";

        private string _folderName = DefaultName;
        private string _targetPath = string.Empty;
        private string _trackName = string.Empty;
        private Vector2 _scroll;

        [MenuItem("Tools/DNExtensions/Linked Assets", false, 1001)]
        public static void ShowWindow()
        {
            GetWindow<LinkedAssetsWindow>("Linked Assets");
        }

        private void OnGUI()
        {
            ProjectSettings settings = HelpfulEditorSettings.Project;

            EditorGUILayout.LabelField("Linked Assets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Folders under Assets/ that are symlinks to somewhere else on disk.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(8);
            DrawTracked(settings);

            EditorGUILayout.Space(12);
            DrawCreate(settings);
        }

        private void DrawTracked(ProjectSettings settings)
        {
            EditorGUILayout.LabelField("Tracked", EditorStyles.boldLabel);

            if (settings.linkedAssetFolders.Count == 0) EditorGUILayout.HelpBox("Nothing tracked yet.", MessageType.Info);
            else DrawTrackedList(settings);

            DrawTrackExisting(settings);
        }

        /// <summary>
        /// Adds a folder that is already on disk. Creating a link tracks it automatically, so this is
        /// for the other way round — a symlink made outside Unity, or one that was untracked.
        /// </summary>
        private void DrawTrackExisting(ProjectSettings settings)
        {
            EditorGUILayout.BeginHorizontal();

            _trackName = EditorGUILayout.TextField("Track Existing", _trackName);

            bool valid = !string.IsNullOrWhiteSpace(_trackName) && !settings.linkedAssetFolders.Contains(_trackName.Trim());

            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button("Track", GUILayout.Width(64f)))
                {
                    settings.linkedAssetFolders.Add(_trackName.Trim());
                    HelpfulEditorSettings.SaveProject();

                    _trackName = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("A folder name directly under Assets/, without the Assets/ prefix.", EditorStyles.miniLabel);
        }

        private void DrawTrackedList(ProjectSettings settings)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));

            // Backwards so an untrack does not disturb the indices still to be drawn.
            for (int i = settings.linkedAssetFolders.Count - 1; i >= 0; i--)
            {
                string folder = settings.linkedAssetFolders[i];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.BeginVertical();

                string assetPath = "Assets/" + folder;
                if (GUILayout.Button(new GUIContent(assetPath, "Ping this folder in the Project window."), EditorStyles.boldLabel))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset) EditorGUIUtility.PingObject(asset);
                }

                EditorGUILayout.LabelField(DescribeState(folder), EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Untrack", GUILayout.Width(70f), GUILayout.Height(28f)))
                {
                    settings.linkedAssetFolders.RemoveAt(i);
                    HelpfulEditorSettings.SaveProject();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private static string DescribeState(string folder)
        {
            string full = LinkedAssets.FullPath(folder);
            if (!Directory.Exists(full)) return "Missing from disk";

            DirectoryInfo info = new DirectoryInfo(full);
            bool linked = (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

            return linked ? "Linked" : "Broken — a real folder, not syncing";
        }

        private void DrawCreate(ProjectSettings settings)
        {
            EditorGUILayout.LabelField("Create", EditorStyles.boldLabel);

            _folderName = EditorGUILayout.TextField("Folder Name", _folderName);

            EditorGUILayout.BeginHorizontal();
            _targetPath = EditorGUILayout.TextField("Links To", _targetPath);

            if (GUILayout.Button("Browse", GUILayout.Width(64f)))
            {
                string selected = EditorUtility.OpenFolderPanel("Folder To Link To", _targetPath, string.Empty);
                if (!string.IsNullOrEmpty(selected))
                {
                    _targetPath = selected;
                    if (_folderName == DefaultName) _folderName = Path.GetFileName(selected);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Create Link", GUILayout.Height(28f))) Create(settings);
        }

        private void Create(ProjectSettings settings)
        {
            if (string.IsNullOrWhiteSpace(_folderName) || string.IsNullOrWhiteSpace(_targetPath))
            {
                EditorUtility.DisplayDialog("Linked Assets", "Both a folder name and a target are needed.", "OK");
                return;
            }

            string localPath = LinkedAssets.FullPath(_folderName);

            if (Directory.Exists(localPath) || File.Exists(localPath))
            {
                EditorUtility.DisplayDialog("Linked Assets", $"Assets/{_folderName} already exists. Remove or rename it first.", "OK");
                return;
            }

            if (!Directory.Exists(_targetPath))
            {
                EditorUtility.DisplayDialog("Linked Assets", "That target folder does not exist on disk.", "OK");
                return;
            }

            if (!CreateSymlink(localPath, _targetPath))
            {
                EditorUtility.DisplayDialog("Linked Assets", $"Could not create the link.\n\n{FailureHint}", "OK");
                return;
            }

            if (!settings.linkedAssetFolders.Contains(_folderName))
            {
                settings.linkedAssetFolders.Add(_folderName);
                HelpfulEditorSettings.SaveProject();
            }

            AssetDatabase.Refresh();
        }

        /// <summary>Why the link most likely failed here, since the reason is entirely platform-specific.</summary>
#if UNITY_EDITOR_WIN
        private const string FailureHint = "On Windows this usually means Developer Mode is off — without it, " +
                                           "creating a symlink needs administrator rights.";
#else
        private const string FailureHint = "Check that the target exists and that this location is writable.";
#endif

        /// <summary>
        /// Shelling out because .NET has no symlink API before .NET 6 — mklink is a cmd builtin
        /// rather than an executable, hence the /c.
        /// </summary>
        private static bool CreateSymlink(string localPath, string targetPath)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

#if UNITY_EDITOR_WIN
                start.FileName = "cmd.exe";
                start.Arguments = $"/c mklink /d \"{localPath.Replace('/', '\\')}\" \"{targetPath.Replace('/', '\\')}\"";
#else
                // sh rather than bash: POSIX guarantees the former, and macOS has been steadily
                // demoting the latter. A single quote inside a path would otherwise close the quoting
                // and hand the rest of the name to the shell as commands.
                start.FileName = "/bin/sh";
                start.Arguments = $"-c \"ln -s {ShellQuote(targetPath)} {ShellQuote(localPath)}\"";
#endif

                using (Process process = Process.Start(start))
                {
                    if (process == null) return false;

                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not create symlink: {e.Message}");
                return false;
            }
        }

#if !UNITY_EDITOR_WIN
        /// <summary>
        /// A path as one shell word. Single quotes protect everything but a single quote, which is
        /// closed, escaped and reopened — the standard POSIX dance, and the only character a folder
        /// name is at all likely to carry that would otherwise end the quoting.
        /// </summary>
        private static string ShellQuote(string path)
        {
            return $"'{path.Replace("'", "'\\''")}'";
        }
#endif
    }
}
