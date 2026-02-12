using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditorInternal;

namespace DNExtensions.Utilities.Button
{
    public class ButtonSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/DNExtensions_ButtonSettings.asset";

        [Header("Appearance")]
        [Range(20, 60)] [SerializeField] private int buttonHeight = 30;
        [Range(0, 20)] [SerializeField] private int buttonSpace = 3;
        [SerializeField] private Color buttonColor = Color.white;

        [Header("Behavior")]
        [SerializeField] private ButtonPlayMode buttonPlayMode = ButtonPlayMode.OnlyWhenPlaying;
        [SerializeField] private string buttonGroup = "";

        public int ButtonHeight => buttonHeight;
        public int ButtonSpace => buttonSpace;
        public Color ButtonColor => buttonColor;
        public ButtonPlayMode ButtonPlayMode => buttonPlayMode;
        public string ButtonGroup => buttonGroup;

        private static ButtonSettings _instance;

        internal static ButtonSettings Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = LoadOrCreate();
                }
                return _instance;
            }
        }

        private static ButtonSettings LoadOrCreate()
        {
            var settings = CreateInstance<ButtonSettings>();
            
            if (File.Exists(SettingsPath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);
            }
            
            return settings;
        }

        internal void Save()
        {
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, SettingsPath, true);
        }

        public void ResetToDefaults()
        {
            buttonHeight = 30;
            buttonSpace = 3;
            buttonColor = Color.white;
            buttonPlayMode = ButtonPlayMode.OnlyWhenPlaying;
            buttonGroup = "";
            Save();
        }
    }

    static class ButtonSettingsProvider
    {
        private const string MenuPath = "Tools/DNExtensions/Button Settings";
        private const int MenuPriority = 1000;

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DNExtensions/Button");
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/DNExtensions/Button", SettingsScope.Project)
            {
                label = "Button",
                guiHandler = (searchContext) =>
                {
                    var settings = new SerializedObject(ButtonSettings.Instance);
                    
                    EditorGUILayout.Space(10);
                    EditorGUILayout.HelpBox(
                        "These settings apply to all buttons that don't explicitly override these values.",
                        MessageType.Info
                    );
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUI.BeginChangeCheck();
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("buttonHeight"), 
                        new GUIContent("Height", "Default button height in pixels"));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("buttonSpace"),
                        new GUIContent("Space Before", "Space above button in pixels"));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("buttonColor"),
                        new GUIContent("Color", "Default button background color"));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("buttonPlayMode"),
                        new GUIContent("Play Mode", "When buttons can be clicked"));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("buttonGroup"),
                        new GUIContent("Group", "Default group name (usually leave empty)"));
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ApplyModifiedProperties();
                        ButtonSettings.Instance.Save();
                    }
                    
                    EditorGUILayout.Space(10);
                    
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Reset Button Settings",
                            "Reset all button settings to their default values?",
                            "Reset",
                            "Cancel"))
                        {
                            ButtonSettings.Instance.ResetToDefaults();
                        }
                    }
                },
                
                keywords = new[] { "Button", "DNExtensions", "Inspector" }
            };

            return provider;
        }
    }
}