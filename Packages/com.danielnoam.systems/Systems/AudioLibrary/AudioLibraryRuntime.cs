using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Automatically initializes the Audio Library at runtime.
    /// </summary>
    internal static class AudioLibraryRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (AudioLibrary.Instance) return;
            
            SOAudioLibrarySettings settings = SOAudioLibrarySettings.Instance;
            
            if (!settings || !settings.Enabled) return;
            
            GameObject go = new GameObject("AudioLibraryManager");
            AudioLibrary audioLibrary = go.AddComponent<AudioLibrary>();
            audioLibrary.Initialize(settings);
        }
    }
}