using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.Systems.AudioTrack
{
    /// <summary>
    /// Manages layered music playback by controlling the volume of simultaneously running audio tracks.
    /// All tracks are loaded and started on initialization at volume 0.
    /// Use Play and Stop to fade tracks in and out.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class AudioTrack : MonoBehaviour
    {
        public static AudioTrack Instance { get; private set; }

        private readonly Dictionary<string, RuntimeTrack> _tracks = new();

        private class RuntimeTrack
        {
            public AudioSource Source;
            public Coroutine FadeCoroutine;
            public float TargetVolume;
        }

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initializes all tracks from the settings asset.
        /// Each track starts playing immediately at volume 0.
        /// </summary>
        public void Initialize(SOAudioTrackSettings settings)
        {
            foreach (var definition in settings.Tracks)
            {
                if (string.IsNullOrEmpty(definition.id) || !definition.clip) continue;

                GameObject go = new GameObject($"Track_{definition.id}");
                go.transform.SetParent(transform);

                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = definition.clip;
                source.outputAudioMixerGroup = definition.mixerGroup;
                source.loop = true;
                source.volume = 0f;
                source.Play();

                _tracks[definition.id] = new RuntimeTrack { Source = source, TargetVolume = definition.volume };
            }
        }

        /// <summary>
        /// Fades in a track by ID.
        /// </summary>
        /// <param name="id">Track ID as defined in SOAudioTrackSettings.</param>
        /// <param name="fadeDuration">Duration of the fade in seconds.</param>
        public static void Play(string id, float fadeDuration = 1f)
        {
            if (!Instance || !Instance.TryGetTrack(id, out var track)) return;
            Instance.StartFade(track, track.TargetVolume, fadeDuration);
        }

        /// <summary>
        /// Fades out one track and in another sequentially or simultaneously.
        /// </summary>
        /// <param name="trackOutID">Track ID to fade out.</param>
        /// <param name="trackInID">Track ID to fade in.</param>
        /// <param name="fadeDuration">Duration of the fade in seconds.</param>
        /// <param name="crossfade">If true, both tracks will be faded simultaneously. Otherwise, sequential fade out/fade in.</param>
        public static void Transition(string trackOutID, string trackInID, float fadeDuration = 1f, bool crossfade = true)
        {
            if (!Instance || !Instance.TryGetTrack(trackOutID, out var trackOut) || !Instance.TryGetTrack(trackInID, out var trackIn)) return;

            if (crossfade)
            {
                Instance.StartFade(trackOut, 0f, fadeDuration);
                Instance.StartFade(trackIn, trackIn.TargetVolume, fadeDuration);
            }
            else
            {
                Instance.StartCoroutine(Instance.SequentialSwapRoutine(trackOut, trackIn, fadeDuration));
            }
            
        }

        /// <summary>
        /// Fades out a track by ID.
        /// </summary>
        /// <param name="id">Track ID as defined in SOAudioTrackSettings.</param>
        /// <param name="fadeDuration">Duration of the fade in seconds.</param>
        public static void Stop(string id, float fadeDuration = 1f)
        {
            if (!Instance || !Instance.TryGetTrack(id, out var track)) return;
            Instance.StartFade(track, 0, fadeDuration);
        }

        /// <summary>
        /// Fades out all tracks.
        /// </summary>
        /// <param name="fadeDuration">Duration of the fade in seconds.</param>
        public static void StopAll(float fadeDuration = 1f)
        {
            if (!Instance) return;
            foreach (var track in Instance._tracks.Values)
            {
                Instance.StartFade(track, 0, fadeDuration);
            }
        }

        private bool TryGetTrack(string id, out RuntimeTrack track)
        {
            if (_tracks.TryGetValue(id, out track)) return true;
            Debug.LogWarning($"AudioTrack: ID '{id}' not found.");
            return false;
        }

        private void StartFade(RuntimeTrack track, float targetVolume, float duration)
        {
            if (track.FadeCoroutine != null)
                StopCoroutine(track.FadeCoroutine);

            track.FadeCoroutine = StartCoroutine(FadeRoutine(track, targetVolume, duration));
        }

        private IEnumerator FadeRoutine(RuntimeTrack track, float targetVolume, float duration)
        {
            float startVolume = track.Source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                track.Source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            track.Source.volume = targetVolume;
            track.FadeCoroutine = null;
        }
        
        private IEnumerator SequentialSwapRoutine(RuntimeTrack trackOut, RuntimeTrack trackIn, float fadeDuration)
        {
            yield return FadeRoutine(trackOut, 0f, fadeDuration);
            yield return FadeRoutine(trackIn, trackIn.TargetVolume, fadeDuration);
        }
    }
}