using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// A favorite asset or external folder stored by GUID and path for rename and branch-switch resilience.
    /// </summary>
    [Serializable]
    internal class FavoriteEntry
    {
        public string Guid;
        public string Path;
        public bool IsExternal;
    }

    /// <summary>
    /// A named favorites tab with optional archive state.
    /// </summary>
    [Serializable]
    internal class FavoriteTab
    {
        public string TabName;
        public Color TabColor = Color.white;
        public bool IsArchived = false;
        public List<string> Paths = new List<string>();
        public List<FavoriteEntry> Entries = new List<FavoriteEntry>();

        public List<FavoriteEntry> GetEntries()
        {
            if (Entries == null)
                Entries = new List<FavoriteEntry>();

            return Entries;
        }

        public bool ContainsEntry(FavoriteEntry candidate)
        {
            foreach (var entry in GetEntries())
            {
                if (QuickNavigationAssets.EntriesMatch(entry, candidate))
                    return true;
            }

            return false;
        }

        public bool RemoveMatchingEntry(FavoriteEntry candidate)
        {
            var entries = GetEntries();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (QuickNavigationAssets.EntriesMatch(entries[i], candidate))
                {
                    entries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Persisted history and favorites data for quick asset navigation.
    /// </summary>
    [Serializable]
    internal class QuickNavigationData : ScriptableObject
    {
        public List<string> HistoryPaths = new List<string>();
        public List<string> FavoritePaths = new List<string>();
        public List<FavoriteTab> FavoriteTabs = new List<FavoriteTab>();

        private static string DataPath => Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "QuickNavigation.json");

        private static QuickNavigationData _instance;
        public static QuickNavigationData Instance
        {
            get
            {
                if (_instance == null) _instance = Load();
                return _instance;
            }
        }

        public void RecordUndo(string actionName)
        {
            Undo.RecordObject(this, actionName);
        }

        public void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(DataPath, json);
        }

        private static QuickNavigationData Load()
        {
            var instance = CreateInstance<QuickNavigationData>();
            instance.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                if (File.Exists(DataPath))
                {
                    string json = File.ReadAllText(DataPath);
                    JsonUtility.FromJsonOverwrite(json, instance);

                    if (instance.FavoriteTabs == null || instance.FavoriteTabs.Count == 0)
                    {
                        instance.FavoriteTabs = new List<FavoriteTab>();
                        var generalTab = new FavoriteTab { TabName = "General", TabColor = Color.white };

                        if (instance.FavoritePaths != null && instance.FavoritePaths.Count > 0)
                        {
                            generalTab.Paths.AddRange(instance.FavoritePaths);
                            instance.FavoritePaths.Clear();
                        }

                        instance.FavoriteTabs.Add(generalTab);
                    }

                    if (instance.MigrateFavorites())
                        instance.Save();

                    return instance;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QuickNavigation] Failed to load data, resetting to defaults: {e.Message}");
            }

            instance.FavoriteTabs.Add(new FavoriteTab { TabName = "General", TabColor = Color.white });
            return instance;
        }

        public void AddToHistory(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;

            HistoryPaths.Remove(guid);
            HistoryPaths.Insert(0, guid);

            if (HistoryPaths.Count > 50)
                HistoryPaths.RemoveAt(HistoryPaths.Count - 1);

            Save();
        }

        public void ClearHistory()
        {
            RecordUndo("Clear History");
            HistoryPaths.Clear();
            Save();
        }

        public void AddToFavorites(string guidOrPath, int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= FavoriteTabs.Count)
                return;

            var entry = QuickNavigationAssets.CreateEntry(guidOrPath);
            if (!QuickNavigationAssets.IsAddable(entry))
                return;

            var tab = FavoriteTabs[tabIndex];
            if (tab.ContainsEntry(entry))
                return;

            RecordUndo("Add to Favorites");
            tab.GetEntries().Add(entry);
            Save();
        }

        public int DuplicateFavoriteTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= FavoriteTabs.Count)
                return -1;

            var source = FavoriteTabs[tabIndex];
            var duplicate = new FavoriteTab
            {
                TabName = GetDuplicateTabName(source.TabName),
                TabColor = source.TabColor.a == 0f ? Color.white : source.TabColor,
                IsArchived = false
            };

            foreach (var entry in source.GetEntries())
            {
                duplicate.GetEntries().Add(new FavoriteEntry
                {
                    Guid = entry.Guid,
                    Path = entry.Path,
                    IsExternal = entry.IsExternal
                });
            }

            RecordUndo("Duplicate Tab");
            int newIndex = tabIndex + 1;
            FavoriteTabs.Insert(newIndex, duplicate);
            Save();
            return newIndex;
        }

        private string GetDuplicateTabName(string tabName)
        {
            string baseName = string.IsNullOrWhiteSpace(tabName) ? "Tab" : tabName.Trim();
            string candidate = baseName + " Copy";
            int counter = 2;

            while (TabNameExists(candidate))
            {
                candidate = $"{baseName} Copy {counter}";
                counter++;
            }

            return candidate;
        }

        private bool TabNameExists(string tabName)
        {
            foreach (var tab in FavoriteTabs)
            {
                if (tab.TabName == tabName)
                    return true;
            }

            return false;
        }

        public bool HealFavorites()
        {
            bool changed = false;

            foreach (var tab in FavoriteTabs)
            {
                foreach (var entry in tab.GetEntries())
                {
                    if (QuickNavigationAssets.TryResolve(entry, out string resolvedPath, out _, out bool entryUpdated))
                    {
                        if (entry.IsExternal && !string.IsNullOrEmpty(resolvedPath) && entry.Path != resolvedPath)
                        {
                            entry.Path = resolvedPath;
                            entryUpdated = true;
                        }

                        if (entryUpdated)
                            changed = true;
                    }
                }
            }

            return changed;
        }

        private bool MigrateFavorites()
        {
            bool changed = false;

            foreach (var tab in FavoriteTabs)
            {
                var entries = tab.GetEntries();

                if (tab.Paths != null && tab.Paths.Count > 0)
                {
                    foreach (string stored in tab.Paths)
                    {
                        var entry = QuickNavigationAssets.CreateEntry(stored);
                        if (!tab.ContainsEntry(entry))
                        {
                            entries.Add(entry);
                            changed = true;
                        }
                    }

                    tab.Paths.Clear();
                    changed = true;
                }

                var deduped = new List<FavoriteEntry>(entries.Count);
                foreach (var entry in entries)
                {
                    bool alreadyAdded = false;
                    foreach (var existing in deduped)
                    {
                        if (QuickNavigationAssets.EntriesMatch(existing, entry))
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }

                    if (alreadyAdded)
                    {
                        changed = true;
                        continue;
                    }

                    deduped.Add(entry);
                }

                if (deduped.Count != entries.Count)
                    changed = true;

                tab.Entries = deduped;
            }

            if (HealFavorites())
                changed = true;

            return changed;
        }
    }
}
