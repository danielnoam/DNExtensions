using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DNExtensions.Utilities.AudioPreview
{
    /// <summary>
    /// Double-click any AudioClip in the Project window to preview it.
    /// Double-click again to restart playback.
    /// </summary>
    internal static class AudioPreviewTool
    {
        private static readonly MethodInfo PlayClipMethod;
        private static readonly MethodInfo StopClipsMethod;

        static AudioPreviewTool()
        {
            var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilType == null) return;

            PlayClipMethod = audioUtilType.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public);

            StopClipsMethod = audioUtilType.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            // OnOpenAsset only supplies a raw instance ID; there is no non-obsolete conversion to EntityId.
#pragma warning disable CS0618 // Type or member is obsolete
            var obj = EditorUtility.EntityIdToObject(instanceID);
#pragma warning restore CS0618
            if (obj is not AudioClip clip) return false;
            if (PlayClipMethod == null || StopClipsMethod == null) return false;

            StopClipsMethod.Invoke(null, null);
            PlayClipMethod.Invoke(null, new object[] { clip, 0, false });

            return true;
        }
    }
}