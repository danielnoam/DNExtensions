using DNExtensions.Utilities;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    [System.Serializable]
    public struct AudioSettings
    {
        public AudioClip clip;
        public float volume;
        public float pitch;
        public float stereoPan;
        public float spatialBlend;
        public float reverbZoneMix;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;
        public bool loop;
        public bool set3DSettings;
        public float dopplerLevel;
        public float spread;
        public AudioRolloffMode rolloffMode;
        public float minDistance;
        public float maxDistance;
    }

    
    /// <summary>
    /// A ScriptableObject that represents an audio profile, which contains settings for how a sound should be played.
    /// It includes a list of audio clips to choose from, as well as various settings for volume, pitch, spatialization, and more.
    /// </summary>
    [CreateAssetMenu(fileName = "New AudioProfile", menuName = "Scriptable Objects/Audio Profile")]
    public class SOAudioProfile : ScriptableObject
    {
        [Tooltip("The list of clips to pick from. One will be chosen at random each time the sound plays.")]
        public AudioClip[] clips;

        [Tooltip("The randomized volume range.")]
        [MinMaxRange(0f, 1f)] public RangedFloat volume = new RangedFloat(1, 1);

        [Tooltip("The randomized pitch range.")]
        [MinMaxRange(-3f, 3f)] public RangedFloat pitch = new RangedFloat(1, 1);

        [Tooltip("Panning in a stereo setup. -1 is full left, 1 is full right.")]
        [Range(-1f, 1f)] public float stereoPan;

        [Tooltip("Sets how much the 3D engine affects the source. 0 is 2D, 1 is full 3D.")]
        [Range(0f, 1f)] public float spatialBlend;

        [Tooltip("The amount of the signal that is routed to the reverb zones.")]
        [Range(0f, 1.1f)] public float reverbZoneMix = 1f;

        [Tooltip("Bypass all effects applied to the AudioSource.")]
        public bool bypassEffects;

        [Tooltip("Bypass the listener effects.")]
        public bool bypassListenerEffects;

        [Tooltip("Bypass the reverb zones.")]
        public bool bypassReverbZones;

        [Tooltip("Should the sound repeat indefinitely? (Note: Only applies if the PlaybackStrategy is Managed).")]
        public bool loop;

        [Tooltip("Enable to reveal and apply custom 3D spatialization settings.")]
        public bool set3DSettings;

        [Tooltip("How much the pitch changes based on the relative velocity between source and listener.")]
        public float dopplerLevel = 1f;

        [Tooltip("The spread angle in degrees for 3D stereo or multichannel sound.")]
        public float spread;

        [Tooltip("How the volume fades over distance.")]
        public float minDistance = 1f;

        [Tooltip("The distance where the sound is no longer audible.")]
        public float maxDistance = 500f;

        [Tooltip("The rolloff curve type.")]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        public AudioSettings GetSettings()
        {
            return new AudioSettings
            {
                clip = clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null,
                volume = volume.RandomValue,
                pitch = pitch.RandomValue,
                stereoPan = stereoPan,
                spatialBlend = spatialBlend,
                reverbZoneMix = reverbZoneMix,
                bypassEffects = bypassEffects,
                bypassListenerEffects = bypassListenerEffects,
                bypassReverbZones = bypassReverbZones,
                loop = loop,
                set3DSettings = set3DSettings,
                dopplerLevel = dopplerLevel,
                spread = spread,
                rolloffMode = rolloffMode,
                minDistance = minDistance,
                maxDistance = maxDistance
            };
        }
    }
}