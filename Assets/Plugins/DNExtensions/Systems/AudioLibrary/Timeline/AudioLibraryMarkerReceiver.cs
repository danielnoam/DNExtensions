using UnityEngine;
using UnityEngine.Playables;

namespace DNExtensions.Systems.AudioLibrary
{
    [AddComponentMenu("DNExtensions/Audio Library/Audio Library Marker Receiver")]
    public class AudioLibraryMarkerReceiver : MonoBehaviour, INotificationReceiver
    {
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is not AudioLibraryMarker marker) return;

            switch (marker.Action)
            {
                case AudioLibraryMarker.LibraryMarkerAction.Play:
                    AudioSource source = marker.CustomSource.Resolve(origin.GetGraph().GetResolver());
                    if (source)
                    {
                        AudioLibrary.PlayOnSource(marker.AudioID, source);
                    }
                    else
                    {
                        AudioLibrary.Play(marker.AudioID);
                    }
                    break;
                case AudioLibraryMarker.LibraryMarkerAction.StopLoop:
                    AudioLibrary.StopLoop(marker.AudioID, marker.FadeOutTime);
                    break;
            }
        }
    }
}
