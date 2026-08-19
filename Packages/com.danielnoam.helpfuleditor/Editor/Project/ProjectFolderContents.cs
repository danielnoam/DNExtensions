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
    /// Scanning a folder is far too expensive to do per row per repaint, so the result is kept in
    /// <see cref="ProjectCache"/>, which outlives a domain reload and is invalidated one folder at a
    /// time. Types are read with GetMainAssetTypeAtPath, which reads the importer rather than
    /// loading the asset.
    /// </summary>
    internal static class ProjectFolderContents
    {
        private const int MaxScanned = 400;

        private static readonly Texture[] Empty = Array.Empty<Texture>();

        public static Texture[] Get(string folderPath, bool recursive)
        {
            // Packages are read-only third-party content, so their composition is noise rather than
            // information — and scanning the Packages root would walk every installed package.
            if (!IsUnderAssets(folderPath)) return Empty;

            ProjectCache.FolderEntry entry = ProjectCache.instance.GetOrCreate(folderPath);

            if (!entry.contentKnown || entry.contentRecursive != recursive)
            {
                Scan(folderPath, recursive, entry);

                entry.contentRecursive = recursive;
                entry.contentKnown = true;
                entry.contentIcons = null;

                ProjectCache.instance.MarkDirty();
            }

            // Textures cannot be serialised, so a reloaded entry resolves its icons from the type
            // names it stored — which is the cheap half of the work.
            return entry.contentIcons ??= ResolveIcons(entry);
        }

        private static void Scan(string folderPath, bool recursive, ProjectCache.FolderEntry entry)
        {
            entry.contentTypeNames.Clear();
            entry.contentSamplePaths.Clear();

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (guids.Length == 0) return;

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

            // Deduplicated by icon rather than by type. Every ScriptableObject subclass is its own
            // type but they nearly all resolve to the same icon, so grouping by type alone produced a
            // strip of identical entries. Only the survivors are stored, so the dedupe is not repeated
            // every time the entry is read back.
            HashSet<Texture> seen = new HashSet<Texture>();

            foreach (KeyValuePair<Type, int> pair in counts.OrderByDescending(entryPair => entryPair.Value))
            {
                string samplePath = firstPath[pair.Key];

                Texture icon = IconFor(pair.Key, samplePath);
                if (!icon || !seen.Add(icon)) continue;

                entry.contentTypeNames.Add(TypeKey(pair.Key));
                entry.contentSamplePaths.Add(samplePath);
            }
        }

        private static Texture[] ResolveIcons(ProjectCache.FolderEntry entry)
        {
            int count = Mathf.Min(entry.contentTypeNames.Count, entry.contentSamplePaths.Count);
            if (count == 0) return Empty;

            List<Texture> icons = new List<Texture>(count);

            for (int i = 0; i < count; i++)
            {
                string samplePath = entry.contentSamplePaths[i];
                Type type = Type.GetType(entry.contentTypeNames[i]);

                // A type that no longer resolves — a script renamed since the scan, or an assembly
                // not yet loaded — still has the asset it was found on to fall back to.
                Texture icon = type != null ? IconFor(type, samplePath) : AssetDatabase.GetCachedIcon(samplePath);
                if (icon) icons.Add(icon);
            }

            return icons.Count == 0 ? Empty : icons.ToArray();
        }

        /// <summary>Assembly-qualified without the version, which changes on every Unity upgrade.</summary>
        private static string TypeKey(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
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

                Texture generic = HelpfulEditorGUI.LoadIcon("ScriptableObject Icon");
                if (generic) return generic;
            }

            Texture icon = AssetPreview.GetMiniTypeThumbnail(type);
            return icon ? icon : AssetDatabase.GetCachedIcon(samplePath);
        }
    }
}
