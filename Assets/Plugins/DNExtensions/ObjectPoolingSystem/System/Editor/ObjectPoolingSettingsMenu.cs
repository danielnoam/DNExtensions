#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.ObjectPooling.Editor
{
    /// <summary>
    /// Editor utilities for ObjectPoolingSettings.
    /// Provides menu items for quick access and asset creation.
    /// </summary>
    internal static class ObjectPoolingSettingsMenu
    {
        private const string SettingsPath = "Assets/Settings/Resources/ObjectPoolingSettings.asset";
        
        [MenuItem("Tools/DNExtensions/Object Pooling Settings")]
        private static void OpenSettings()
        {
            var settings = ObjectPoolingSettings.Instance;
            
            if (!settings)
            {
                // Try to find it anywhere in project
                string[] guids = AssetDatabase.FindAssets("t:ObjectPoolingSettings");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    settings = AssetDatabase.LoadAssetAtPath<ObjectPoolingSettings>(path);
                }
            }
            
            if (settings)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                // Offer to create it
                if (EditorUtility.DisplayDialog(
                    "Create Object Pooling Settings?",
                    "No ObjectPoolingSettings found. Create one now?\n\n" +
                    "It will be created at: " + SettingsPath + "\n\n" +
                    "Note: Must be in a Resources folder for runtime access.",
                    "Create", "Cancel"))
                {
                    CreateSettingsAsset();
                }
            }
        }
        
        private static void CreateSettingsAsset()
        {
            var settings = ScriptableObject.CreateInstance<ObjectPoolingSettings>();
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(SettingsPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                // Create nested folders
                if (directory != null)
                {
                    string[] folders = directory.Split('/');
                    string currentPath = folders[0];
                
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string parentPath = currentPath;
                        currentPath = $"{currentPath}/{folders[i]}";
                    
                        if (!AssetDatabase.IsValidFolder(currentPath))
                        {
                            AssetDatabase.CreateFolder(parentPath, folders[i]);
                        }
                    }
                }
            }
            
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            
            Debug.Log($"Created ObjectPoolingSettings at: {SettingsPath}");
        }
    }
}
#endif