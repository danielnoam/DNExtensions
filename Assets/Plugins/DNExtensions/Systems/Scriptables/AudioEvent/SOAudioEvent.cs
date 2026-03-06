using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// ScriptableObject for configurable audio playback with randomization and 3D audio settings.
    /// </summary>
    [CreateAssetMenu(fileName = "New AudioEvent", menuName = "Scriptables/Audio Event")]
    public class SOAudioEvent : ScriptableObject
    {
        [Header("Settings")] 
        public AudioClip[] clips;
        public AudioMixerGroup mixerGroup;
        [MinMaxRange(0f, 1f)] public RangedFloat volume = new RangedFloat(1,1);
        [MinMaxRange(-3f, 3f)] public RangedFloat pitch = 1f;
        [Range(-1f, 1f), Tooltip("Left,Right")] public float stereoPan;
        [Range(0f, 1f), Tooltip("2D,3D")] public float spatialBlend;
        [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;
        public bool loop;
        public bool set3DSettings;
        [EnableIf("set3DSettings")] [MinMaxRange(0f, 5f)] public float dopplerLevel = 1f;
        [EnableIf("set3DSettings")] [MinMaxRange(0f, 360f)] public float spread;
        [EnableIf("set3DSettings")] public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [EnableIf("set3DSettings")] [Min(0)] public float minDistance = 1f;
        [EnableIf("set3DSettings")] [Min(0)] public float maxDistance = 500f;

        private void SetAudioSourceSettings(AudioSource source)
        {
            if (!source) return;

            source.clip = clips[Random.Range(0, clips.Length)];
            source.outputAudioMixerGroup = mixerGroup;
            source.volume = Random.Range(volume.minValue, volume.maxValue);
            source.pitch = Random.Range(pitch.minValue, pitch.maxValue);
            source.panStereo = stereoPan;
            source.spatialBlend = spatialBlend;
            source.reverbZoneMix = reverbZoneMix;
            source.bypassEffects = bypassEffects;
            source.bypassListenerEffects = bypassListenerEffects;
            source.bypassReverbZones = bypassReverbZones;
            source.loop = loop;
            if (set3DSettings)
            {
                source.dopplerLevel = dopplerLevel;
                source.spread = spread;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.rolloffMode = rolloffMode;
            }
        }
        
        /// <summary>
        /// Play audio from this event through the provided AudioSource.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="delay"></param>
        public void Play(AudioSource source, float delay = 0f)
        {
            if (!source || !source.enabled) return;
            if (clips.Length == 0) { Debug.Log("No clips found"); return; }

            SetAudioSourceSettings(source);
    
            if (delay > 0f)
            {
                source.PlayDelayed(delay);
            }
            else
            {
                source.Play();
            }
        }
        
        

        /// <summary>
        /// Play audio from this event at a point in space.
        /// The audio will be destroyed after playing.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="delay"></param>
        public void PlayAtPoint(Vector3 position = new(), float delay = 0)
        {
            if (clips.Length == 0) { Debug.Log("No clips found"); return; }

            AudioSource source = new GameObject("OneShotAudioEvent").AddComponent<AudioSource>();
            source.transform.position = position;
            
            SetAudioSourceSettings(source);
            
            if (delay > 0f)
            {
                source.PlayDelayed(delay);
                Destroy(source.gameObject, source.clip.length + delay);
            }
            else
            {
                source.Play();
                Destroy(source.gameObject, source.clip.length);
            }
        }
        

        /// <summary>
        /// Play audio from this event through the provided AudioSource.
        /// The audio must be from this event for it to stop.
        /// </summary>
        /// <param name="source"></param>
        public void Stop(AudioSource source)
        {
            if (!source) return;

            foreach (var clip in clips)
            {
                if (source.clip == clip)
                {
                    source.Stop();
                }
            }
        }

        /// <summary>
        /// Pause audio from this event through the provided AudioSource.
        /// The audio must be from this event for it to pause.
        /// </summary>
        /// <param name="source"></param>
        public void Pause(AudioSource source)
        {
            if (!source) return;

            foreach (var clip in clips)
            {
                if (source.clip == clip)
                {
                    source.Pause();
                }
            }
        }

        /// <summary>
        /// Unpause audio from this event through the provided AudioSource.
        /// The audio must be from this event for it to continue.
        /// </summary>
        /// <param name="source"></param>
        public void Continue(AudioSource source)
        {
            if (!source) return;

            foreach (var clip in clips)
            {
                if (source.clip == clip)
                {
                    source.UnPause();
                }
            }
        }

        /// <summary>
        /// Check if the provided AudioSource is playing audio from this event.
        /// The audio must be from this event for it to return true.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public bool IsPlaying(AudioSource source)
        {
            if (!source) return false;

            foreach (var clip in clips)
            {
                if (source.clip != clip) continue;

                if (source.isPlaying)
                {
                    return true;
                }
            }

            return false;
        }
    }
    
    public static class AudioSourceExtensions
    {
        /// <summary>
        /// Play audio from an SOAudioEvent through this AudioSource.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="audioEvent"></param>
        /// <param name="delay"></param>
        public static void Play(this AudioSource source, SOAudioEvent audioEvent, float delay = 0)
        {
            audioEvent.Play(source, delay);
        }
    }
}