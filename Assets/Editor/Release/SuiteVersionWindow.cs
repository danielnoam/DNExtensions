using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DNExtensions.Utilities;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace DNExtensions.Release
{
    /// <summary>
    /// Sets one version on every DNExtensions package: each package.json, the com.danielnoam dependencies between them,
    /// the lock file, the README install URLs and a new dated CHANGELOG section. Tag the merged commit vX.Y.Z afterwards.
    /// </summary>
    internal class SuiteVersionWindow : EditorWindow
    {
        private const string PackagePrefix = "com.danielnoam.";

        private static readonly Regex SemVer = new Regex(@"^\d+\.\d+\.\d+$");
        private static readonly Regex VersionLine = new Regex(@"^(\s*""version"":\s*"")([^""]+)("")", RegexOptions.Multiline);
        private static readonly Regex DependencyVersion = new Regex(@"(""com\.danielnoam\.[a-z]+"":\s*"")\d+\.\d+\.\d+("")");
        private static readonly Regex ReadmeTag = new Regex(@"(DNExtensions\.git\?path=/Packages/com\.danielnoam\.[a-z]+)#v\d+\.\d+\.\d+");

        private string _currentVersion;
        private string _newVersion;

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string PackagesFolder => Path.Combine(ProjectRoot, "Packages");

        [MenuItem(DNExtensionsMenu.Root + "Release/Set Suite Version")]
        public static void ShowWindow()
        {
            var window = GetWindow<SuiteVersionWindow>(true, "Set Suite Version");
            window.minSize = new Vector2(320f, 130f);
            window.maxSize = new Vector2(320f, 130f);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            _currentVersion = ReadCurrentVersion();
            _newVersion = _currentVersion;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Current", _currentVersion);
            _newVersion = EditorGUILayout.TextField("New", _newVersion).Trim();

            var isValid = SemVer.IsMatch(_newVersion) && _newVersion != _currentVersion;
            if (!SemVer.IsMatch(_newVersion))
            {
                EditorGUILayout.HelpBox("Use MAJOR.MINOR.PATCH, e.g. 1.3.0.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!isValid))
            {
                if (GUILayout.Button("Apply"))
                {
                    Apply(_newVersion);
                    Close();
                }
            }
        }

        private static string[] GetPackageJsonPaths()
        {
            return Directory.GetDirectories(PackagesFolder, PackagePrefix + "*")
                .Select(folder => Path.Combine(folder, "package.json"))
                .Where(File.Exists)
                .OrderBy(path => path)
                .ToArray();
        }

        private static string ReadCurrentVersion()
        {
            var path = GetPackageJsonPaths().FirstOrDefault();
            if (path == null)
            {
                return "?";
            }

            var match = VersionLine.Match(File.ReadAllText(path));
            return match.Success ? match.Groups[2].Value : "?";
        }

        private static void Apply(string version)
        {
            foreach (var path in GetPackageJsonPaths())
            {
                RewriteFile(path, text =>
                {
                    text = VersionLine.Replace(text, "${1}" + version + "${3}", 1);
                    return DependencyVersion.Replace(text, "${1}" + version + "${2}");
                });
            }

            RewriteFile(Path.Combine(PackagesFolder, "packages-lock.json"), text => DependencyVersion.Replace(text, "${1}" + version + "${2}"));
            RewriteFile(Path.Combine(ProjectRoot, "README.md"), text => ReadmeTag.Replace(text, "${1}#v" + version));
            RewriteFile(Path.Combine(ProjectRoot, "CHANGELOG.md"), text => AddChangelogSection(text, version));

            Client.Resolve();
            Debug.Log($"DNExtensions set to {version}. Commit, merge, then tag the merged commit v{version} and push the tag.");
        }

        private static string AddChangelogSection(string text, string version)
        {
            const string unreleased = "## [Unreleased]";
            if (!text.Contains(unreleased) || text.Contains($"## [{version}]"))
            {
                return text;
            }

            var index = text.IndexOf(unreleased, StringComparison.Ordinal) + unreleased.Length;
            return text.Insert(index, $"\n\n## [{version}] - {DateTime.Now:yyyy-MM-dd}");
        }

        private static void RewriteFile(string path, Func<string, string> rewrite)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Set Suite Version: {path} not found, skipped.");
                return;
            }

            var text = File.ReadAllText(path);
            var newText = rewrite(text);
            if (newText != text)
            {
                File.WriteAllText(path, newText);
            }
        }
    }
}
