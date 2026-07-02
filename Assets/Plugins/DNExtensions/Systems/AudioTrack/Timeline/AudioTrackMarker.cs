using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DNExtensions.Systems.AudioTrack
{
    /// <summary>
    /// Timeline marker that plays or stops an AudioTrack by ID when the playhead reaches it.
    /// Requires an AudioTrackMarkerReceiver bound to the containing AudioTrackMarkerTrack.
    /// </summary>
    public class AudioTrackMarker : Marker, INotification
    {
        public enum TrackMarkerAction { Play, Stop, Restart }

        [SerializeField, AudioTrackID] private string trackID;
        [SerializeField] private TrackMarkerAction action = TrackMarkerAction.Play;
        [SerializeField] private float fadeDuration = 1f;

        public string TrackID => trackID;
        public TrackMarkerAction Action => action;
        public float FadeDuration => fadeDuration;

        public PropertyName id => new PropertyName();
    }
}
