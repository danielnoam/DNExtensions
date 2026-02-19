using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// A centralized audio manager that handles playing audio clips and profiles based on string IDs.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioLibrary : MonoBehaviour
    {
        public static AudioLibrary Instance { get; private set;}

        private SOAudioLibrarySettings _librarySettings;
        private int _preWarmAmount = 20;

        private readonly Dictionary<string, AudioData> _audioCache = new();
        private readonly Dictionary<string, AudioSource> _activeLoopSources = new();
        private readonly Queue<AudioSource> _pool = new();
        private int _totalCreatedSources;

        private struct AudioData
        {
            public Object AudioObject;
            public AudioMixerGroup Group;
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
        
        
        #region Audio Handling Logic

        public void Initialize(SOAudioLibrarySettings settings)
        {
            _librarySettings = settings;
            _preWarmAmount = settings.PreWarmAmount;
            InitializeCache();
            CreatePool();
        }
        
        private void InitializeCache()
        {
            if (!_librarySettings) return;
            foreach (var category in _librarySettings.AudioCategories)
            {
                if (!category) continue;
                foreach (var mapping in category.AudioMappings)
                {
                    if (string.IsNullOrEmpty(mapping.id) || !mapping.audioObject) continue;
                    
                    _audioCache[mapping.id] = new AudioData
                    {
                        AudioObject = mapping.audioObject,
                        Group = category.AudioMixerGroup
                    };
                }
            }
        }

        private bool TryGetAudioData(string audioID, out AudioData data)
        {
            if (_audioCache.TryGetValue(audioID, out data)) return true;
            Debug.LogWarning($"AudioManager: ID '{audioID}' not found.");
            return false;
        }

        
        private bool ConfigureSource(AudioSource source, AudioData data, Vector3 pos, bool usePos)
        {
            source.outputAudioMixerGroup = data.Group;
            source.transform.position = usePos ? pos : transform.position;

            if (data.AudioObject is SOAudioProfile profile)
            {
                var settings = profile.GetSettings();
                source.clip = settings.clip;
                source.volume = settings.volume;
                source.pitch = settings.pitch;
                source.spatialBlend = settings.spatialBlend;
                source.reverbZoneMix = settings.reverbZoneMix;
                source.bypassEffects = settings.bypassEffects;
                source.bypassListenerEffects = settings.bypassListenerEffects;
                source.bypassReverbZones = settings.bypassReverbZones;
                source.loop = settings.loop;

                if (settings.set3DSettings)
                {
                    source.dopplerLevel = settings.dopplerLevel;
                    source.spread = settings.spread;
                    source.minDistance = settings.minDistance;
                    source.maxDistance = settings.maxDistance;
                    source.rolloffMode = settings.rolloffMode;
                }
            }
            else if (data.AudioObject is AudioClip clip)
            {
                source.clip = clip;
                source.spatialBlend = usePos ? 1f : 0f;
                source.loop = false;
            }

            return source.clip;
        }

        private void SetupAndPlay(string id, AudioSource source, AudioData data, Vector3 pos, bool usePos)
        {
            source.gameObject.SetActive(true);

            if (!ConfigureSource(source, data, pos, usePos))
            {
                ReturnSourceToPool(source);
                return;
            }

            if (source.loop) _activeLoopSources[id] = source;

            source.Play();

            if (!source.loop)
                StartCoroutine(AutoReturnRoutine(source, source.clip.length / Mathf.Abs(source.pitch), id));
        }

        #endregion

        
        #region Pooling Logic

        private void CreatePool()
        {
            for (int i = 0; i < _preWarmAmount; i++)
            {
                _pool.Enqueue(CreateAudioSource());
            }
        }

        private AudioSource CreateAudioSource()
        {
            GameObject go = new GameObject($"PooledSource_{_totalCreatedSources++}");
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            go.SetActive(false);
            return source;
        }
        
        private AudioSource GetSourceFromPool()
        {
            return _pool.Count > 0 ? _pool.Dequeue() : CreateAudioSource();
        }

        private void ReturnSourceToPool(AudioSource source)
        {
            source.Stop();
            source.gameObject.SetActive(false);
            _pool.Enqueue(source);
        }

        private IEnumerator AutoReturnRoutine(AudioSource source, float duration, string id = null)
        {
            yield return new WaitForSeconds(duration);
            
            if (!string.IsNullOrEmpty(id))
            {
                if (_activeLoopSources.TryGetValue(id, out var current) && current == source)
                {
                    _activeLoopSources.Remove(id);
                }
            }

            ReturnSourceToPool(source);
        }
        

        #endregion
        
        
        #region Public API

        /// <summary>
        /// Plays an audio clip or profile based on the provided ID.
        /// The sound will be played at the AudioManager's position and will not be spatialized.
        /// </summary>
        /// <param name="audioID"></param>
        public static void Play(string audioID)
        {
            if (!Instance || !Instance.TryGetAudioData(audioID, out var data)) return;
            
            AudioSource source = Instance.GetSourceFromPool();
            Instance.SetupAndPlay(audioID, source, data, Vector3.zero, false);
        }
        

        /// <summary>
        /// Plays an audio clip or profile based on the provided ID at a specific world position.
        /// The sound will be spatialized based on the AudioSource settings.
        /// </summary>
        /// <param name="audioID"></param>
        /// <param name="position"></param>
        public static void PlayAtPosition(string audioID, Vector3 position)
        {
            if (!Instance || !Instance.TryGetAudioData(audioID, out var data)) return;
            
            AudioSource source = Instance.GetSourceFromPool();
            Instance.SetupAndPlay(audioID, source, data, position, true);
        }

        
        /// <summary>
        /// Plays an audio clip or profile based on the provided ID at the position of a target Transform.
        /// The sound will be spatialized based on the AudioSource settings.
        /// </summary>
        /// <param name="audioID"></param>
        /// <param name="target"></param>
        public static void PlayAtPosition(string audioID, Transform target)
        {
            if (!Instance || !Instance.TryGetAudioData(audioID, out var data)) return;
            
            AudioSource source = Instance.GetSourceFromPool();
            Instance.SetupAndPlay(audioID, source, data, target.position, true);
        }
        
        /// <summary>
        /// Plays an audio clip or profile based on the provided ID using a specific AudioSource.
        /// </summary>
        /// <param name="audioID"></param>
        /// <param name="source"></param>
        public static void PlayOnSource(string audioID, AudioSource source)
        {
            if (!Instance || !Instance.TryGetAudioData(audioID, out var data)) return;
            
            if (!source) return;
            if (!Instance.ConfigureSource(source, data, source.transform.position, true)) return;
            source.Play();
        }
        
        /// <summary>
        /// Stops a looping sound associated with the given ID.
        /// If the ID is currently playing a looping sound, it will be stopped and the AudioSource will be returned to the pool.
        /// </summary>
        /// <param name="id"></param>
        public static void StopLoop(string id)
        {
            if (!Instance) return;
            
            if (Instance._activeLoopSources.Remove(id, out AudioSource source))
            {
                Instance.ReturnSourceToPool(source);
            }
        }
        
        /// <summary>
        /// Stops all currently playing looping sounds.
        /// All looping AudioSources will be stopped and returned to the pool.
        /// </summary>
        public static void StopAllLoops()
        {
            if (!Instance) return;
            
            foreach (var source in Instance._activeLoopSources.Values)
            {
                Instance.ReturnSourceToPool(source);
            }
            Instance._activeLoopSources.Clear();
        }

        #endregion
        

    }
}