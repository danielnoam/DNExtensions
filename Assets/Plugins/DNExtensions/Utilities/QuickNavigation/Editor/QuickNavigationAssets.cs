using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Resolves and loads Project assets referenced by Quick Navigation.
    /// </summary>
    internal static class QuickNavigationAssets
    {
        private static readonly Color ExternalFolderIconColor = new Color(1f, 0.85f, 0.2f);

        public static string ResolveToPath(string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return null;

            if (stored.IndexOf('/') >= 0 || Path.IsPathRooted(stored))
                return NormalizePath(stored);

            return AssetDatabase.GUIDToAssetPath(stored);
        }

        public static FavoriteEntry CreateEntry(string guidOrPath)
        {
            var entry = new FavoriteEntry();

            if (string.IsNullOrEmpty(guidOrPath))
                return entry;

            if (TryGetProjectAssetPath(guidOrPath, out string projectAssetPath))
            {
                entry.Path = projectAssetPath;
                entry.Guid = AssetDatabase.AssetPathToGUID(projectAssetPath);
                return entry;
            }

            if (IsExternalPath(guidOrPath))
            {
                entry.IsExternal = true;
                entry.Path = NormalizeExternalPath(guidOrPath);
                entry.Guid = string.Empty;
                return entry;
            }

            if (guidOrPath.IndexOf('/') >= 0)
            {
                entry.Path = NormalizePath(guidOrPath);
                entry.Guid = AssetDatabase.AssetPathToGUID(entry.Path);
            }
            else
            {
                entry.Guid = guidOrPath;
                entry.Path = AssetDatabase.GUIDToAssetPath(guidOrPath);
            }

            return entry;
        }

        public static bool IsAddable(FavoriteEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Path))
                return false;

            if (entry.IsExternal)
                return Directory.Exists(entry.Path);

            return !string.IsNullOrEmpty(entry.Guid) || IsValidPath(entry.Path);
        }

        public static bool TryResolve(FavoriteEntry entry, out string path, out Object obj, out bool entryUpdated)
        {
            entryUpdated = false;
            path = null;
            obj = null;

            if (entry == null)
                return false;

            if (entry.IsExternal)
            {
                if (string.IsNullOrEmpty(entry.Path))
                    return false;

                string normalizedPath = NormalizeExternalPath(entry.Path);
                if (!Directory.Exists(normalizedPath))
                    return false;

                if (entry.Path != normalizedPath)
                {
                    entry.Path = normalizedPath;
                    entryUpdated = true;
                }

                path = normalizedPath;
                return true;
            }

            if (!string.IsNullOrEmpty(entry.Guid))
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(entry.Guid);
                if (IsValidPath(guidPath))
                {
                    if (entry.Path != guidPath)
                    {
                        entry.Path = guidPath;
                        entryUpdated = true;
                    }

                    path = guidPath;
                    obj = LoadAtPath(guidPath);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(entry.Path))
            {
                string normalizedPath = NormalizePath(entry.Path);
                if (IsValidPath(normalizedPath))
                {
                    string currentGuid = AssetDatabase.AssetPathToGUID(normalizedPath);
                    if (!string.IsNullOrEmpty(currentGuid) && entry.Guid != currentGuid)
                    {
                        entry.Guid = currentGuid;
                        entryUpdated = true;
                    }

                    if (entry.Path != normalizedPath)
                    {
                        entry.Path = normalizedPath;
                        entryUpdated = true;
                    }

                    path = normalizedPath;
                    obj = LoadAtPath(normalizedPath);
                    return true;
                }
            }

            return false;
        }

        public static bool IsValid(FavoriteEntry entry)
        {
            return TryResolve(entry, out _, out _, out _);
        }

        public static bool EntriesMatch(FavoriteEntry a, FavoriteEntry b)
        {
            if (a == null || b == null)
                return false;

            if (a.IsExternal || b.IsExternal)
            {
                if (!a.IsExternal || !b.IsExternal)
                    return false;

                if (string.IsNullOrEmpty(a.Path) || string.IsNullOrEmpty(b.Path))
                    return false;

                return NormalizeExternalPath(a.Path) == NormalizeExternalPath(b.Path);
            }

            if (!string.IsNullOrEmpty(a.Guid) && a.Guid == b.Guid)
                return true;

            if (!string.IsNullOrEmpty(a.Path) && !string.IsNullOrEmpty(b.Path) && NormalizePath(a.Path) == NormalizePath(b.Path))
                return true;

            return false;
        }

        public static bool IsValid(string stored)
        {
            var entry = CreateEntry(stored);
            return IsAddable(entry) && TryResolve(entry, out _, out _, out _);
        }

        public static Object Load(string stored)
        {
            return LoadAtPath(ResolveToPath(stored));
        }

        public static string GetDisplayName(FavoriteEntry entry, string path)
        {
            if (entry != null && entry.IsExternal && !string.IsNullOrEmpty(path))
                return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            return null;
        }

        public static void DrawExternalFolderIcon(Rect rect)
        {
            Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
            if (!icon)
                return;

            Color previous = GUI.color;
            GUI.color = ExternalFolderIconColor;
            GUI.Label(rect, icon);
            GUI.color = previous;
        }

        public static bool CanAcceptDraggedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var entry = CreateEntry(path);
            return IsAddable(entry);
        }

        public static bool IsExternalPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (TryGetProjectAssetPath(path, out _))
                return false;

            if (Path.IsPathRooted(path))
                return Directory.Exists(path);

            string normalized = NormalizePath(path);
            if (normalized.StartsWith("Assets/") || normalized.StartsWith("Packages/"))
                return false;

            return Directory.Exists(path);
        }

        private static bool TryGetProjectAssetPath(string path, out string assetPath)
        {
            assetPath = null;
            if (string.IsNullOrEmpty(path))
                return false;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path).Replace('\\', '/');
            }
            catch
            {
                return false;
            }

            string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            string projectRoot = Path.GetDirectoryName(dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot) || !fullPath.StartsWith(projectRoot + "/", System.StringComparison.OrdinalIgnoreCase))
                return false;

            string relative = fullPath.Substring(projectRoot.Length + 1);
            if (!relative.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                return false;

            if (!IsValidPath(relative))
                return false;

            assetPath = NormalizePath(relative);
            return true;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string NormalizeExternalPath(string path)
        {
            try
            {
                return Path.GetFullPath(path).Replace('\\', '/');
            }
            catch
            {
                return NormalizePath(path);
            }
        }

        private static bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (AssetDatabase.IsValidFolder(path))
                return true;

            return AssetDatabase.LoadAssetAtPath<Object>(path) != null;
        }

        private static Object LoadAtPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (AssetDatabase.IsValidFolder(path))
                return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);

            return AssetDatabase.LoadAssetAtPath<Object>(path);
        }
    }
}
