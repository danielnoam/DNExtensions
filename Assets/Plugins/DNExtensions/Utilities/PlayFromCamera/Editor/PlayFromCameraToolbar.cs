using UnityEditor.Toolbars;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.Utilities.PlayFromCamera
{
    public class PlayFromCameraToolbar
    {
        [MainToolbarElement("PlayFromCamera/PlayFromCamera Button", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement ProjectSettingsButton()
        {
            var icon = EditorGUIUtility.IconContent("SceneViewCamera@2x").image as Texture2D;
            var content = new MainToolbarContent(icon, "Start play mode from the current Scene View camera position");
            var button = new MainToolbarButton(content, PlayFromCamera.PlayFromCurrentCamera)
            {
                populateContextMenu = (menu) =>
                {
                    menu.AppendAction("Open Settings", _ =>
                    {
                        SettingsService.OpenProjectSettings("Project/DNExtensions/Play from Camera");
                    });
                }
            };

            return button;
        }
    }
}