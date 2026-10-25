using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;

namespace DNExtensions.BuildTool
{
    /// <summary>
    /// Pushes builds to itch.io with butler, one channel per target. butler only sends what changed since the last
    /// push to a channel, and tags the build with the version.
    /// </summary>
    internal static class ItchUpload
    {
        public const string ApiKeyEnvironmentVariable = "BUTLER_API_KEY";

        /// <summary>
        /// The butler set in Settings, the one the itch app keeps up to date, or butler on PATH, in that order. An editor
        /// that was already open when butler was added to PATH still has the old PATH, hence the itch app's copy.
        /// </summary>
        public static string Executable
        {
            get
            {
                string configured = BuildMachineSettings.ButlerPath;
                if (!string.IsNullOrWhiteSpace(configured)) return configured;

                string broth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "itch", "broth", "butler");
                string chosen = Path.Combine(broth, "chosen-version");
                if (File.Exists(chosen))
                {
                    string bundled = Path.Combine(broth, "versions", File.ReadAllText(chosen).Trim(), "butler.exe");
                    if (File.Exists(bundled)) return bundled;
                }

                return "butler";
            }
        }

        private static (string name, string value)[] ButlerEnvironment =>
            string.IsNullOrEmpty(BuildMachineSettings.ItchApiKey)
                ? Array.Empty<(string, string)>()
                : new[] { (ApiKeyEnvironmentVariable, BuildMachineSettings.ItchApiKey) };

        public static BuildProcess.Result Run(string arguments, int timeoutSeconds = 60) => BuildProcess.Run(Executable, arguments, timeoutSeconds, ButlerEnvironment);

        public static string Channel(SOBuildConfig config, SOBuildConfig.TargetKind kind)
        {
            string channel = kind == SOBuildConfig.TargetKind.AndroidApk ? config.itchAndroidChannel : config.itchWindowsChannel;
            return string.IsNullOrWhiteSpace(channel) ? kind.ToString().ToLowerInvariant() : channel.Trim();
        }

