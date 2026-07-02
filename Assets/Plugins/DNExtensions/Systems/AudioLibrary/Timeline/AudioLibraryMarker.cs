using DNExtensions.Utilities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Timeline marker that plays a sound from the AudioLibrary, or stops a looping one, when the playhead reaches it.
    /// Requires an AudioLibraryMarkerReceiver bound to the containing AudioLibraryMarkerTrack.
    /// </summary>
    public class AudioLibraryMarker : Marker, INotification
    {
        public enum LibraryMarkerAction { Play, StopLoop }

        [SerializeField, AudioLibraryID] private string audioID;
        [SerializeField] private LibraryMarkerAction action = LibraryMarkerAction.Play;
        [SerializeField, ShowIf(nameof(action), LibraryMarkerAction.StopLoop)] private float fadeOutTime;

        [SerializeField, ShowIf(nameof(action), LibraryMarkerAction.Play),
         Tooltip("Optional. If assigned, plays on this AudioSource instead of a pooled AudioLibrary source.")]
        private ExposedReference<AudioSource> customSource;

        public string AudioID => audioID;
        public LibraryMarkerAction Action => action;
        public float FadeOutTime => fadeOutTime;
        public ExposedReference<AudioSource> CustomSource => customSource;

        public PropertyName id => new PropertyName();
    }
}
