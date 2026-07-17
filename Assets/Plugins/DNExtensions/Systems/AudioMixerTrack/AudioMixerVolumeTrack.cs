using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DNExtensions.Systems.AudioMixerTrack
{
    [TrackColor(0.3f, 0.55f, 0.85f)]
    [TrackClipType(typeof(AudioMixerVolumeAsset))]
    [TrackBindingType(typeof(AudioMixer))]
    public class AudioMixerVolumeTrack : TrackAsset
    {
        [SerializeField, Tooltip("When clips end, hold the last value instead of restoring the mixer's saved volume.")]
        private bool holdValueAfterClips = true;

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<AudioMixerVolumeMixerBehaviour>.Create(graph, inputCount);
            playable.GetBehaviour().HoldValueAfterClips = holdValueAfterClips;
            return playable;
        }
    }
}
