using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Per-folder facts the Project window overlays need on every row of every repaint — whether a
    /// folder draws a foldout arrow, and which asset types it holds. Both are far too expensive to
    /// recompute per row, and both survive here rather than in a plain dictionary so a domain reload
    /// does not throw the whole lot away and stall the first repaint after every recompile.
    ///
    /// Entries are invalidated one folder at a time by <see cref="ProjectCacheInvalidator"/>, rather
    /// than by clearing everything whenever the project changes.
    /// </summary>
    [FilePath("Library/HelpfulEditorProjectCache.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ProjectCache : ScriptableSingleton<ProjectCache>, ISerializationCallbackReceiver
    {
        [Serializable]
        internal sealed class FolderEntry
        {
            public string path;

            public bool foldoutKnown;
            public bool hasSubfolders;
            public bool hasChildren;

            public bool contentKnown;
            public bool contentRecursive;
            public List<string> contentTypeNames = new List<string>();
            public List<string> contentSamplePaths = new List<string>();

            /// <summary>Resolved from the stored type names on first use, since textures cannot be serialised.</summary>
            [NonSerialized] public Texture[] contentIcons;
        }

        [SerializeField] private List<FolderEntry> _entries = new List<FolderEntry>();

        private readonly Dictionary<string, FolderEntry> _byPath = new Dictionary<string, FolderEntry>();

        private bool _dirty;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Written at the two points the in-memory copy is about to be lost, rather than on every
            // change: a folder scan can touch hundreds of entries at once and a file write per entry
            // would cost more than the scan it is saving.
            AssemblyReloadEvents.beforeAssemblyReload -= Flush;
            AssemblyReloadEvents.beforeAssemblyReload += Flush;

            EditorApplication.quitting -= Flush;
            EditorApplication.quitting += Flush;
        }

        private static void Flush() => instance.SaveIfDirty();

        public void OnBeforeSerialize()
        {
            // Folders that no longer exist would otherwise accumulate forever, since nothing ever
            // asks about them again to notice they are gone.
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (!Directory.Exists(_entries[i].path)) _entries.RemoveAt(i);
            }
        }

        public void OnAfterDeserialize()
        {
            _byPath.Clear();

            foreach (FolderEntry entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.path)) _byPath[entry.path] = entry;
            }
        }

        public FolderEntry GetOrCreate(string folderPath)
        {
            if (_byPath.TryGetValue(folderPath, out FolderEntry entry)) return entry;

            entry = new FolderEntry { path = folderPath };
            _entries.Add(entry);
            _byPath[folderPath] = entry;
            return entry;
        }

        public void MarkDirty() => _dirty = true;

        private void SaveIfDirty()
        {
            if (!_dirty) return;

            _dirty = false;
            Save(true);
        }

        /// <summary>
        /// Drops what a folder knows about itself. Content and foldout state are invalidated
        /// together because the two things that change them — an asset appearing and a subfolder
        /// appearing — are the same event as far as the callback is concerned.
        /// </summary>
        public void Invalidate(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            if (!_byPath.TryGetValue(folderPath, out FolderEntry entry)) return;

            entry.foldoutKnown = false;
            entry.contentKnown = false;
            entry.contentIcons = null;
            entry.contentTypeNames.Clear();
            entry.contentSamplePaths.Clear();

            _dirty = true;
        }
    }

    /// <summary>
    /// Marks only the folders an import actually touched, instead of the blanket clear a
    /// projectChanged subscription would do. A single asset import used to cost a full rescan of
    /// every folder the window had ever drawn.
    /// </summary>
    internal sealed class ProjectCacheInvalidator : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            // With recursive content on, an asset changes the composition of every folder above it,
            // so the invalidation has to climb rather than stop at the immediate parent.
            bool climb = HelpfulEditorSettings.Project.folderContentRecursive;
            bool touched = false;

            touched |= InvalidateFor(importedAssets, climb);
            touched |= InvalidateFor(deletedAssets, climb);
            touched |= InvalidateFor(movedAssets, climb);
            touched |= InvalidateFor(movedFromAssetPaths, climb);

            if (touched) EditorApplication.RepaintProjectWindow();
        }

        private static bool InvalidateFor(string[] paths, bool climb)
        {
            if (paths == null || paths.Length == 0) return false;

            foreach (string path in paths)
            {
                // A folder's own row shows what is inside it, so a changed folder invalidates
                // itself as well as its parent.
                if (AssetDatabase.IsValidFolder(path)) ProjectCache.instance.Invalidate(path);

                string parent = ParentOf(path);

                while (!string.IsNullOrEmpty(parent))
                {
                    ProjectCache.instance.Invalidate(parent);
                    if (!climb) break;

                    parent = ParentOf(parent);
                }
            }

            return true;
        }

        private static string ParentOf(string path)
        {
            int slash = path.LastIndexOf('/');
            return slash <= 0 ? null : path.Substring(0, slash);
        }
    }
}
