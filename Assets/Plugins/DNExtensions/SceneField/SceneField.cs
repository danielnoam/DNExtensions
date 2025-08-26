using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace DNExtensions
{
    [System.Serializable]
    public class SceneField
    {
        [SerializeField] private Object sceneAsset;
        [SerializeField] private string sceneName = "";
        [SerializeField] private string scenePath = "";

        public string SceneName 
        { 
            get
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("Scene name is empty! No scene asset assigned.");
                    return "";
                }

                // Validate scene exists and is in build settings
                if (!IsSceneValid())
                {
                    Debug.LogWarning($"Scene '{sceneName}' is not valid or not in build settings!");
                }

                return sceneName;
            }
        }

        public string ScenePath 
        { 
            get
            {
                if (string.IsNullOrEmpty(scenePath))
                {
                    Debug.LogWarning("Scene path is empty! No scene asset assigned.");
                    return "";
                }

                // Validate scene exists and is in build settings
                if (!IsSceneValid())
                {
                    Debug.LogWarning($"Scene path '{scenePath}' is not valid or not in build settings!");
                }

                return scenePath;
            }
        }

        public int BuildIndex
        {
            get
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("Scene name is empty!");
                    return -1;
                }

                #if UNITY_EDITOR
                // Get all scenes in build settings
                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (scenes[i].enabled && scenes[i].path == scenePath)
                    {
                        return i;
                    }
                }
                Debug.LogWarning($"Scene '{sceneName}' not found in build settings or is disabled!");
                return -1;
                #else
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
                if (buildIndex == -1)
                {
                    Debug.LogWarning($"Runtime: Scene '{scenePath}' not found in build settings!");
                }
                return buildIndex;
                #endif
            }
        }
        
        
        public static implicit operator string(SceneField sceneField)
        {
            return sceneField.SceneName;
        }

        public static implicit operator int(SceneField sceneField)
        {
            return sceneField.BuildIndex;
        }

        /// <summary>
        /// Checks if the scene is valid and exists in build settings
        /// </summary>
        /// <returns>True if the scene is valid and in build settings, false otherwise</returns>
        public bool IsSceneValid()
        {
            // Check if basic data is present
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            #if UNITY_EDITOR
            // In the editor, check if scene asset still exists
            if (!sceneAsset)
            {
                return false;
            }

            // Check if the scene is in build settings and enabled
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (var buildScene in scenes)
            {
                if (buildScene.path == scenePath && buildScene.enabled)
                {
                    return true;
                }
            }
            return false;
            #else
            // In runtime, use SceneUtility to check build index
            return SceneUtility.GetBuildIndexByScenePath(scenePath) != -1;
            #endif
        }

        /// <summary>
        /// Checks if the scene is in build settings (enabled or disabled)
        /// </summary>
        /// <returns>True if the scene is in build settings, false otherwise</returns>
        public bool IsSceneInBuildSettings()
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            #if UNITY_EDITOR
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (var buildScene in scenes)
            {
                if (buildScene.path == scenePath)
                {
                    return true;
                }
            }
            return false;
            #else
            // In runtime, any scene with a valid build index is in build settings
            return SceneUtility.GetBuildIndexByScenePath(scenePath) != -1;
            #endif
        }
        
        
        
        
        public void LoadScene(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsSceneValid())
            {
                SceneManager.LoadScene(SceneName, mode);
            }
            else
            {
                Debug.LogWarning($"Scene '{sceneName}' is not valid or not in build settings!");
            }
        }
        
        public void UnloadScene()
        {
            if (IsSceneValid())
            {
                SceneManager.UnloadSceneAsync(SceneName);
            }
            else
            {
                Debug.LogWarning($"Scene '{sceneName}' is not valid or not in build settings!");
            }
        }

        public AsyncOperation LoadSceneAsync(LoadSceneMode mode = LoadSceneMode.Single)
        {
            
            if (IsSceneValid())
            {
                return SceneManager.LoadSceneAsync(SceneName, mode);
            }
            else
            {
                Debug.LogWarning($"Scene '{sceneName}' is not valid or not in build settings!");
                return null;
            }
        }
        
        
        public AsyncOperation UnloadSceneAsync()
        {
            if (IsSceneValid())
            {
                return SceneManager.UnloadSceneAsync(SceneName);
            }
            else
            {
                Debug.LogWarning($"Scene '{sceneName}' is not valid or not in build settings!");
                return null;
            }
        }


    }
    
}