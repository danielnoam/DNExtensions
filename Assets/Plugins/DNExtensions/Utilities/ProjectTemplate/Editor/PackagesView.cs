using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DNExtensions.Utilities
{
    internal class PackagesView
    {
        internal class RegistryPackage
        {
            internal string Id;
            internal string Version;
            internal bool Checked = true;
        }

        internal class EmbeddedPackage
        {
            internal string Name;
            internal string FolderPath;
            internal bool Checked = true;
        }

        readonly List<RegistryPackage> _registry = new();
        readonly List<EmbeddedPackage> _embedded = new();
        Vector2 _scroll;

        internal void Load(string packagesFolder)
        {
            _registry.Clear();
            _embedded.Clear();

            string manifestPath = Path.Combine(packagesFolder, "manifest.json");
            if (File.Exists(manifestPath))
            {
                foreach (var kvp in ParseDependencies(File.ReadAllText(manifestPath)))
                    _registry.Add(new RegistryPackage { Id = kvp.Key, Version = kvp.Value });
            }

            if (Directory.Exists(packagesFolder))
            {
                foreach (string dir in Directory.GetDirectories(packagesFolder).OrderBy(d => d))
                {
                    string pkgJson = Path.Combine(dir, "package.json");
                    if (!File.Exists(pkgJson)) continue;
                    string displayName = ParseJsonString(File.ReadAllText(pkgJson), "displayName")
                        ?? Path.GetFileName(dir);
                    _embedded.Add(new EmbeddedPackage { Name = displayName, FolderPath = dir });
                }
            }
        }

        internal void Draw(bool showAllNone = true)
        {
            if (showAllNone)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(36)))
                        SetAll(true);
                    if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
                        SetAll(false);
                    GUILayout.FlexibleSpace();
                }
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            if (_registry.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Registry Packages", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                foreach (var pkg in _registry)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(8);
                        pkg.Checked = EditorGUILayout.ToggleLeft(GUIContent.none, pkg.Checked, GUILayout.Width(20));
                        EditorGUILayout.LabelField(pkg.Id, GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField(pkg.Version, EditorStyles.miniLabel, GUILayout.Width(100));
                    }
                }
            }

            if (_embedded.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Embedded Packages", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                foreach (var pkg in _embedded)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(8);
                        pkg.Checked = EditorGUILayout.ToggleLeft(pkg.Name, pkg.Checked);
                    }
                }
            }

            if (_registry.Count == 0 && _embedded.Count == 0)
                EditorGUILayout.HelpBox("No packages found.", MessageType.Info);
        }

        internal void SetAll(bool value)
        {
            foreach (var p in _registry) p.Checked = value;
            foreach (var p in _embedded) p.Checked = value;
        }

        // Returns selected registry packages as id -> version
        internal Dictionary<string, string> GetSelectedRegistry() =>
            _registry.Where(p => p.Checked).ToDictionary(p => p.Id, p => p.Version);

        // Returns selected embedded package folder paths
        internal List<string> GetSelectedEmbeddedPaths() =>
            _embedded.Where(p => p.Checked).Select(p => p.FolderPath).ToList();

        internal int SelectedCount =>
            _registry.Count(p => p.Checked) + _embedded.Count(p => p.Checked);

        internal int TotalCount => _registry.Count + _embedded.Count;

        static Dictionary<string, string> ParseDependencies(string json)
        {
            var deps = new Dictionary<string, string>();
            int start = json.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            if (start < 0) return deps;
            int brace = json.IndexOf('{', start);
            int end = json.IndexOf('}', brace);
            if (brace < 0 || end < 0) return deps;

            string block = json.Substring(brace + 1, end - brace - 1);
            foreach (string line in block.Split(','))
            {
                string trimmed = line.Trim();
                int colon = trimmed.IndexOf("\":", StringComparison.Ordinal);
                if (colon < 0) continue;
                string key = trimmed.Substring(0, colon).Trim().Trim('"');
                string val = trimmed.Substring(colon + 2).Trim().Trim('"');
                if (!string.IsNullOrEmpty(key))
                    deps[key] = val;
            }
            return deps;
        }

        static string ParseJsonString(string json, string key)
        {
            string search = $"\"{key}\"";
            int idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return null;
            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return null;
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }
    }
}