using UnityEngine;
using UnityEngine.Playables;

namespace DNExtensions.Systems.AudioMixerTrack
{
    public class AudioMixerVolumeBehaviour : PlayableBehaviour
    {
        public string ExposedParameter;
        public AnimationCurve VolumeCurve;
    }
}
