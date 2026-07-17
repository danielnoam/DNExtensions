using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DNExtensions.Systems.AudioMixerTrack
{
    [Serializable]
    public class AudioMixerVolumeAsset : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private string exposedParameter = "MasterVolume";
        [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AudioMixerVolumeBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.ExposedParameter = exposedParameter;
            behaviour.VolumeCurve = volumeCurve;
            return playable;
        }
    }
}
