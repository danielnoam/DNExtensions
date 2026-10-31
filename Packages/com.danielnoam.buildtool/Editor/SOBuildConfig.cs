using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace DNExtensions.BuildTool
{
    /// <summary>
    /// Everything the build window runs for this project: which Build Profiles to build, how the files are named, what
    /// to check first and where to upload. Lives in the project so the setup is shared, but holds no secrets; per-machine
    /// settings are in <see cref="BuildMachineSettings"/> and GitHub authentication comes from the gh CLI.
    /// </summary>
    public class SOBuildConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/Settings/Build/BuildConfig.asset";

        public enum ReleaseType
        {
            Draft,
            Prerelease,
            Published
        }

        public enum TargetKind
        {
            AndroidApk,
            Windows
        }

        [Serializable]
        public class Target
        {
            public bool enabled = true;
            public TargetKind kind;
            public BuildProfile profile;
            public bool developmentBuild;
        }

        public List<Target> targets = new List<Target>
        {
            new Target { kind = TargetKind.AndroidApk },
            new Target { kind = TargetKind.Windows }
        };

        [Tooltip("Names the executable, the APK, the zip and the release title, e.g. MyGame-1.2.3.apk. Empty uses the product name without spaces")]
        public string fileName = string.Empty;

        [Tooltip("The project default, relative to the project folder. Each machine can pick its own folder in Settings. Each run goes into <folder>/<version>/")]
        public string outputRoot = "Builds";
        [Tooltip("Zip the Windows player folder into a single file, which is what gets uploaded, and delete the unzipped folder")]
        public bool zipWindowsBuild = true;

        [Tooltip("Run the project's own [BuildCheck] methods before building, and refuse to build if any reports a problem")]
        [UnityEngine.Serialization.FormerlySerializedAs("requireValidLevels")]
        public bool runProjectChecks = true;

        [Tooltip("Pins the Android target API level so it can't change between machines or SDK updates. 0 leaves it to Player Settings")]
        [Min(0)] public int androidTargetApi;

        public bool uploadToGitHub;
        [Tooltip("owner/name of the repository the releases go to. Empty uses this checkout's origin remote. Another repository gets the release on its default branch, and the version tag goes to origin")]
        public string githubRepository = string.Empty;
        [Tooltip("Draft: only you can see it, to test before publishing. Prerelease: public but marked as a test build. " +
                 "Published: public.")]
        public ReleaseType githubReleaseType = ReleaseType.Draft;
        [Tooltip("If this version was already released, replace its files and move its tag to the current commit instead of stopping. " +
                 "It stays draft or published as it was. Players already on this version are not offered the new build.")]
        public bool replaceExistingRelease;

        public bool copyToFolder;

        public bool uploadToItch;
        [Tooltip("user/game, as in the game's itch.io address user.itch.io/game")]
        public string itchProject = string.Empty;
        [Tooltip("The itch.io channel for each target. A channel ending in a platform name (windows, android) tags the upload with that platform")]
        public string itchWindowsChannel = "windows";
        public string itchAndroidChannel = "android";

        public bool uploadToSteam;
        [Tooltip("The game's App ID in Steamworks")]
        [Min(0)] public int steamAppId;
        [Tooltip("The depot that receives the Windows build. Android builds are not uploaded to Steam")]
        [Min(0)] public int steamWindowsDepotId;
        [Tooltip("The branch the new build goes live on. Empty only uploads it; default can't be set from here, Steamworks asks for that to be done on the site")]
        public string steamSetLiveBranch = string.Empty;

        /// <summary>The name builds and releases go by: <see cref="fileName"/>, or the product name with only safe characters.</summary>
        public string ResolvedFileName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(fileName) ? PlayerSettings.productName : fileName;
                var safe = new System.Text.StringBuilder();
                foreach (char c in name ?? string.Empty)
                {
                    if (char.IsLetterOrDigit(c) || c == '-' || c == '_') safe.Append(c);
                }

                return safe.Length > 0 ? safe.ToString() : "Game";
            }
        }

        public static SOBuildConfig LoadOrCreate()
        {
            var config = AssetDatabase.LoadAssetAtPath<SOBuildConfig>(AssetPath);
            if (config) return config;

            EnsureFolder("Assets/Settings/Build");
            config = CreateInstance<SOBuildConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
