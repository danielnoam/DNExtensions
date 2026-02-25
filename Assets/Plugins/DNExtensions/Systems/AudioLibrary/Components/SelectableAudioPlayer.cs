using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// Plays audio from the audio library on Selectable interaction events.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DNExtensions/Audio Library/Selectable Audio Player")]
    public class SelectableAudioPlayer : MonoBehaviour
    {
        [SerializeField] private bool onHover;
        [SerializeField, AudioID] private string hoverAudioID = string.Empty;

        [SerializeField] private bool onUnhover;
        [SerializeField, AudioID] private string unhoverAudioID = string.Empty;

        [SerializeField] private bool onSelect;
        [SerializeField, AudioID] private string selectAudioID = string.Empty;

        [SerializeField, HideInInspector, AutoGetSelf] private Selectable selectable;

        private void Awake()
        {
            if (onHover)   selectable.OnPointerEnter(_ => Play(hoverAudioID));
            if (onUnhover) selectable.OnPointerExit(_ => Play(unhoverAudioID));
            if (onSelect)  selectable.OnSelect(_ => Play(selectAudioID));
        }

        private static void Play(string audioID)
        {
            if (string.IsNullOrEmpty(audioID)) return;
            AudioLibrary.Play(audioID);
        }
    }
}