using UnityEngine.Timeline;

namespace DNExtensions.Systems.AudioTrack
{
    /// <summary>
    /// Holds AudioTrackMarkers. Bind this track to a GameObject with an AudioTrackMarkerReceiver.
    /// </summary>
    [TrackBindingType(typeof(AudioTrackMarkerReceiver))]
    [TrackColor(0.35f, 0.65f, 0.95f)]
    public class AudioTrackMarkerTrack : MarkerTrack
    {
    }
}
