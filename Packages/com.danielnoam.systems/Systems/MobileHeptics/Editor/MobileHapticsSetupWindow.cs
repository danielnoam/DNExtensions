using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DNExtensions.Systems.MobileHaptics
{
    /// <summary>
    /// Project-wide settings for MobileHaptics. Configured via Project Settings.
    /// </summary>
    public class MobileHapticsSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/MobileHapticsSettings.asset";
        
        public bool addVibratePermission = true;

        private static MobileHapticsSettings _instance;

        public static MobileHapticsSettings Instance
        {
            get
            {
                if (_instance) return _instance;

                if (File.Exists(SettingsPath))
                {
                    var objects = InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
                    if (objects.Length > 0)
                        _instance = objects[0] as MobileHapticsSettings;
                }

                if (!_instance)
                {
                    _instance = CreateInstance<MobileHapticsSettings>();
                    Save();
                }

                return _instance;
            }
        }

        private static void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(
                new Object[] { _instance }, SettingsPath, true);
        }
        
        [MenuItem("Tools/DNExtensions/Mobile Haptics Settings")]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DNExtensions/Mobile Haptics");
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/DNExtensions/Mobile Haptics", SettingsScope.Project)
            {
                label = "Mobile Haptics",
                guiHandler = _ =>
                {
                    var settings = Instance;
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
                    settings.addVibratePermission = EditorGUILayout.Toggle(new GUIContent("Add VIBRATE Permission"), settings.addVibratePermission);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox("The VIBRATE permission is injected at build time via IPostGenerateGradleAndroidProject. ", MessageType.Info);

                    if (EditorGUI.EndChangeCheck()) Save();
                }
            };
        }
    }
}
