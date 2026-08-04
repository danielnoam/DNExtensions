using UnityEngine;

namespace DNExtensions.Systems.AudioTrack
{
    /// <summary>
    /// Automatically initializes the Audio Track system at runtime.
    /// </summary>
    internal static class AudioTrackRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (AudioTrack.Instance) return;

            SOAudioTrackSettings settings = SOAudioTrackSettings.Instance;

            if (!settings || !settings.Enabled) return;

            GameObject go = new GameObject("AudioTrackManager");
            AudioTrack audioTrack = go.AddComponent<AudioTrack>();
            audioTrack.Initialize(settings);
        }
    }
}