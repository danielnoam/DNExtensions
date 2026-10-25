using UnityEditor;
using UnityEngine;

namespace DNExtensions.BuildTool
{
    /// <summary>
    /// Pins the Android target API level to the build config's <see cref="SOBuildConfig.androidTargetApi"/>. Left on
    /// Automatic, Unity targets the highest SDK installed on whichever machine builds, so the target can change silently
    /// between machines or after an SDK update, and Google Play rejects uploads below its current minimum. Reapplied on
    /// every script reload, so switching it back in Player Settings does not stick; change it in the build window instead.
    /// </summary>
    [InitializeOnLoad]
    internal static class AndroidSdkPin
    {
        static AndroidSdkPin()
        {
            // Deferred, player settings are not reliably writable while the domain is still loading
            EditorApplication.delayCall += Apply;
        }

        public static void Apply()
        {
            var config = AssetDatabase.LoadAssetAtPath<SOBuildConfig>(SOBuildConfig.AssetPath);
            if (!config || config.androidTargetApi <= 0) return;

            var target = (AndroidSdkVersions)config.androidTargetApi;
            if (PlayerSettings.Android.targetSdkVersion == target) return;

            var previous = PlayerSettings.Android.targetSdkVersion;
            PlayerSettings.Android.targetSdkVersion = target;
            AssetDatabase.SaveAssets();

            Debug.Log($"[Build] Android target API pinned to {config.androidTargetApi} (was {(previous == AndroidSdkVersions.AndroidApiLevelAuto ? "Automatic" : ((int)previous).ToString())}).");
        }
    }
}
