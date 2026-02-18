using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Automatically initializes the Audio Library at runtime.
    /// </summary>
    public static class AudioLibraryManagerRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (AudioLibrary.Instance) return;
            
            SOAudioLibrarySettings settings = SOAudioLibrarySettings.Instance;
            
            if (!settings)
            {
                // Debug.LogError("AudioLibrarySettings not found in Resources folder! Create one via: Tools > DNExtensions > Audio Library Settings");
                return;
            }

            if (!settings.Enabled)
            {
                return;
            }
            
            GameObject go = new GameObject("AudioManager");
            AudioLibrary audioLibrary = go.AddComponent<AudioLibrary>();
            audioLibrary.Initialize(settings);
        }
    }
}