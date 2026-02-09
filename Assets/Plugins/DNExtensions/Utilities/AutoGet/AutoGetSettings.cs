using System.IO;
using UnityEditorInternal;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities.AutoGet
{
    public class AutoGetSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/DNExtensions_AutoGetSettings.asset";
        
        [Tooltip("When should fields be automatically populated?")]
        [SerializeField] private AutoPopulateMode autoPopulateMode = AutoPopulateMode.WhenEmpty;
        [Tooltip("Apply auto-population when editing prefabs?")]
        [SerializeField] private bool autoPopulateInPrefabs = true;
        [Tooltip("Validate AutoGet fields when selecting objects in hierarchy")]
        [SerializeField] private bool validateOnSelection = true;
        [Tooltip("Validate AutoGet fields when saving scenes")]
        [SerializeField] private bool validateOnSceneSave = true;
        [Tooltip("Show populate button (🔄) next to AutoGet fields")]
        [SerializeField] private bool showPopulateButton;
        [Tooltip("Cache reflection data for better performance. Disable if using Hot Reload plugins.")]
        [SerializeField] private bool cacheReflectionData;

        public AutoPopulateMode AutoPopulateMode => autoPopulateMode;
        public bool AutoPopulateInPrefabs => autoPopulateInPrefabs;
        public bool ValidateOnSelection => validateOnSelection;
        public bool ValidateOnSceneSave => validateOnSceneSave;
        public bool ShowPopulateButton => showPopulateButton;
        public bool CacheReflectionData => cacheReflectionData;

        private static AutoGetSettings _instance;

        public static AutoGetSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = LoadOrCreate();
                }
                return _instance;
#else
                return null;
#endif
            }
        }

#if UNITY_EDITOR
        private static AutoGetSettings LoadOrCreate()
        {
            var settings = CreateInstance<AutoGetSettings>();
            
            if (File.Exists(SettingsPath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            }
            
            return settings;
        }

        public void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, SettingsPath, true);
        }

        public void ResetToDefaults()
        {
            autoPopulateMode = AutoPopulateMode.WhenEmpty;
            autoPopulateInPrefabs = true;
            validateOnSelection = true;
            validateOnSceneSave = true;
            showPopulateButton = false;
            cacheReflectionData = false;
            Save();
        }
#endif
    }
}