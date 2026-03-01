using System;
using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace DNExtensions.Systems.AudioTrack
{
    /// <summary>
    /// Defines a single music track (stem) that plays as part of the layered music system.
    /// All tracks run simultaneously and are controlled via volume fades.
    /// </summary>
    [Serializable]
    public struct TrackDefinition
    {
        public string id;
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>
    /// Settings asset for the Audio Track system.
    /// Defines all music tracks available at runtime.
    /// Must be placed in a Resources folder for runtime access.
    /// </summary>
    [UniqueSO]
    public class SOAudioTrackSettings : ScriptableObject
    {
        private static SOAudioTrackSettings _instance;

        public static SOAudioTrackSettings Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = Resources.Load<SOAudioTrackSettings>("AudioTrackSettings");

#if UNITY_EDITOR
                if (!_instance)
                    Debug.LogWarning("AudioTrackSettings not found in Resources folder. Create one via Tools > DNExtensions > Audio Track Settings");
#endif
                return _instance;
            }
        }
        [Tooltip("Enables the system")]
        [SerializeField] private bool enabled;
        [SerializeField] private TrackDefinition[] tracks = Array.Empty<TrackDefinition>();

        public bool Enabled => enabled;
        public TrackDefinition[] Tracks => tracks;
    }
}