        public static void AddIssues(List<BuildRunner.Issue> issues, SOBuildConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.itchProject) || config.itchProject.Count(c => c == '/') != 1)
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "itch.io project must be user/game. Set it in Settings."));
                return;
            }

            var version = Run("version", 15);
            if (!version.Succeeded)
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "butler not found. Install the itch app, or download butler from itch.io/docs/butler, then set its path in Settings if it is not on PATH."));
                return;
            }

            // status reads the project's channels, which needs a working login and access to the game
            var status = Run($"status {BuildProcess.Quote(config.itchProject.Trim())}", 60);
            if (!status.Succeeded)
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, $"butler can't reach {config.itchProject}: {FirstLine(status.Message)} Run butler login, or set an API key in Settings."));
            }
        }

        public static BuildRunner.StepResult Upload(SOBuildConfig config, IReadOnlyDictionary<SOBuildConfig.TargetKind, string> built)
        {
            var step = new BuildRunner.StepResult { Name = "itch.io" };
            var timer = Stopwatch.StartNew();
            var pushed = new List<string>();

            try
            {
                foreach (var (kind, path) in built)
                {
                    string target = $"{config.itchProject.Trim()}:{Channel(config, kind)}";
                    EditorUtility.DisplayProgressBar("Build", $"Pushing {kind} to {target}", 1f);

                    var push = Run($"push {BuildProcess.Quote(path)} {BuildProcess.Quote(target)} --userversion {BuildProcess.Quote(BuildRunner.Version)}", 30 * 60);
                    if (!push.Succeeded)
                    {
                        step.Succeeded = false;
                        step.Message = $"{target}: {push.Message}";
                        return step;
                    }

                    pushed.Add(target);
                }

                step.Succeeded = true;
                step.Message = $"Pushed {BuildRunner.Version} to {string.Join(", ", pushed)}";
                return step;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                step.Seconds = timer.Elapsed.TotalSeconds;
            }
        }

        private static string FirstLine(string text) => (text ?? string.Empty).Split('\n').Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0) ?? string.Empty;
    }

    /// <summary>
    /// Uploads the Windows build to Steam with SteamPipe: writes an app build script into Temp and runs it through
    /// steamcmd, logged in as the Steam user from Settings with the login steamcmd cached.
    /// </summary>
    internal static class SteamUpload
    {
        public static string Executable => string.IsNullOrWhiteSpace(BuildMachineSettings.SteamCmdPath) ? "steamcmd" : BuildMachineSettings.SteamCmdPath;

        private static string Folder => Path.Combine(BuildProcess.ProjectFolder, "Temp", "BuildTool", "Steam");

        public static void AddIssues(List<BuildRunner.Issue> issues, SOBuildConfig config)
        {
            if (config.steamAppId <= 0 || config.steamWindowsDepotId <= 0)
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "Steam needs an App ID and a Windows depot ID. Set them in Settings."));
            }

            if (!config.targets.Any(target => target.enabled && target.kind == SOBuildConfig.TargetKind.Windows))
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "Steam upload is on, but the Windows target isn't enabled."));
            }

            if (string.IsNullOrWhiteSpace(BuildMachineSettings.SteamUser))
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "No Steam user on this computer. Set it in Settings."));
            }

            if (Path.IsPathRooted(Executable) ? !File.Exists(Executable) : !OnPath(Executable))
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "steamcmd not found. Download it from Valve's developer site, then set its path in Settings."));
            }

            else if (!string.IsNullOrWhiteSpace(BuildMachineSettings.SteamUser))
            {
                // steamcmd updates itself on its first run and only logs in without asking when the login is cached
                var login = BuildProcess.Run(Executable, $"+login {BuildProcess.Quote(BuildMachineSettings.SteamUser)} +quit", 300);
                if (!(login.Output + login.Error).Contains("Waiting for user info...OK"))
                {
                    issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, $"steamcmd isn't logged in as {BuildMachineSettings.SteamUser}. Log in once by hand in a terminal, with Steam Guard: steamcmd +login {BuildMachineSettings.SteamUser}"));
                }
            }

            if (string.Equals(config.steamSetLiveBranch?.Trim(), "default", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new BuildRunner.Issue(BuildRunner.Severity.Error, "Steam doesn't let a build script set the default branch live. Leave Set Live empty and set it live on the Steamworks site."));
            }
        }

        public static BuildRunner.StepResult Upload(SOBuildConfig config, IReadOnlyDictionary<SOBuildConfig.TargetKind, string> built)
        {
            var step = new BuildRunner.StepResult { Name = "Steam" };
            var timer = Stopwatch.StartNew();

            try
            {
                if (!built.TryGetValue(SOBuildConfig.TargetKind.Windows, out string windows))
                {
                    step.Succeeded = false;
                    step.Message = "No Windows build to upload.";
                    return step;
                }

                EditorUtility.DisplayProgressBar("Build", $"Uploading {BuildRunner.Version} to Steam", 1f);

                string content = ContentFolder(windows);
                string script = WriteAppBuild(config, content);

                // steamcmd asks for nothing when the login is cached; a missing one fails instead of waiting for input
                var run = BuildProcess.Run(Executable, $"+login {BuildProcess.Quote(BuildMachineSettings.SteamUser)} +run_app_build {BuildProcess.Quote(script)} +quit", 60 * 60);
                string output = run.Output + run.Error;

                // steamcmd's exit code is not reliable, its own success line is
                step.Succeeded = output.Contains("Successfully finished AppID");
                step.Message = step.Succeeded ? SuccessLine(output) : $"{LastLines(output)}\nIf it asks for a password or Steam Guard, log in once by hand: steamcmd +login {BuildMachineSettings.SteamUser}";
                return step;
            }
            catch (Exception e)
            {
                step.Succeeded = false;
                step.Message = e.Message;
                return step;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                step.Seconds = timer.Elapsed.TotalSeconds;
            }
        }

        /// <summary>The player folder to upload: the build's own folder, or the zip unpacked, since Steam wants the files.</summary>
        private static string ContentFolder(string artifact)
        {
            if (!artifact.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return Path.GetDirectoryName(artifact);

            string folder = Path.Combine(Folder, "Content");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            ZipFile.ExtractToDirectory(artifact, folder);

            // The zip holds one top folder named after the game, which is what goes in the depot root
            var top = Directory.GetDirectories(folder);
            return top.Length == 1 && Directory.GetFiles(folder).Length == 0 ? top[0] : folder;
        }

        private static string WriteAppBuild(SOBuildConfig config, string content)
        {
            Directory.CreateDirectory(Path.Combine(Folder, "Output"));

            var vdf = new StringBuilder();
            vdf.AppendLine("\"AppBuild\"");
            vdf.AppendLine("{");
            vdf.AppendLine($"\t\"AppID\" \"{config.steamAppId}\"");
            vdf.AppendLine($"\t\"Desc\" \"{config.ResolvedFileName} {BuildRunner.Version}\"");
            if (!string.IsNullOrWhiteSpace(config.steamSetLiveBranch)) vdf.AppendLine($"\t\"SetLive\" \"{config.steamSetLiveBranch.Trim()}\"");
            vdf.AppendLine($"\t\"ContentRoot\" \"{VdfPath(content)}\"");
            vdf.AppendLine($"\t\"BuildOutput\" \"{VdfPath(Path.Combine(Folder, "Output"))}\"");
            vdf.AppendLine("\t\"Depots\"");
            vdf.AppendLine("\t{");
            vdf.AppendLine($"\t\t\"{config.steamWindowsDepotId}\"");
            vdf.AppendLine("\t\t{");
            vdf.AppendLine("\t\t\t\"FileMapping\"");
            vdf.AppendLine("\t\t\t{");
            vdf.AppendLine("\t\t\t\t\"LocalPath\" \"*\"");
            vdf.AppendLine("\t\t\t\t\"DepotPath\" \".\"");
            vdf.AppendLine("\t\t\t\t\"recursive\" \"1\"");
            vdf.AppendLine("\t\t\t}");
            vdf.AppendLine("\t\t}");
            vdf.AppendLine("\t}");
            vdf.AppendLine("}");

            string path = Path.Combine(Folder, $"app_build_{config.steamAppId}.vdf");
            File.WriteAllText(path, vdf.ToString());
            return path;
        }

        private static string VdfPath(string path) => Path.GetFullPath(path).Replace('\\', '/');

        private static bool OnPath(string executable)
        {
            foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                if (File.Exists(Path.Combine(folder, executable)) || File.Exists(Path.Combine(folder, executable + ".exe"))) return true;
            }

            return false;
        }

        private static string SuccessLine(string output) => output.Split('\n').Select(line => line.Trim()).LastOrDefault(line => line.Contains("Successfully finished AppID")) ?? "Uploaded.";

        private static string LastLines(string output)
        {
            var lines = output.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
            return string.Join("\n", lines.Skip(Math.Max(0, lines.Count - 4)));
        }
    }
}
