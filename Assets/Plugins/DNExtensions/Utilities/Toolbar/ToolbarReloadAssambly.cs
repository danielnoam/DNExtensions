using UnityEditor.Toolbars;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;





namespace DNExtensions
{
    public class AssemblyReloadLock
    {
        private static bool _isLocked;
        
        private static bool IsLocked
        {
            get => EditorPrefs.GetBool("AssemblyReloadLock.IsLocked", false);
            set
            {
                if (value == IsLocked)
                    return;
        
                EditorPrefs.SetBool("AssemblyReloadLock.IsLocked", value);
        
                if (value)
                    EditorApplication.LockReloadAssemblies();
                else
                {
                    EditorApplication.UnlockReloadAssemblies();
                    EditorUtility.RequestScriptReload();
                }
            }
        }
        
        
        [MainToolbarElement("Project/Toggle Reload Assembly", defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement AssemblyReloadToggle()
        {
            var enabled = IsLocked;
            var iconEnabled = EditorGUIUtility.IconContent("Locked").image as Texture2D;
            var iconDisabled = EditorGUIUtility.IconContent("Unlocked").image as Texture2D;
            var icon = enabled ? iconEnabled : iconDisabled;
            var tooltip = enabled ? "Assembly Reload Disabled" : "Assembly Reload Enabled";
            var content = new MainToolbarContent("Assembly Reload", icon, tooltip);
            var toggle = new MainToolbarToggle(content, enabled, OnToggleValueChanged)
            {
                populateContextMenu = (menu) =>
                {
                    menu.AppendSeparator();

                    menu.AppendAction("Force Reload", _ =>
                    {
                        EditorUtility.RequestScriptReload();
                    });
                }
            };



            return toggle;
        }

        private static void OnToggleValueChanged(bool state)
        {
            IsLocked = state;
            MainToolbar.Refresh("Project/Toggle Reload Assembly");
        }
        

    }

}