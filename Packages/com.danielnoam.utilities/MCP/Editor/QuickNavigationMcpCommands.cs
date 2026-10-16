using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace DNExtensions.Utilities.MCP
{
    internal static class QuickNavigationMcpCommands
    {
        [CliCommand("dn_recent_assets", "List the assets the user opened most recently and their Quick Navigation favorites, as a hint for what they are working on")]
        private static string RecentAssets(
            [CliArg("count", "How many history entries to return, newest first")] int count = 15,
            [CliArg("include_favorites", "Also list the favorites tabs")] bool includeFavorites = true)
        {
            QuickNavigationData data = QuickNavigationData.Instance;
            var builder = new StringBuilder("Recent:\n");

            int written = 0;

            foreach (string stored in data.HistoryPaths)
            {
                if (written >= count) break;

                string path = QuickNavigationAssets.ResolveToPath(stored);
                if (string.IsNullOrEmpty(path)) continue;

                builder.AppendLine($"  {path}");
                written++;
            }

            if (!includeFavorites) return builder.ToString();

            foreach (FavoriteTab tab in data.FavoriteTabs)
            {
                if (tab == null || tab.IsArchived) continue;

                builder.AppendLine($"Favorites \"{tab.TabName}\":");

                foreach (FavoriteEntry entry in tab.GetEntries())
                {
                    string path = entry.IsExternal ? entry.Path : AssetDatabase.GUIDToAssetPath(entry.Guid);
                    if (string.IsNullOrEmpty(path)) path = entry.Path;

                    builder.AppendLine($"  {path}");
                }
            }

            return builder.ToString();
        }
    }
}
