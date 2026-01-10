using UnityEditor.Toolbars;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;





namespace DNExtensions
{
    public class ToolbarProjectButtons
    {

        [MainToolbarElement("Project/Project Settings Button", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement ProjectSettingsButton()
        {

            var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
            var content = new MainToolbarContent(icon, "Open project settings window");

            return new MainToolbarButton(content, () => { SettingsService.OpenProjectSettings(); });
        }


        [MainToolbarElement("Project/Preferences Button", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarButton PreferencesButton()
        {

            var icon = EditorGUIUtility.IconContent("HeadZoomSilhouette").image as Texture2D;
            var content = new MainToolbarContent(icon, "Open preferences window");

            return new MainToolbarButton(content, () => { SettingsService.OpenUserPreferences(); });
        }
        
        [MainToolbarElement("Project/Build Settings Button", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement BuildSettingsButton()
        {

            var icon = EditorGUIUtility.IconContent("BuildSettings.Editor").image as Texture2D;
            var content = new MainToolbarContent(icon, "Open build settings window");

            return new MainToolbarButton(content, () => { EditorWindow.GetWindow(typeof(BuildPlayerWindow), false, "Build Settings"); });
        }
    }

}