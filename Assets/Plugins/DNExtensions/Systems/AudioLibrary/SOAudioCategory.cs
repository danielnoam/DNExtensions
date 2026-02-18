using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// A struct that represents a mapping between a string ID and an audio object (AudioClip or AudioMixerGroup).
    /// </summary>
    [Serializable]
    public struct AudioMapping
    {
        public string id;
        public UnityEngine.Object audioObject;
    }
    
    
    /// <summary>
    /// A ScriptableObject that represents a category of audio, such as "Music", "SFX", "Voice", etc.
    /// It contains a reference to an AudioMixerGroup and a list of AudioMappings that map string IDs to audio objects.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCategory", menuName = "Scriptable Objects/Audio Category")]
    public class SOAudioCategory : ScriptableObject
    {
        
        [SerializeField] private string label = "New Category";
        [SerializeField] private AudioMixerGroup audioMixerGroup;
        [SerializeField] private AudioMapping[] audioMappings = Array.Empty<AudioMapping>();
        
        
        public string Label => label;
        public AudioMixerGroup AudioMixerGroup => audioMixerGroup;
        public AudioMapping[] AudioMappings => audioMappings;
    }
}
