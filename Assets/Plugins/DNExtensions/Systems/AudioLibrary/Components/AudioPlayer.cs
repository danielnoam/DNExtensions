using DNExtensions.Utilities.AutoGet;
using DNExtensions.Utilities.Button;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Plays a selected audio from the audio library on the audio source.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("DNExtensions/Audio Library/Audio Player")]
    public class AudioPlayer : MonoBehaviour
    {
        
        [SerializeField, AudioID] private string audioID = string.Empty;
        [SerializeField] private bool playOnStart;
        
        
        [SerializeField, AutoGetSelf, HideInInspector] private AudioSource audioSource;


        private void Start()
        {
            if (playOnStart) PlayAudio();
        }

        /// <summary>
        /// Plays the audio on the audio source.
        /// </summary>
        [Button]
        public void PlayAudio()
        {
            if (string.IsNullOrEmpty(audioID)) return;
            AudioLibrary.PlayOnSource(audioID, audioSource);
        }
        
        
        /// <summary>
        /// Stops the audio from playing.
        /// </summary>
        [Button]
        public void StopAudio()
        {
            audioSource.Stop();
        }
        

    }
}