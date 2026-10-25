using System.IO;
using UnityEditor;

namespace DNExtensions.BuildTool
{
    /// <summary>
    /// Build settings that belong to one computer rather than the project, such as a Google Drive folder, which differs
    /// by user name and drive. Kept in EditorPrefs under this project's GUID, so each machine and project has its own
    /// and none of them land in git.
    /// </summary>
    internal static class BuildMachineSettings
    {
        /// <summary>An EditorPrefs key for this project. EditorPrefs are shared by every project on the machine.</summary>
        public static string Key(string name) => $"BuildTool.{PlayerSettings.productGUID}.{name}";

        /// <summary>Where builds go on this machine. Empty means the project default in <see cref="SOBuildConfig.outputRoot"/>.</summary>
        public static string OutputFolderOverride
        {
            get => EditorPrefs.GetString(Key("OutputFolder"), string.Empty);
            set => EditorPrefs.SetString(Key("OutputFolder"), value ?? string.Empty);
        }

        public static string CopyFolder
        {
            get => EditorPrefs.GetString(Key("CopyFolder"), string.Empty);
            set => EditorPrefs.SetString(Key("CopyFolder"), value ?? string.Empty);
        }

        /// <summary>butler's full path. Empty looks for butler on PATH and in the itch app's install folder.</summary>
        public static string ButlerPath
        {
            get => EditorPrefs.GetString(Key("ButlerPath"), string.Empty);
            set => EditorPrefs.SetString(Key("ButlerPath"), value ?? string.Empty);
        }

        /// <summary>
        /// The itch.io API key butler pushes with, saved encrypted for this Windows user. Empty uses the login from
        /// <c>butler login</c>, or the BUTLER_API_KEY environment variable.
        /// </summary>
        public static string ItchApiKey
        {
            get => GetSecret("ItchApiKey");
            set => SetSecret("ItchApiKey", value);
        }

        /// <summary>steamcmd's full path. Empty looks for steamcmd on PATH.</summary>
        public static string SteamCmdPath
        {
            get => EditorPrefs.GetString(Key("SteamCmdPath"), string.Empty);
            set => EditorPrefs.SetString(Key("SteamCmdPath"), value ?? string.Empty);
        }

        /// <summary>
        /// The Steam account that uploads. No password is kept: steamcmd caches the login after it is done once by hand,
        /// with Steam Guard, in a terminal.
        /// </summary>
        public static string SteamUser
        {
            get => EditorPrefs.GetString(Key("SteamUser"), string.Empty);
            set => EditorPrefs.SetString(Key("SteamUser"), value ?? string.Empty);
        }

        /// <summary>A value kept in EditorPrefs encrypted with Windows DPAPI, so only this Windows user can read it back.</summary>
        public static string GetSecret(string name)
        {
            string stored = EditorPrefs.GetString(Key(name), string.Empty);
            if (string.IsNullOrEmpty(stored)) return string.Empty;

            try
            {
                return System.Text.Encoding.UTF8.GetString(WindowsDataProtection.Unprotect(System.Convert.FromBase64String(stored)));
            }
            catch (System.Exception)
            {
                // Saved by another Windows user or a reinstalled system, it cannot be decrypted here
                return string.Empty;
            }
        }

        public static void SetSecret(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                EditorPrefs.DeleteKey(Key(name));
                return;
            }

            EditorPrefs.SetString(Key(name), System.Convert.ToBase64String(WindowsDataProtection.Protect(System.Text.Encoding.UTF8.GetBytes(value))));
        }

        /// <summary>The build folder in effect: this machine's choice, or the project default, relative to the project folder.</summary>
        public static string ResolveOutputRoot(SOBuildConfig config)
        {
            string folder = string.IsNullOrWhiteSpace(OutputFolderOverride) ? config.outputRoot : OutputFolderOverride;
            return Path.GetFullPath(Path.Combine(BuildProcess.ProjectFolder, string.IsNullOrWhiteSpace(folder) ? "Builds" : folder));
        }

        /// <summary>
        /// An absolute build folder saved in the shared config only works on the machine that picked it, so it becomes
        /// this machine's choice and the config goes back to the project default.
        /// </summary>
        public static void MigrateFromConfig(SOBuildConfig config)
        {
            if (!Path.IsPathRooted(config.outputRoot ?? string.Empty)) return;

            if (string.IsNullOrWhiteSpace(OutputFolderOverride)) OutputFolderOverride = config.outputRoot;
            config.outputRoot = "Builds";
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }
    }
}
