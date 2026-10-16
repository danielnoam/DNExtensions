using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.MCP
{
    /// <summary>
    /// Shared lookups for the DNExtensions MCP commands. Failures throw, so the Pipeline server
    /// reports them back to the agent as a command error instead of a silent empty result.
    /// </summary>
    public static class McpUtility
    {
        /// <summary>
        /// Finds a GameObject in the loaded scenes by its hierarchy path, e.g. "Player/Camera".
        /// A path with no slash also matches the first GameObject with that name anywhere in the hierarchy.
        /// </summary>
        public static GameObject FindGameObject(string hierarchyPath)
        {
            if (string.IsNullOrWhiteSpace(hierarchyPath))
            {
                throw new ArgumentException("A GameObject hierarchy path is required.");
            }

            string[] parts = hierarchyPath.Trim('/').Split('/');

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name != parts[0]) continue;

                    Transform current = root.transform;

                    for (int p = 1; p < parts.Length && current; p++)
                    {
                        current = current.Find(parts[p]);
                    }

                    if (current) return current.gameObject;
                }
            }

            if (parts.Length == 1)
            {
                foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
                {
                    if (gameObject.name == parts[0] && gameObject.scene.IsValid()) return gameObject;
                }
            }

            throw new ArgumentException($"No GameObject found at '{hierarchyPath}' in the loaded scenes.");
        }

        /// <summary>
        /// Resolves a target that is either an asset path ("Assets/...") or a scene hierarchy path.
        /// </summary>
        public static Object FindTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException("A target is required: an asset path or a scene hierarchy path.");
            }

            if (target.StartsWith("Assets/") || target.StartsWith("Packages/"))
            {
                return LoadAsset<Object>(target);
            }

            return FindGameObject(target);
        }

        public static T LoadAsset<T>(string assetPath) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (!asset)
            {
                throw new ArgumentException($"No {typeof(T).Name} found at '{assetPath}'.");
            }

            return asset;
        }

        /// <summary>
        /// Every asset of this type in the project whose name or path contains the filter (case-insensitive).
        /// </summary>
        public static List<T> FindAssets<T>(string filter = "") where T : Object
        {
            var results = new List<T>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!string.IsNullOrEmpty(filter) && path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T typed) results.Add(typed);
                }
            }

            return results;
        }

        public static string GetHierarchyPath(Transform transform)
        {
            var builder = new StringBuilder(transform.name);

            for (Transform parent = transform.parent; parent; parent = parent.parent)
            {
                builder.Insert(0, '/').Insert(0, parent.name);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Throws when the editor is not in play mode, for commands that only mean something at runtime.
        /// </summary>
        public static void RequirePlayMode(string what)
        {
            if (Application.isPlaying) return;

            throw new InvalidOperationException($"{what} only works in play mode. Enter play mode first (unity command editor_play).");
        }
    }
}
