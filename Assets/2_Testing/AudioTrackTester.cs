using DNExtensions.Utilities.Button;
using UnityEngine;

namespace DNExtensions.Systems.AudioTrack
{
    /// <summary>
    /// Inspector tester for the Audio Track system.
    /// </summary>
    [AddComponentMenu("")]
    public class AudioTrackTester : MonoBehaviour
    {
        [SerializeField, AudioTrackID] private string trackID;
        [SerializeField] private float fadeDuration = 1f;

        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void Play() => AudioTrack.Play(trackID, fadeDuration);

        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void Stop() => AudioTrack.Stop(trackID, fadeDuration);

        [Button(ButtonPlayMode.OnlyWhenPlaying)]
        public void StopAll() => AudioTrack.StopAll(fadeDuration);
    }
}