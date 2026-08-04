using UnityEngine.Timeline;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Holds AudioLibraryMarkers. Bind this track to a GameObject with an AudioLibraryMarkerReceiver.
    /// </summary>
    [TrackBindingType(typeof(AudioLibraryMarkerReceiver))]
    [TrackColor(0.95f, 0.65f, 0.35f)]
    public class AudioLibraryMarkerTrack : MarkerTrack
    {
    }
}
