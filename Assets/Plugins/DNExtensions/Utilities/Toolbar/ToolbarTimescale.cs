using UnityEditor.Toolbars;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;




namespace DNExtensions
{
    public class ToolbarTimescale
    {
        const float MinTimeScale = 0f;
        const float MaxTimeScale = 5f;
        const float DefaultTimeScale = 1f;
        

        [MainToolbarElement("Timescale/Slider", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement TimeSlider()
        {
            var content = new MainToolbarContent("Time Scale", "Adjust the timescale of the game");

            var slider = new MainToolbarSlider(content, Time.timeScale, MinTimeScale, MaxTimeScale, OnSliderValueChanged)
                {
                    populateContextMenu = (menu) =>
                    {
                        menu.AppendSeparator();
                        
                        menu.AppendAction("Reset To Default", _ =>
                        {
                            Time.timeScale = DefaultTimeScale;
                            MainToolbar.Refresh("Timescale/Slider");
                        });
                        
                        menu.AppendAction("Set Min", _ =>
                        {
                            Time.timeScale = MinTimeScale;
                            MainToolbar.Refresh("Timescale/Slider");
                        });
                        
                        menu.AppendAction("Set Max", _ =>
                        {
                            Time.timeScale = MaxTimeScale;
                            MainToolbar.Refresh("Timescale/Slider");
                        });
                    }
                };

            return slider;
        }


        [MainToolbarElement("Timescale/Reset Button", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement ResetButton()
        {
            var icon = EditorGUIUtility.IconContent("Refresh").image as Texture2D;

            var content = new MainToolbarContent(icon, "Reset timescale to default");

            var button = new MainToolbarButton(content, () =>
            {
                Time.timeScale = DefaultTimeScale;
                MainToolbar.Refresh("Timescale/Slider");
            });

            return button;
        }

        private static void OnSliderValueChanged(float newValue)
        {
            Time.timeScale = newValue;
        }



    }
}
