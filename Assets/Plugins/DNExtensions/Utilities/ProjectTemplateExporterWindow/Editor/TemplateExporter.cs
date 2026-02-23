using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DNExtensions.Utilities
{
    internal static class TemplateExporter
    {
        internal static async Task<bool> Export(
            string templateName,
            string templateDescription,
            string savePath,
            List<string> selectedAssetFiles,
            HashSet<string> settingsExclude,
            Dictionary<string, string> selectedRegistryPackages,
            List<string> selectedEmbeddedPackagePaths)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "unity_template_" + System.Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(tempDir, "package");

            try
            {
                EditorUtility.DisplayProgressBar("Exporting Template", "Copying files...", 0.1f);
                Directory.CreateDirectory(packageRoot);

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;

                foreach (string absPath in selectedAssetFiles)
                {
                    string relative = absPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                    string dest = Path.Combine(packageRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(absPath, dest, true);

                    string metaSrc = absPath + ".meta";
                    if (File.Exists(metaSrc))
                        File.Copy(metaSrc, dest + ".meta", true);

                    string dir = Path.GetDirectoryName(absPath);
                    while (!string.IsNullOrEmpty(dir) && dir.Length > Application.dataPath.Length)
                    {
                        string folderMeta = dir + ".meta";
                        if (File.Exists(folderMeta))
                        {
                            string relMeta = folderMeta.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                            string destMeta = Path.Combine(packageRoot, relMeta);
                            if (!File.Exists(destMeta))
                                File.Copy(folderMeta, destMeta, true);
                        }
                        dir = Path.GetDirectoryName(dir);
                    }
                }

                EditorUtility.DisplayProgressBar("Exporting Template", "Writing packages...", 0.4f);
                WriteManifest(packageRoot, selectedRegistryPackages);

                foreach (string embeddedPath in selectedEmbeddedPackagePaths)
                {
                    string folderName = Path.GetFileName(embeddedPath);
                    CopyDirectory(embeddedPath, Path.Combine(packageRoot, "Packages", folderName));
                }

                CopyDirectory(Path.Combine(projectRoot, "ProjectSettings"), Path.Combine(packageRoot, "ProjectSettings"), settingsExclude);

                WritePackageJson(packageRoot, templateName, templateDescription);

                EditorUtility.DisplayProgressBar("Exporting Template", "Compressing...", 0.7f);
                bool success = await Task.Run(() => CompressToTgz(packageRoot, savePath));
                return success;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        static void WriteManifest(string packageRoot, Dictionary<string, string> deps)
        {
            string packagesDir = Path.Combine(packageRoot, "Packages");
            Directory.CreateDirectory(packagesDir);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"dependencies\": {");
            var entries = new List<string>();
            foreach (var kvp in deps)
                entries.Add($"    \"{kvp.Key}\": \"{kvp.Value}\"");
            sb.AppendLine(string.Join(",\n", entries));
            sb.AppendLine("  }");
            sb.Append("}");
            File.WriteAllText(Path.Combine(packagesDir, "manifest.json"), sb.ToString());
        }

        static void WritePackageJson(string packageRoot, string templateName, string description)
        {
            string unityVersion = Application.unityVersion;
            int lastDot = unityVersion.LastIndexOf('.');
            string shortVersion = lastDot > 0 ? unityVersion.Substring(0, lastDot) : unityVersion;
            string safeName = templateName.ToLower().Replace(" ", "-");
            string desc = string.IsNullOrWhiteSpace(description) ? templateName : description;

            string json = $@"{{
  ""name"": ""{safeName}"",
  ""displayName"": ""{templateName}"",
  ""version"": ""1.0.0"",
  ""type"": ""template"",
  ""host"": ""hub"",
  ""unity"": ""{shortVersion}"",
  ""description"": ""{desc}""
}}";
            File.WriteAllText(Path.Combine(packageRoot, "package.json"), json);
        }

        static bool CompressToTgz(string sourceDir, string outputPath)
        {
            string parentDir = Path.GetDirectoryName(sourceDir);
            string folderName = Path.GetFileName(sourceDir);
            bool isWindows = System.Environment.OSVersion.Platform == System.PlatformID.Win32NT;

            string shell = isWindows ? "cmd.exe" : "/bin/bash";
            string args = isWindows
                ? $"/c tar -czf \"{outputPath}\" -C \"{parentDir}\" \"{folderName}\""
                : $"-c \"tar -czf '{outputPath}' -C '{parentDir}' '{folderName}'\"";

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
                UnityEngine.Debug.LogError($"[TemplateExporter] {process.StandardError.ReadToEnd()}");
                return false;
            }
            return true;
        }

        static void CopyDirectory(string source, string destination, HashSet<string> excludeFiles = null)
        {
            source = Path.GetFullPath(source);
            if (!Directory.Exists(source)) return;
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (excludeFiles != null && excludeFiles.Contains(Path.GetFileName(file))) continue;
                string relative = file.Substring(source.Length + 1);
                string dest = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(file, dest, true);
            }
        }
    }
}