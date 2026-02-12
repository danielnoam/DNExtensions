#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Asset processor that prevents creation of duplicate unique ScriptableObjects.
    /// </summary>
    public class UniqueSOProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                
                if (asset == null)
                    continue;
                
                var assetType = asset.GetType();
                
                if (!assetType.IsDefined(typeof(UniqueSOAttribute), false))
                    continue;
                
                CheckForDuplicates(asset, assetPath, assetType);
            }
        }

        private static void CheckForDuplicates(Object asset, string assetPath, System.Type assetType)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{assetType.Name}");
            
            if (guids.Length <= 1)
                return;
            
            var existingPaths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != assetPath)
                .ToList();
            
            if (existingPaths.Count > 0)
            {
                EditorApplication.delayCall += () =>
                {
                    if (!AssetDatabase.LoadAssetAtPath<Object>(assetPath))
                        return;
                    
                    bool deleteNew = EditorUtility.DisplayDialog(
                        "Duplicate Unique Asset",
                        $"{assetType.Name} already exists at:\n{existingPaths[0]}\n\n" +
                        $"Only one instance of {assetType.Name} is allowed.\n\n" +
                        $"Delete the new instance at:\n{assetPath}?",
                        "Delete New",
                        "Keep New (Delete Old)"
                    );
                    
                    if (deleteNew)
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                        Debug.Log($"Deleted duplicate {assetType.Name} at {assetPath}");
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(existingPaths[0]);
                        Debug.Log($"Deleted old {assetType.Name} at {existingPaths[0]}");
                    }
                    
                    AssetDatabase.Refresh();
                };
            }
        }
    }
}
#endif