using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DNExtensions.Utilities
{
    internal static class TemplateImporter
    {
        internal static async Task<(bool success, string packageRoot, string tempDir)> Extract(string tgzPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "unity_template_import_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            bool ok = await Task.Run(() =>
            {
                bool isWindows = System.Environment.OSVersion.Platform == System.PlatformID.Win32NT;
                string shell = isWindows ? "cmd.exe" : "/bin/bash";
                string args = isWindows
                    ? $"/c tar -xzf \"{tgzPath}\" -C \"{tempDir}\""
                    : $"-c \"tar -xzf '{tgzPath}' -C '{tempDir}'\"";

                var psi = new ProcessStartInfo(shell, args)
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"[TemplateImporter] {process.StandardError.ReadToEnd()}");
                    return false;
                }
                return true;
            });

            if (!ok)
            {
                Directory.Delete(tempDir, true);
                return (false, null, null);
            }

            string packageRoot = FindPackageRoot(tempDir);
            if (packageRoot == null)
            {
                Directory.Delete(tempDir, true);
                return (false, null, null);
            }

            return (true, packageRoot, tempDir);
        }

        internal static (string name, string description) ReadPackageJson(string packageRoot)
        {
            string jsonPath = Path.Combine(packageRoot, "package.json");
            if (!File.Exists(jsonPath)) return ("Unknown", "");
            string json = File.ReadAllText(jsonPath);
            string name = ParseJsonString(json, "displayName") ?? ParseJsonString(json, "name") ?? "Unknown";
            string desc = ParseJsonString(json, "description") ?? "";
            return (name, desc);
        }

        internal static async Task<bool> Import(
            string packageRoot,
            List<string> selectedAssetFiles,
            HashSet<string> settingsToImport,
            Dictionary<string, string> selectedRegistryPackages,
            List<string> selectedEmbeddedPackagePaths)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Importing Template", "Copying assets...", 0.1f);

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string projectData = Path.Combine(packageRoot, "ProjectData~");
                string templateAssetsRoot = Path.Combine(projectData, "Assets");

                foreach (string srcPath in selectedAssetFiles)
                {
                    string relative = srcPath.Substring(templateAssetsRoot.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                    string dest = Path.Combine(Application.dataPath, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(srcPath, dest, true);

                    string metaSrc = srcPath + ".meta";
                    if (File.Exists(metaSrc))
                        File.Copy(metaSrc, dest + ".meta", true);

                    string dir = Path.GetDirectoryName(srcPath);
                    while (!string.IsNullOrEmpty(dir) && dir.Length > templateAssetsRoot.Length)
                    {
                        string folderMeta = dir + ".meta";
                        if (File.Exists(folderMeta))
                        {
                            string rel = folderMeta.Substring(packageRoot.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                            string destMeta = Path.Combine(projectRoot, rel);
                            if (!File.Exists(destMeta))
                                File.Copy(folderMeta, destMeta, true);
                        }
                        dir = Path.GetDirectoryName(dir);
                    }
                }

                EditorUtility.DisplayProgressBar("Importing Template", "Merging packages...", 0.6f);
                MergeManifest(projectRoot, selectedRegistryPackages);

                foreach (string embeddedPath in selectedEmbeddedPackagePaths)
                {
                    string folderName = Path.GetFileName(embeddedPath);
                    CopyDirectory(embeddedPath, Path.Combine(projectRoot, "Packages", folderName));
                }

                EditorUtility.DisplayProgressBar("Importing Template", "Applying settings...", 0.8f);
                CopySelectedSettings(projectData, projectRoot, settingsToImport);

                await Task.Yield();
                AssetDatabase.Refresh();
                return true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void MergeManifest(string projectRoot, Dictionary<string, string> selectedPackages)
        {
            string dstManifest = Path.Combine(projectRoot, "Packages", "manifest.json");
            var dstDeps = File.Exists(dstManifest)
                ? ParseDependencies(File.ReadAllText(dstManifest))
                : new Dictionary<string, string>();

            foreach (var kvp in selectedPackages)
                if (!dstDeps.ContainsKey(kvp.Key))
                    dstDeps[kvp.Key] = kvp.Value;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"dependencies\": {");
            var entries = new System.Collections.Generic.List<string>();
            foreach (var kvp in dstDeps)
                entries.Add($"    \"{kvp.Key}\": \"{kvp.Value}\"");
            sb.AppendLine(string.Join(",\n", entries));
            sb.AppendLine("  }");
            sb.Append("}");
            File.WriteAllText(dstManifest, sb.ToString());
        }

        static void CopySelectedSettings(string packageRoot, string projectRoot, HashSet<string> settingsToImport)
        {
            string srcSettings = Path.Combine(packageRoot, "ProjectSettings");
            string dstSettings = Path.Combine(projectRoot, "ProjectSettings");
            if (!Directory.Exists(srcSettings)) return;

            foreach (string file in Directory.GetFiles(srcSettings))
            {
                string filename = Path.GetFileName(file);
                if (!settingsToImport.Contains(filename)) continue;
                File.Copy(file, Path.Combine(dstSettings, filename), true);
            }
        }

        static void CopyDirectory(string source, string destination)
        {
            source = Path.GetFullPath(source);
            if (!Directory.Exists(source)) return;
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length + 1);
                string dest = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(file, dest, true);
            }
        }

        static string FindPackageRoot(string tempDir)
        {
            if (File.Exists(Path.Combine(tempDir, "package.json")))
                return tempDir;
            foreach (string dir in Directory.GetDirectories(tempDir))
                if (File.Exists(Path.Combine(dir, "package.json")))
                    return dir;
            return null;
        }

        static Dictionary<string, string> ParseDependencies(string json)
        {
            var deps = new Dictionary<string, string>();
            int start = json.IndexOf("\"dependencies\"");
            if (start < 0) return deps;
            int brace = json.IndexOf('{', start);
            int end = json.IndexOf('}', brace);
            if (brace < 0 || end < 0) return deps;

            string block = json.Substring(brace + 1, end - brace - 1);
            foreach (string line in block.Split(','))
            {
                string trimmed = line.Trim();
                int colon = trimmed.IndexOf("\":");
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
            int idx = json.IndexOf(search);
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