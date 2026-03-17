using System;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Plays animation clips directly via the Playables API without requiring an Animator Controller.
    /// </summary>
    [AddComponentMenu("DNExtensions/Simple Animator")]
    [RequireComponent(typeof(Animator))]
    public class SimpleAnimator : MonoBehaviour
    {
        [Tooltip("If set, auto plays the index on start")]
        [SerializeField] private OptionalField<int> playOnStart = new OptionalField<int>(0, false);
        [SerializeField] private AnimationClip[] clips;

        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private int _activeSlot;
        private int _currentIndex = -1;
        private float _crossfadeDuration;
        private float _crossfadeElapsed;
        private bool _isCrossfading;

        public bool IsPlaying => _graph.IsValid() && _graph.IsPlaying();
        public int CurrentIndex => _currentIndex;
        public int ClipCount => clips?.Length ?? 0;
        public AnimationClip CurrentClip => _currentIndex >= 0 && _currentIndex < clips.Length ? clips[_currentIndex] : null;

        public float Speed
        {
            get => _graph.IsValid() ? (float)_mixer.GetSpeed() : 1f;
            set
            {
                if (_graph.IsValid())
                {
                    _mixer.SetSpeed(value);
                }
            }
        }

        private void OnValidate()
        {
            if (clips is not { Length: > 0 }) return;
            
            playOnStart.Value = Mathf.Clamp(playOnStart.Value, 0, clips.Length - 1);
        }
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.runtimeAnimatorController = null;
        }

        private void OnDisable()
        {
            DestroyGraph();
        }

        private void OnDestroy()
        {
            DestroyGraph();
        }
        
        private void Start()
        {
            if (playOnStart && clips is { Length: > 0 })
            {
                Play(playOnStart.Value);
            }
        }

        private void Update()
        {
            if (!_isCrossfading) return;

            _crossfadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeElapsed / _crossfadeDuration);

            int fromSlot = 1 - _activeSlot;
            _mixer.SetInputWeight(fromSlot, 1f - t);
            _mixer.SetInputWeight(_activeSlot, t);

            if (t < 1f) return;

            _isCrossfading = false;
            ClearSlot(fromSlot);
        }

        private void PlayInternal(AnimationClip clip, int index, float crossfadeDuration)
        {
            EnsureGraph();

            bool isFirstPlay = _currentIndex < 0;

            if (_isCrossfading)
            {
                int fromSlot = 1 - _activeSlot;
                ClearSlot(fromSlot);
                _mixer.SetInputWeight(fromSlot, 0f);
                _mixer.SetInputWeight(_activeSlot, 1f);
                _isCrossfading = false;
            }

            int newSlot = isFirstPlay ? 0 : 1 - _activeSlot;
            SetSlotClip(newSlot, clip);

            if (crossfadeDuration <= 0f || isFirstPlay)
            {
                _mixer.SetInputWeight(newSlot, 1f);

                if (!isFirstPlay)
                {
                    ClearSlot(_activeSlot);
                    _mixer.SetInputWeight(_activeSlot, 0f);
                }
            }
            else
            {
                _mixer.SetInputWeight(newSlot, 0f);
                _crossfadeDuration = crossfadeDuration;
                _crossfadeElapsed = 0f;
                _isCrossfading = true;
            }

            _activeSlot = newSlot;
            _currentIndex = index;

            if (!_graph.IsPlaying())
            {
                _graph.Play();
            }
        }

        private void EnsureGraph()
        {
            if (_graph.IsValid()) return;

            _graph = PlayableGraph.Create($"SimpleAnimator_{gameObject.name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);

            var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            output.SetSourcePlayable(_mixer);
        }

        private void SetSlotClip(int slot, AnimationClip clip)
        {
            ClearSlot(slot);

            var playable = AnimationClipPlayable.Create(_graph, clip);
            _graph.Connect(playable, 0, _mixer, slot);
        }

        private void ClearSlot(int slot)
        {
            var input = _mixer.GetInput(slot);
            if (!input.IsValid()) return;

            _graph.Disconnect(_mixer, slot);
            input.Destroy();
        }

        private void DestroyGraph()
        {
            if (!_graph.IsValid()) return;

            _graph.Destroy();
            _currentIndex = -1;
            _isCrossfading = false;
        }
        
        /// <summary>
        /// Plays a clip by name with an optional crossfade duration in seconds.
        /// </summary>
        /// <param name="clipName">The name of the clip to play</param>
        /// <param name="crossfadeDuration">If greater than zero, crossfades from the current clip to this one</param>
        public void Play(string clipName, float crossfadeDuration = 0f)
        {
            int index = Array.FindIndex(clips, c => c && c.name == clipName);
            if (index < 0)
            {
                Debug.LogError($"Clip '{clipName}' not found.", this);
                return;
            }

            Play(index, crossfadeDuration);
        }

        /// <summary>
        /// Plays a clip by index with an optional crossfade duration in seconds.
        /// </summary>
        /// <param name="index">The index of the clip to play</param>
        /// <param name="crossfadeDuration">If greater than zero, crossfades from the current clip to this one</param>
        public void Play(int index, float crossfadeDuration = 0f)
        {
            if (index < 0 || index >= clips.Length)
            {
                Debug.LogError($"Index {index} out of range.", this);
                return;
            }

            if (!clips[index])
            {
                Debug.LogError($"Clip at index {index} is null.", this);
                return;
            }

            PlayInternal(clips[index], index, crossfadeDuration);
        }
        
        /// <summary>
        /// Plays a clip with an optional crossfade duration in seconds.
        /// </summary>
        /// <param name="clip">The clip to play</param>
        /// <param name="crossfadeDuration">If greater than zero, crossfades from the current clip to this one</param>
        public void Play(AnimationClip clip, float crossfadeDuration = 0f)
        {
            if (!clip)
            {
                Debug.LogError($"Clip not found.", this);
                return;
            }

            PlayInternal(clip, -1, crossfadeDuration);
        }

        /// <summary>
        /// Stops playback and resets the current clip index.
        /// </summary>
        public void Stop()
        {
            if (!_graph.IsValid()) return;

            _graph.Stop();
            ClearSlot(0);
            ClearSlot(1);
            _currentIndex = -1;
            _isCrossfading = false;
        }
    }
}