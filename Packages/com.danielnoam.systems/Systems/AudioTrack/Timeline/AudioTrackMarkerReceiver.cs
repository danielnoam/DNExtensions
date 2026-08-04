using UnityEngine;
using UnityEngine.Playables;

namespace DNExtensions.Systems.AudioTrack
{
    [AddComponentMenu("DNExtensions/Audio Track/Audio Track Marker Receiver")]
    public class AudioTrackMarkerReceiver : MonoBehaviour, INotificationReceiver
    {
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is not AudioTrackMarker marker) return;

            switch (marker.Action)
            {
                case AudioTrackMarker.TrackMarkerAction.Play:
                    AudioTrack.Play(marker.TrackID, marker.FadeDuration);
                    break;
                case AudioTrackMarker.TrackMarkerAction.Stop:
                    AudioTrack.Stop(marker.TrackID, marker.FadeDuration);
                    break;
                case AudioTrackMarker.TrackMarkerAction.Restart:
                    AudioTrack.Restart(marker.TrackID, marker.FadeDuration);
                    break;
            }
        }
    }
}
