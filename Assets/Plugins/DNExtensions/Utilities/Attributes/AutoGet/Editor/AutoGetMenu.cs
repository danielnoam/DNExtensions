#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Utilities.AutoGet
{
    internal static class AutoGetMenu
    {
        private const string MenuRoot = "Tools/DNExtensions/AutoGet/";
        private const int MenuPriority = 1000;
        
        [MenuItem("CONTEXT/MonoBehaviour/Populate AutoGet Fields")]
        private static void PopulateAutoGetFieldsContext(MenuCommand command)
        {
            var behaviour = command.context as MonoBehaviour;
            if (behaviour == null) return;
            
            Undo.RecordObject(behaviour, "Populate AutoGet Fields");
            AutoGetSystem.Process(behaviour);
            EditorUtility.SetDirty(behaviour);
        }
        
        [MenuItem("CONTEXT/MonoBehaviour/Populate AutoGet Fields", true)]
        private static bool PopulateAutoGetFieldsContextValidation(MenuCommand command)
        {
            var behaviour = command.context as MonoBehaviour;
            return behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour);
        }
        
        [MenuItem(MenuRoot + "Settings",  false,  MenuPriority)]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DNExtensions/AutoGet");
        }
        
        [MenuItem(MenuRoot + "Populate Selected", false, MenuPriority + 1)]
        internal static void PopulateSelected()
        {
            var count = 0;
            foreach (var gameObject in Selection.gameObjects)
            {
                var behaviours = gameObject.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour))
                    {
                        Undo.RecordObject(behaviour, "Populate AutoGet Fields");
                        AutoGetSystem.Process(behaviour);
                        count++;
                    }
                }
            }
            
            Debug.Log($"Populated AutoGet fields on {count} component(s).");
        }
        
        [MenuItem(MenuRoot + "Populate Current Scene", false, MenuPriority + 2)]
        internal static void PopulateCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            var count = 0;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var behaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour))
                    {
                        Undo.RecordObject(behaviour, "Populate AutoGet Fields");
                        AutoGetSystem.Process(behaviour);
                        count++;
                    }
                }
            }
            
            Debug.Log($"Populated AutoGet fields on {count} component(s) in scene '{scene.name}'.");
        }
        
        [MenuItem(MenuRoot + "Clear Cache", false, MenuPriority + 3)]
        private static void ClearCache()
        {
            AutoGetCache.Clear();
            Debug.Log("AutoGet reflection cache cleared.");
        }
        
        [MenuItem(MenuRoot + "Clear Cache", true)]
        private static bool ClearCacheValidation()
        {
            return AutoGetSettings.Instance.CacheReflectionData && AutoGetCache.CacheSize > 0;
        }
    }
}
#endif