using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Distinct asset types held by a folder, as icons ordered by how many of each it contains — the
    /// Project window's counterpart to the Hierarchy's component strip.
    ///
    /// Scanning a folder is far too expensive to do per row per repaint, so results are cached until
    /// the project changes. Types are read with GetMainAssetTypeAtPath, which reads the importer
    /// rather than loading the asset.
    /// </summary>
    internal static class ProjectFolderContents
    {
        private const int MaxScanned = 400;

        private static readonly Dictionary<string, Texture[]> Cache = new Dictionary<string, Texture[]>();
        private static readonly Texture[] Empty = Array.Empty<Texture>();

        private static bool _cachedRecursive;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;
        }

        private static void Invalidate() => Cache.Clear();

        public static Texture[] Get(string folderPath, bool recursive)
        {
            // Packages are read-only third-party content, so their composition is noise rather than
            // information — and scanning the Packages root would walk every installed package.
            if (!IsUnderAssets(folderPath)) return Empty;

            // The cache holds one shape of answer at a time; flipping the setting invalidates it.
            if (_cachedRecursive != recursive)
            {
                _cachedRecursive = recursive;
                Cache.Clear();
            }

            if (Cache.TryGetValue(folderPath, out Texture[] cached)) return cached;

            Texture[] icons = Build(folderPath, recursive);
            Cache[folderPath] = icons;
            return icons;
        }

        private static Texture[] Build(string folderPath, bool recursive)
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (guids.Length == 0) return Empty;

            Dictionary<Type, int> counts = new Dictionary<Type, int>();
            Dictionary<Type, string> firstPath = new Dictionary<Type, string>();
            int scanned = 0;

            foreach (string guid in guids)
            {
                // A folder of thousands of assets would stall the window otherwise. The sample is
                // more than enough to establish which types dominate.
                if (scanned >= MaxScanned) break;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (!recursive && !IsDirectChild(path, folderPath)) continue;

                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null) continue;

                scanned++;
                counts.TryGetValue(type, out int count);
                counts[type] = count + 1;

                if (!firstPath.ContainsKey(type)) firstPath[type] = path;
            }

            if (counts.Count == 0) return Empty;

            // Deduplicated by icon rather than by type. Every ScriptableObject subclass is its own
            // type but they nearly all resolve to the same icon, so grouping by type alone produced a
            // strip of identical entries.
            List<Texture> icons = new List<Texture>();

            // Compared as objects rather than by instance id, which is a compile error from 6.4 on.
            HashSet<Texture> seen = new HashSet<Texture>();

            foreach (KeyValuePair<Type, int> entry in counts.OrderByDescending(entry => entry.Value))
            {
                Texture icon = IconFor(entry.Key, firstPath[entry.Key]);
                if (!icon || !seen.Add(icon)) continue;

                icons.Add(icon);
            }

            return icons.Count == 0 ? Empty : icons.ToArray();
        }

        private static bool IsUnderAssets(string folderPath)
        {
            return folderPath == "Assets" || folderPath.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static bool IsDirectChild(string assetPath, string folderPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            return directory != null && directory.Replace('\\', '/') == folderPath;
        }

        private static Texture IconFor(Type type, string samplePath)
        {
            // A ScriptableObject's mini type thumbnail resolves to its MonoScript — the C# script
            // icon — rather than the ScriptableObject icon the Project window actually shows. The
            // asset's cached icon is what is on screen, including any custom icon set on the script.
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                Texture assetIcon = AssetDatabase.GetCachedIcon(samplePath);
                if (assetIcon) return assetIcon;

                Texture generic = EditorGUIUtility.IconContent("ScriptableObject Icon")?.image;
                if (generic) return generic;
            }

            Texture icon = AssetPreview.GetMiniTypeThumbnail(type);
            return icon ? icon : AssetDatabase.GetCachedIcon(samplePath);
        }
    }
}
