
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Utilities.AutoGet
{
    
    /// <summary>
    /// Handles automatic validation of AutoGet fields based on settings.
    /// Validates on selection and scene save events.
    /// </summary>
    [InitializeOnLoad]
    internal static class AutoGetValidator
    {
        static AutoGetValidator()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }
        
        private static void OnSelectionChanged()
        {
            var settings = AutoGetSettings.Instance;
            if (!settings.ValidateOnSelection)
                return;
            
            foreach (var gameObject in Selection.gameObjects)
            {
                var behaviours = gameObject.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour))
                    {
                        AutoGetSystem.Process(behaviour);
                    }
                }
            }
        }
        
        private static void OnSceneSaving(Scene scene, string path)
        {
            var settings = AutoGetSettings.Instance;
            if (!settings.ValidateOnSceneSave)
                return;
            
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var behaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour != null && AutoGetSystem.HasAutoGetFields(behaviour))
                    {
                        AutoGetSystem.Process(behaviour);
                    }
                }
            }
        }
    }
}
