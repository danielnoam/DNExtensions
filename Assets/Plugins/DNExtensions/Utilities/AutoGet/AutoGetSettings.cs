using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Settings for AutoGet system behavior.
    /// Access via AutoGetSettings.Instance.
    /// </summary>
    public class AutoGetSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Settings/Editor/AutoGetSettings.asset";
        
        [Tooltip("When should fields be automatically populated?")]
        [SerializeField] private AutoPopulateMode autoPopulateMode = AutoPopulateMode.WhenEmpty;
        [Tooltip("Apply auto-population when editing prefabs?")]
        [SerializeField] private bool autoPopulateInPrefabs = true;
        [Tooltip("Validate AutoGet fields when selecting objects in hierarchy")]
        [SerializeField] private bool validateOnSelection = true;
        [Tooltip("Validate AutoGet fields when saving scenes")]
        [SerializeField] private bool validateOnSceneSave = true;
        [Tooltip("Show populate button (🔄) next to AutoGet fields")]
        [SerializeField] private bool showPopulateButton = true;
        [Tooltip("Cache reflection data for better performance. Disable if using Hot Reload plugins.")]
        [SerializeField] private bool cacheReflectionData = false;

        public AutoPopulateMode AutoPopulateMode => autoPopulateMode;
        public bool AutoPopulateInPrefabs => autoPopulateInPrefabs;
        public bool ValidateOnSelection => validateOnSelection;
        public bool ValidateOnSceneSave => validateOnSceneSave;
        public bool ShowPopulateButton => showPopulateButton;
        public bool CacheReflectionData => cacheReflectionData;

        private static AutoGetSettings _instance;

        /// <summary>
        /// Gets the singleton instance of AutoGetSettings.
        /// Creates the asset if it doesn't exist.
        /// </summary>
        public static AutoGetSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<AutoGetSettings>(SettingsPath);

                    if (_instance == null)
                    {
                        _instance = CreateInstance<AutoGetSettings>();
                        
                        var directory = Path.GetDirectoryName(SettingsPath);
                        if (!Directory.Exists(directory))
                        {
                            if (directory != null) Directory.CreateDirectory(directory);
                        }

                        AssetDatabase.CreateAsset(_instance, SettingsPath);
                        AssetDatabase.SaveAssets();
                    }
                }

                return _instance;
#else
                return null;
#endif
            }
        }
        
    }
}