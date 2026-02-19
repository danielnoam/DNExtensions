using System;
using DNExtensions.Utilities;
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

        
        
        [SerializeField] private bool enabled;
        [SerializeField] private int preWarmAmount = 15;
        [SerializeField] private SOAudioCategory[] audioCategories = Array.Empty<SOAudioCategory>();
        
        public SOAudioCategory[] AudioCategories => audioCategories;
        public bool Enabled => enabled;
        public  int PreWarmAmount => preWarmAmount;
        
    }



    

    
    
}
