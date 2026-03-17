using System;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using UnityEngine;

namespace DNExtensions.Systems.AudioLibrary
{
    /// <summary>
    /// A ScriptableObject that represents a library of audio categories. Each category contains a list of audio mappings that map string IDs to audio objects.
    /// </summary>
    [UniqueSO]
    public class SOAudioLibrarySettings : ScriptableObject
    {
        private static SOAudioLibrarySettings _instance;

        public static SOAudioLibrarySettings Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = Resources.Load<SOAudioLibrarySettings>("AudioLibrarySettings");

#if UNITY_EDITOR
                if (!_instance)
                {
                    Debug.LogWarning("AudioLibrarySettings not found in Resources folder. Create one via Tools > DNExtensions > Audio Library Settings");
                }
#endif
                return _instance;
            }
        }


        [SerializeField] private bool enabled = true;
        [Tooltip("If the pool should be limited")]
        [SerializeField] private OptionalField<int> limitPoolSize = new OptionalField<int>(30, true);
        [Tooltip("If the pool should be pre-created and how many audio sources to create at startup")]
        [SerializeField] private OptionalField<int> preWarm = new OptionalField<int>(15, true);
        [SerializeField] private SOAudioCategory[] audioCategories = Array.Empty<SOAudioCategory>();

        public SOAudioCategory[] AudioCategories => audioCategories;
        public bool Enabled => enabled;
        public OptionalField<int> PreWarm => preWarm;
        public OptionalField<int> LimitPoolSize => limitPoolSize;

    }
}