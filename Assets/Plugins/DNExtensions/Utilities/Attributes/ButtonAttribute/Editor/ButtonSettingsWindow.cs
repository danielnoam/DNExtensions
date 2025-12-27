
using DNExtensions.Button;
using UnityEditor;
using UnityEngine;

namespace DNExtensions
{
    public class ButtonSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Tools/DNExtensions/Button Settings")]
        public static void ShowWindow()
        {
            ButtonSettingsWindow window = GetWindow<ButtonSettingsWindow>();
            window.titleContent = new GUIContent("Button Settings");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Default Button Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These settings apply to all buttons that don't explicitly override these values.", 
                MessageType.Info
            );
            
            // Play Mode
            ButtonPlayMode currentPlayMode = ButtonSettings.ButtonPlayMode;
            ButtonPlayMode newPlayMode = DrawPlayModePopup(
                new GUIContent("Play Mode", "When buttons can be clicked"),
                currentPlayMode
            );
            if (newPlayMode != currentPlayMode)
            {
                ButtonSettings.ButtonPlayMode = newPlayMode;
            }
            
            
            // Height
            ButtonSettings.ButtonHeight = EditorGUILayout.IntSlider(
                new GUIContent("Height", "Default button height in pixels"),
                ButtonSettings.ButtonHeight,
                20,
                60
            );
            
            // Space
            ButtonSettings.ButtonSpace = EditorGUILayout.IntSlider(
                new GUIContent("Space Before", "Space above button in pixels"),
                ButtonSettings.ButtonSpace,
                0,
                20
            );
            

            
            // Color
            ButtonSettings.ButtonColor = EditorGUILayout.ColorField(
                new GUIContent("Color", "Default button background color"),
                ButtonSettings.ButtonColor
            );
            

            
            // Group 
            ButtonSettings.ButtonGroup = EditorGUILayout.TextField(
                new GUIContent("Group", "Default group name (usually leave empty)"),
                ButtonSettings.ButtonGroup
            );
            
            
            // Reset to defaults button
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Button Settings",
                    "Reset all button settings to their default values?",
                    "Reset",
                    "Cancel"))
                {
                    ButtonSettings.ResetToDefaults();
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private ButtonPlayMode DrawPlayModePopup(GUIContent label, ButtonPlayMode current)
        {
            if (current == ButtonPlayMode.UseDefault)
            {
                current = ButtonPlayMode.Both;
            }

            string[] displayOptions = new string[]
            {
                "Both (Edit & Play Mode)",
                "Only When Playing",
                "Only When Not Playing"
            };

            ButtonPlayMode[] values = new ButtonPlayMode[]
            {
                ButtonPlayMode.Both,
                ButtonPlayMode.OnlyWhenPlaying,
                ButtonPlayMode.OnlyWhenNotPlaying
            };

            int currentIndex = System.Array.IndexOf(values, current);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(label, currentIndex, displayOptions);
            
            return values[newIndex];
        }
    }
    
    public static class ButtonSettings
    {
        private const string PrefHeight = "DNExtensions_Button_Height";
        private const string PrefSpace = "DNExtensions_Button_Space";
        private const string PrefPlayMode = "DNExtensions_Button_PlayMode";
        private const string PrefGroup = "DNExtensions_Button_Group";
        private const string PrefColor = "DNExtensions_Button_Color";

        private const int DefaultButtonHeight = 30;
        private const int DefaultButtonSpace = 3;
        private const ButtonPlayMode DefaultButtonPlayMode = ButtonPlayMode.OnlyWhenPlaying;
        private const string DefaultButtonGroup = "";
        private const string DefaultButtonColor = "#FFFFFF";
        
        public static int ButtonHeight
        {
            get => EditorPrefs.GetInt(PrefHeight, DefaultButtonHeight);
            set => EditorPrefs.SetInt(PrefHeight, value);
        }
        
        public static int ButtonSpace
        {
            get => EditorPrefs.GetInt(PrefSpace, DefaultButtonSpace);
            set => EditorPrefs.SetInt(PrefSpace, value);
        }
        
        public static ButtonPlayMode ButtonPlayMode
        {
            get => (ButtonPlayMode)EditorPrefs.GetInt(PrefPlayMode, (int)DefaultButtonPlayMode);
            set => EditorPrefs.SetInt(PrefPlayMode, (int)value);
        }
        
        public static string ButtonGroup
        {
            get => EditorPrefs.GetString(PrefGroup, DefaultButtonGroup);
            set => EditorPrefs.SetString(PrefGroup, value);
        }
        
        public static Color ButtonColor
        {
            get => ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefColor, DefaultButtonColor), out Color color) 
                ? color 
                : Color.white;
            set => EditorPrefs.SetString(PrefColor, "#" + ColorUtility.ToHtmlStringRGB(value));
        }
        
        public static void ResetToDefaults()
        {
            ButtonHeight = DefaultButtonHeight;
            ButtonSpace = DefaultButtonSpace;
            ButtonPlayMode = DefaultButtonPlayMode;
            ButtonGroup = DefaultButtonGroup;
            ButtonColor = Color.white;
        }
    }
}

