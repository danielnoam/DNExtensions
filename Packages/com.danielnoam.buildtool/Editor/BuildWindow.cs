using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace DNExtensions.BuildTool
{
    /// <summary>
    /// Front end for <see cref="BuildRunner"/>. The Build tab holds what changes per build: version, targets, uploads.
    /// The Settings tab holds the project's rarely changed setup and this computer's own folders, keystore and logins.
    /// </summary>
    internal class BuildWindow : EditorWindow
    {
        public const string MenuPath = "Tools/DNExtensions/Build";

        private static readonly string[] Tabs = { "Build", "Settings" };

        [SerializeField] private int tab;

        private SOBuildConfig _config;
        private SerializedObject _serializedConfig;
        private Vector2 _scroll;
        private List<BuildRunner.Issue> _issues;
        private BuildRunner.RunResult _lastResult;
        private string _ghStatus;
        private bool _ghReady;
        private string _repository;
        private string _notesPreview;
        private bool _notesFoldout;
        private string _signingStatus;
        private bool _signingOk;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<BuildWindow>("Build");
            window.minSize = new Vector2(460f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            _config = SOBuildConfig.LoadOrCreate();
            BuildMachineSettings.MigrateFromConfig(_config);
            _serializedConfig = new SerializedObject(_config);
            RefreshGitHubStatus();
        }

        private void OnGUI()
        {
            if (!_config)
            {
                OnEnable();
                return;
            }

            _serializedConfig.Update();

            EditorGUILayout.Space(4);
            tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(24f));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (tab == 0) DrawBuildTab();
            else DrawSettingsTab();

            EditorGUILayout.EndScrollView();

            if (!_serializedConfig.ApplyModifiedProperties()) return;

            AndroidSdkPin.Apply();
            _repository = BuildRunner.Repository(_config);
        }

        private void DrawBuildTab()
        {
            DrawVersion();
            DrawTargets();
            DrawUploads();
            DrawIssues();
            DrawActions();
            DrawLastResult();
        }

        private void DrawSettingsTab()
        {
            DrawProjectSettings();
            DrawThisComputer();
            DrawSigning();
            DrawGitHubLogin();
            DrawItchSettings();
            DrawSteamSettings();
        }

        private void DrawVersion()
        {
            Header("Version");

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string version = EditorGUILayout.DelayedTextField(BuildRunner.Version, GUILayout.Width(90f));
            if (EditorGUI.EndChangeCheck()) BuildRunner.SetVersion(version);

            if (GUILayout.Button("+ Patch", EditorStyles.miniButtonLeft)) BuildRunner.BumpVersion(BuildRunner.VersionPart.Patch);
            if (GUILayout.Button("+ Minor", EditorStyles.miniButtonMid)) BuildRunner.BumpVersion(BuildRunner.VersionPart.Minor);
            if (GUILayout.Button("+ Major", EditorStyles.miniButtonRight)) BuildRunner.BumpVersion(BuildRunner.VersionPart.Major);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Android version code {BuildRunner.AndroidVersionCode(BuildRunner.Version)}, release tag v{BuildRunner.Version}, files named {_config.ResolvedFileName}", EditorStyles.miniLabel);
        }

        private void DrawTargets()
        {
            Header("Targets");

            var targets = _serializedConfig.FindProperty("targets");

            for (int i = 0; i < targets.arraySize; i++)
            {
                var target = targets.GetArrayElementAtIndex(i);
                var kind = (SOBuildConfig.TargetKind)target.FindPropertyRelative("kind").enumValueIndex;
                var profile = target.FindPropertyRelative("profile");

                EditorGUILayout.BeginHorizontal();

                var enabled = target.FindPropertyRelative("enabled");
                enabled.boolValue = EditorGUILayout.ToggleLeft(Nicify(kind), enabled.boolValue, EditorStyles.boldLabel, GUILayout.Width(110f));

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUILayout.PropertyField(profile, GUIContent.none);

                    var development = target.FindPropertyRelative("developmentBuild");
                    development.boolValue = GUILayout.Toggle(development.boolValue, "Dev", EditorStyles.miniButton, GUILayout.Width(40f));
                }

                EditorGUILayout.EndHorizontal();

                if (profile.objectReferenceValue) continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(114f);
                if (GUILayout.Button($"Create {Nicify(kind)} Profile", EditorStyles.miniButton)) CreateProfile(kind, profile);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField("Targets build one after another; the active platform goes first and is restored after.", EditorStyles.wordWrappedMiniLabel);

            string folder = BuildMachineSettings.ResolveOutputRoot(_config);
            using (new EditorGUI.DisabledScope(!Directory.Exists(folder)))
            {
                if (GUILayout.Button(new GUIContent("Open Build Folder", folder), EditorStyles.miniButton)) EditorUtility.RevealInFinder(folder);
            }
        }

        private void DrawUploads()
        {
            Header("Uploads");

            var copy = _serializedConfig.FindProperty("copyToFolder");
            EditorGUILayout.PropertyField(copy, new GUIContent("Copy To Folder"));
            if (copy.boolValue && string.IsNullOrWhiteSpace(BuildMachineSettings.CopyFolder))
            {
                SettingsHint("No copy folder is set on this computer.");
            }

            var github = _serializedConfig.FindProperty("uploadToGitHub");
            EditorGUILayout.PropertyField(github, new GUIContent("GitHub Release"));
            if (github.boolValue) DrawGitHubUpload();

            var itch = _serializedConfig.FindProperty("uploadToItch");
            EditorGUILayout.PropertyField(itch, new GUIContent("itch.io"));
            if (itch.boolValue)
            {
                EditorGUI.indentLevel++;
                string project = string.IsNullOrWhiteSpace(_config.itchProject) ? "no project set" : _config.itchProject.Trim();
                EditorGUILayout.LabelField("Pushes To", $"{project} : {ItchUpload.Channel(_config, SOBuildConfig.TargetKind.Windows)}, {ItchUpload.Channel(_config, SOBuildConfig.TargetKind.AndroidApk)}");
                EditorGUI.indentLevel--;
                if (string.IsNullOrWhiteSpace(_config.itchProject)) SettingsHint("No itch.io project is set.");
            }

            var steam = _serializedConfig.FindProperty("uploadToSteam");
            EditorGUILayout.PropertyField(steam, new GUIContent("Steam"));
            if (!steam.boolValue) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("steamSetLiveBranch"), new GUIContent("Set Live On Branch"));
            EditorGUI.indentLevel--;
            if (_config.steamAppId <= 0 || _config.steamWindowsDepotId <= 0) SettingsHint("Steam needs an App ID and a Windows depot ID.");
            else if (string.IsNullOrWhiteSpace(BuildMachineSettings.SteamUser)) SettingsHint("No Steam user is set on this computer.");
        }

        private void DrawGitHubUpload()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("githubReleaseType"), new GUIContent("Release As"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("replaceExistingRelease"), new GUIContent("Replace Existing Release"));

            EditorGUILayout.LabelField("Repository", string.IsNullOrEmpty(_repository) ? "none set" : _repository);
            if (!_ghReady) SettingsHint("The GitHub CLI isn't ready on this computer.");

            _notesFoldout = EditorGUILayout.Foldout(_notesFoldout, "Release Notes Preview", true);
            if (_notesFoldout)
            {
                if (_notesPreview == null || GUILayout.Button("Regenerate", EditorStyles.miniButton)) _notesPreview = BuildRunner.BuildReleaseNotes();
                EditorGUILayout.HelpBox(_notesPreview, MessageType.None);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawProjectSettings()
        {
            Header("Project");
            EditorGUILayout.LabelField("Saved in the build config asset, shared by everyone on the project.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("fileName"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("outputRoot"), new GUIContent("Default Build Folder", "Relative to the project folder. This computer can use its own folder below"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("zipWindowsBuild"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("androidTargetApi"), new GUIContent("Android Target API"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("githubRepository"), new GUIContent("GitHub Repository"));

            var checks = _serializedConfig.FindProperty("runProjectChecks");
            EditorGUILayout.PropertyField(checks);
            if (!checks.boolValue) return;

            var found = BuildChecks.All().Select(check => check.name).ToList();
            EditorGUILayout.LabelField(found.Count > 0 ? $"Checks found: {string.Join(", ", found)}" : "No [BuildCheck] methods in this project.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawThisComputer()
        {
            Header("This Computer");
            EditorGUILayout.LabelField("Saved only on this computer, for this project.", EditorStyles.wordWrappedMiniLabel);

            string folder = BuildMachineSettings.ResolveOutputRoot(_config);
            bool overridden = !string.IsNullOrWhiteSpace(BuildMachineSettings.OutputFolderOverride);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Build Folder", "Where builds go on this computer"), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
            EditorGUILayout.SelectableLabel(folder, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("Browse", GUILayout.Width(60f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Build into", Directory.Exists(folder) ? folder : BuildProcess.ProjectFolder, string.Empty);
                if (!string.IsNullOrEmpty(picked)) BuildMachineSettings.OutputFolderOverride = picked;
            }
            using (new EditorGUI.DisabledScope(!overridden))
            {
                if (GUILayout.Button(new GUIContent("Reset", $"Back to the project default ({_config.outputRoot})"), GUILayout.Width(50f)))
                {
                    BuildMachineSettings.OutputFolderOverride = string.Empty;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string copyFolder = EditorGUILayout.TextField(new GUIContent("Copy Folder", "Where Copy To Folder puts each version's files"), BuildMachineSettings.CopyFolder);
            if (EditorGUI.EndChangeCheck()) BuildMachineSettings.CopyFolder = copyFolder;
            if (GUILayout.Button("Browse", GUILayout.Width(60f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Copy builds to", BuildMachineSettings.CopyFolder, string.Empty);
                if (!string.IsNullOrEmpty(picked)) BuildMachineSettings.CopyFolder = picked;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSigning()
        {
            Header("Android Signing");

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string path = EditorGUILayout.TextField("Keystore", AndroidSigning.KeystorePath);
            if (EditorGUI.EndChangeCheck()) AndroidSigning.KeystorePath = path;
            if (GUILayout.Button("Browse", GUILayout.Width(60f)))
            {
                string picked = EditorUtility.OpenFilePanel("Release keystore", Path.GetDirectoryName(AndroidSigning.KeystorePath), "keystore,jks");
                if (!string.IsNullOrEmpty(picked)) AndroidSigning.KeystorePath = picked;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            string alias = EditorGUILayout.TextField("Alias", AndroidSigning.Alias);
            if (EditorGUI.EndChangeCheck()) AndroidSigning.Alias = alias;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string password = EditorGUILayout.PasswordField(new GUIContent("Password", "Kept for this editor session, or saved encrypted for this Windows user with Remember"), AndroidSigning.Password);
            if (EditorGUI.EndChangeCheck())
            {
                AndroidSigning.Password = password;
                _signingStatus = null;
            }
            if (GUILayout.Button("Test", GUILayout.Width(60f)))
            {
                _signingOk = AndroidSigning.Verify(out _signingStatus);
            }
            EditorGUILayout.EndHorizontal();

            AndroidSigning.RememberPassword = EditorGUILayout.Toggle(new GUIContent("Remember Password",
                "Save the password on this computer, encrypted so only your Windows user can read it. Off: it is asked for again after Unity restarts."),
                AndroidSigning.RememberPassword);

            if (!string.IsNullOrEmpty(_signingStatus))
            {
                EditorGUILayout.HelpBox(_signingStatus, _signingOk ? MessageType.Info : MessageType.Error);
            }

            if (AndroidSigning.KeystoreExists) return;

            EditorGUILayout.HelpBox("No keystore at this path. Pick your keystore file with Browse (create one in Player Settings > Publishing Settings > Keystore Manager).", MessageType.Warning);
        }

        private void DrawGitHubLogin()
        {
            Header("GitHub");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(_ghStatus, _ghReady ? MessageType.Info : MessageType.Warning);
            if (GUILayout.Button("Refresh", GUILayout.Width(60f), GUILayout.Height(38f))) RefreshGitHubStatus();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawItchSettings()
        {
            Header("itch.io");

            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("itchProject"), new GUIContent("Project"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("itchWindowsChannel"), new GUIContent("Windows Channel"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("itchAndroidChannel"), new GUIContent("Android Channel"));

            PathField("butler", "Empty finds butler on PATH or in the itch app", () => BuildMachineSettings.ButlerPath, value => BuildMachineSettings.ButlerPath = value);

            EditorGUI.BeginChangeCheck();
            string key = EditorGUILayout.PasswordField(new GUIContent("API Key", "Saved encrypted for this Windows user. Empty uses butler login or BUTLER_API_KEY"), BuildMachineSettings.ItchApiKey);
            if (EditorGUI.EndChangeCheck()) BuildMachineSettings.ItchApiKey = key;

            EditorGUILayout.LabelField("Run Checks with itch.io on to test butler and the login.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawSteamSettings()
        {
            Header("Steam");

            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("steamAppId"), new GUIContent("App ID"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("steamWindowsDepotId"), new GUIContent("Windows Depot ID"));

            PathField("steamcmd", "Empty finds steamcmd on PATH", () => BuildMachineSettings.SteamCmdPath, value => BuildMachineSettings.SteamCmdPath = value);

            EditorGUI.BeginChangeCheck();
            string user = EditorGUILayout.TextField(new GUIContent("Steam User", "The account that uploads. Log in once by hand so steamcmd caches it"), BuildMachineSettings.SteamUser);
            if (EditorGUI.EndChangeCheck()) BuildMachineSettings.SteamUser = user;

            EditorGUILayout.LabelField("No password is saved. Log in once in a terminal, with Steam Guard: steamcmd +login <user>. Run Checks with Steam on to test it.", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>A per-machine executable path with a Browse button.</summary>
        private static void PathField(string label, string tooltip, System.Func<string> get, System.Action<string> set)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string path = EditorGUILayout.TextField(new GUIContent(label, tooltip), get());
            if (EditorGUI.EndChangeCheck()) set(path);
            if (GUILayout.Button("Browse", GUILayout.Width(60f)))
            {
                string picked = EditorUtility.OpenFilePanel($"Find {label}", string.IsNullOrEmpty(get()) ? string.Empty : Path.GetDirectoryName(get()), "exe");
                if (!string.IsNullOrEmpty(picked)) set(picked);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIssues()
        {
            if (_issues == null) return;

            Header("Check Results");

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("All checks passed.", MessageType.Info);
                return;
            }

            foreach (var issue in _issues)
            {
                EditorGUILayout.HelpBox(issue.Message, issue.Severity == BuildRunner.Severity.Error ? MessageType.Error : MessageType.Warning);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10);

            bool hasUploads = BuildRunner.HasUploads(_config);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Run Checks", GUILayout.Height(30f))) _issues = BuildRunner.Preflight(_config, hasUploads);
                if (GUILayout.Button("Build", GUILayout.Height(30f))) StartRun(false);

                using (new EditorGUI.DisabledScope(!hasUploads))
                {
                    if (GUILayout.Button("Build & Upload", GUILayout.Height(30f))) StartRun(true);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawLastResult()
        {
            if (_lastResult == null) return;

            Header($"Last Run: {_lastResult.Version}");

            foreach (var step in _lastResult.Steps)
            {
                string text = $"{step.Name} ({step.Seconds:0.0}s)\n{step.Message}";
                EditorGUILayout.HelpBox(text, step.Succeeded ? MessageType.Info : MessageType.Error);
            }

            if (Directory.Exists(_lastResult.OutputFolder) && GUILayout.Button("Open Build Folder", EditorStyles.miniButton))
            {
                EditorUtility.RevealInFinder(_lastResult.OutputFolder);
            }
        }

        /// <summary>A warning that points at the Settings tab, where the missing value is set.</summary>
        private void SettingsHint(string message)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            if (GUILayout.Button("Settings", GUILayout.Width(70f), GUILayout.Height(38f))) tab = 1;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Deferred past the GUI pass: a build switches platforms and reloads assets, which must not happen while the
        /// window is halfway through laying itself out.
        /// </summary>
        private void StartRun(bool upload)
        {
            _serializedConfig.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(_config);

            EditorApplication.delayCall += () =>
            {
                _issues = BuildRunner.Preflight(_config, upload);
                if (_issues.Any(issue => issue.Severity == BuildRunner.Severity.Error))
                {
                    Repaint();
                    return;
                }

                string destinations = upload ? string.Join(" and ", UploadNames()) : "nowhere (build only)";
                var enabledTargets = _config.targets.Where(target => target.enabled).Select(target => Nicify(target.kind));
                if (!EditorUtility.DisplayDialog("Build", $"Build {_config.ResolvedFileName} {BuildRunner.Version} for {string.Join(", ", enabledTargets)} and upload to {destinations}?", "Build", "Cancel")) return;

                _lastResult = BuildRunner.Run(_config, upload);
                Repaint();
            };
        }

        private IEnumerable<string> UploadNames()
        {
            if (_config.copyToFolder) yield return "the copy folder";
            if (_config.uploadToGitHub) yield return $"GitHub ({_config.githubReleaseType.ToString().ToLowerInvariant()} release)";
            if (_config.uploadToItch) yield return $"itch.io ({_config.itchProject})";
            if (_config.uploadToSteam) yield return string.IsNullOrWhiteSpace(_config.steamSetLiveBranch) ? "Steam" : $"Steam (live on {_config.steamSetLiveBranch.Trim()})";
        }

        private void RefreshGitHubStatus()
        {
            _repository = BuildRunner.Repository(_config);

            var version = BuildProcess.Gh("--version", 10);
            if (!version.Succeeded)
            {
                _ghReady = false;
                _ghStatus = "GitHub CLI not found. Install: winget install --id GitHub.cli, then run: gh auth login";
                return;
            }

            var auth = BuildProcess.Gh("auth status", 15);
            _ghReady = auth.Succeeded;
            _ghStatus = auth.Succeeded ? "GitHub CLI ready. " + FirstLine(auth.Message) : "GitHub CLI not signed in. Run: gh auth login";
        }

        private void CreateProfile(SOBuildConfig.TargetKind kind, SerializedProperty profileProperty)
        {
            string wanted = kind == SOBuildConfig.TargetKind.AndroidApk ? "Android" : "Windows";
            var matches = BuildProfile.GetInstalledPlatformModules()
                .Where(module => module.displayName != null && module.displayName.Contains(wanted))
                .ToList();

            if (matches.Count == 0)
            {
                EditorUtility.DisplayDialog("Build", $"The {wanted} build support module is not installed. Add it from Unity Hub.", "OK");
                return;
            }

            var profile = BuildProfile.CreateBuildProfile(matches[0].platformGuid, $"{_config.ResolvedFileName} {wanted}");
            profileProperty.objectReferenceValue = profile;
            _serializedConfig.ApplyModifiedProperties();
        }

        private static string FirstLine(string text)
        {
            return (text ?? string.Empty).Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.Contains("Logged in")) ?? string.Empty;
        }

        private static string Nicify(SOBuildConfig.TargetKind kind) => kind == SOBuildConfig.TargetKind.AndroidApk ? "Android APK" : "Windows";

        private static void Header(string text)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }
    }
}
