using DNExtensions.Utilities;
using DNExtensions.Utilities.AutoGet;
using UnityEngine;
using UnityEngine.UI;

namespace DNExtensions.Systems.AudioLibrary
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DNExtensions/Audio Library/Selectable Audio Player")]
    public class SelectableAudioPlayer : MonoBehaviour
    {
        [SerializeField] private bool onSelect;
        [SerializeField, AudioLibraryID] private string selectAudioID = string.Empty;
        [SerializeField] private bool onDeselect;
        [SerializeField, AudioLibraryID] private string deselectAudioID = string.Empty;
        [SerializeField] private bool onSubmit;
        [SerializeField, AudioLibraryID] private string submitAudioID = string.Empty;
        [SerializeField, HideInInspector, AutoGetSelf] private Selectable selectable;

        private void OnValidate()
        {
            AutoGetSystem.Process(this);
        }

        private void Awake()
        {
            if (onSelect)   selectable.OnSelect(_ => Play(selectAudioID));
            if (onDeselect) selectable.OnDeselect(_ => Play(deselectAudioID));
            if (onSubmit)
            {
                if (selectable is Button button)
                {
                    button.onClick.AddListener(() => Play(submitAudioID));
                }
                else
                {
                    selectable.OnSubmit(_ => Play(submitAudioID));
                }
            }
        }

        private static void Play(string audioID)
        {
            if (string.IsNullOrEmpty(audioID)) return;
            AudioLibrary.Play(audioID);
        }
    }
}