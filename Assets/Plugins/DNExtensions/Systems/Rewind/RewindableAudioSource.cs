
using UnityEngine;

namespace DNExtensions.Systems.Rewind
{
    [RequireComponent(typeof(AudioSource))]
    public class RewindableAudioSource : Rewindable
    {
        [Header("Rewind Settings")]
        [SerializeField] private bool reverseAudioDuringRewind = true;
        [SerializeField] private bool muteAudioDuringRewind;
        
        
        private readonly FrameRecordContainer<bool> _isPlaying = new FrameRecordContainer<bool>();
        private readonly FrameRecordContainer<int> _timeSamples = new FrameRecordContainer<int>();
        private readonly FrameRecordContainer<float> _volume = new FrameRecordContainer<float>();
        private readonly FrameRecordContainer<float> _pitch = new FrameRecordContainer<float>();
        
        private AudioSource _audioSource;
        

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public override void Record(int frame)
        {

            _isPlaying.Record(frame, _audioSource.isPlaying, isPlaying => 
                !_isPlaying.HasRecords() || isPlaying != _isPlaying.GetLastRecorded());

            _timeSamples.Record(frame, _audioSource.timeSamples, samples => 
                !_timeSamples.HasRecords() || samples != _timeSamples.GetLastRecorded());
            
            _volume.Record(frame, _audioSource.volume, vol => 
                !_volume.HasRecords() || !Mathf.Approximately(vol, _volume.GetLastRecorded()));
            
            _pitch.Record(frame, _audioSource.pitch, pitch => 
                !_pitch.HasRecords() || !Mathf.Approximately(pitch, _pitch.GetLastRecorded()));
        }

        public override void Rewind(int frame)
        {
            if (_volume.TryGetValue(frame, out var volume))
            {
                _audioSource.volume = muteAudioDuringRewind ? 0f : volume;
            }
            
            if (_pitch.TryGetValue(frame, out var pitch))
            {
                _audioSource.pitch = reverseAudioDuringRewind ? -Mathf.Abs(pitch) : pitch;
            }
            
            if (_timeSamples.TryGetValue(frame, out var timeSamples))
            {
                _audioSource.timeSamples = timeSamples;
            }
            
            if (_isPlaying.TryGetValue(frame, out var isPlaying))
            {
                if (isPlaying && !_audioSource.isPlaying)
                {
                    _audioSource.Play();
                }
                else if (!isPlaying && _audioSource.isPlaying)
                {
                    _audioSource.Pause();
                }
            }
        }
        

        public override void ClearFutureFrames(int frame)
        {
            _isPlaying.RemoveFramesAfter(frame);
            _timeSamples.RemoveFramesAfter(frame);
            _volume.RemoveFramesAfter(frame);
            _pitch.RemoveFramesAfter(frame);
        }
        
    }
}