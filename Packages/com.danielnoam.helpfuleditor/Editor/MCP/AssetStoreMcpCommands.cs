using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace DNExtensions.HelpfulEditor.MCP
{
    /// <summary>
    /// Lets an agent browse, download and import the user's Asset Store library, including what has
    /// never been downloaded. The library itself is fetched by dn_store_refresh and cached, so listing
    /// is instant and works offline; downloads and the refresh run in the background and are polled.
    /// </summary>
    internal static class AssetStoreMcpCommands
    {
        private const int DefaultLimit = 50;

        [CliCommand("dn_store_refresh", "Fetch the signed-in user's whole Asset Store library (every owned asset, downloaded or not, filed by category) into a local cache. Runs in the background for a few seconds; dn_store_categories and dn_store_list report when it is done")]
        private static string Refresh()
        {
            if (!AssetStoreLibrary.StartRefresh(out string reason))
            {
                throw new InvalidOperationException(reason);
            }

            AssetStoreLibrary.IsSignedIn(out string user);

            return $"Fetching {user}'s Asset Store library in the background. Call dn_store_categories or dn_store_list in a few seconds.";
        }

        [CliCommand("dn_store_categories", "List the categories of the user's Asset Store library with how many assets each holds and how many of those are already downloaded")]
        private static string Categories()
        {
            if (Unready(out string message)) return message;

            Dictionary<long, AssetStoreLibrary.LocalPackage> local = AssetStoreLibrary.LocalPackages();
            StringBuilder builder = new StringBuilder(Header(local));

            foreach (AssetStoreLibrary.Category category in AssetStoreLibrary.Categories)
            {
                int downloaded = AssetStoreLibrary.Items.Count(item => InCategory(item, category.name) && local.ContainsKey(item.id));

                builder.Append('\n').Append(category.name).Append(": ").Append(category.count).Append(" owned, ").Append(downloaded).Append(" downloaded");
            }

            return builder.ToString();
        }

        [CliCommand("dn_store_list", "List assets from the user's Asset Store library with their product id, category and whether they are downloaded. Filter by category (includes its subcategories), a search text, or downloaded state")]
        private static string List(
            [CliArg("category", "Only this category and its subcategories, e.g. 'tools' or 'audio/sound-fx' (empty for all)")] string category = "",
            [CliArg("search", "Only assets whose name contains this text")] string search = "",
            [CliArg("downloaded", "all, yes (already in the download cache) or no (owned but never downloaded)")] string downloaded = "all",
            [CliArg("include_hidden", "Also list purchases hidden in My Assets")] bool includeHidden = false,
            [CliArg("limit", "Most rows to return (0 for no limit)")] int limit = DefaultLimit)
        {
            if (Unready(out string message)) return message;

            Dictionary<long, AssetStoreLibrary.LocalPackage> local = AssetStoreLibrary.LocalPackages();
            List<AssetStoreLibrary.Item> matches = Filter(category, search, downloaded, includeHidden, local);

            StringBuilder builder = new StringBuilder(Header(local)).Append('\n').Append(matches.Count).Append(" match");

            int shown = limit > 0 ? Math.Min(limit, matches.Count) : matches.Count;
            if (shown < matches.Count) builder.Append($", showing the first {shown} (raise limit for more)");

            foreach (AssetStoreLibrary.Item item in matches.Take(shown))
            {
                builder.Append('\n').Append(item.id).Append(" | ").Append(item.name).Append(" | ").Append(string.IsNullOrEmpty(item.category) ? "uncategorised" : item.category);
                builder.Append(" | ").Append(local.ContainsKey(item.id) ? "downloaded" : "not downloaded");

                if (item.hidden) builder.Append(" | hidden");
            }

            return builder.ToString();
        }

        [CliCommand("dn_store_download", "Download Asset Store packages into the local Asset Store cache, exactly like the Package Manager's Download button. Give product ids, or a category / search to download every match that is not downloaded yet. Runs in the background; poll dn_store_download_status")]
        private static string Download(
            [CliArg("ids", "Comma-separated product ids from dn_store_list")] string ids = "",
            [CliArg("category", "Download every not-yet-downloaded asset in this category and its subcategories")] string category = "",
            [CliArg("search", "Download every not-yet-downloaded asset whose name contains this text")] string search = "",
            [CliArg("all", "Download everything owned that is not downloaded yet. Can be many gigabytes")] bool all = false,
            [CliArg("redownload", "Download ids again even when they are already in the cache, e.g. to update them")] bool redownload = false)
        {
            if (Unready(out string message)) throw new InvalidOperationException(message);

            Dictionary<long, AssetStoreLibrary.LocalPackage> local = AssetStoreLibrary.LocalPackages();
            List<long> productIds = new List<long>();

            if (!string.IsNullOrWhiteSpace(ids))
            {
                foreach (string part in ids.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!long.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id))
                    {
                        throw new ArgumentException($"'{part}' is not a product id.");
                    }

                    if (redownload || !local.ContainsKey(id)) productIds.Add(id);
                }
            }
            else if (all || !string.IsNullOrWhiteSpace(category) || !string.IsNullOrWhiteSpace(search))
            {
                productIds.AddRange(Filter(category, search, "no", false, local).Select(item => item.id));
            }
            else
            {
                throw new ArgumentException("Give ids, a category, a search, or all=true.");
            }

            if (productIds.Count == 0)
            {
                return "Nothing to download: every matching asset is already in the download cache.";
            }

            if (!AssetStoreLibrary.Download(productIds, out string reason))
            {
                throw new InvalidOperationException(reason);
            }

            AssetStoreDownloads.Track(productIds);

            return $"Downloading {productIds.Count} package(s): {string.Join(", ", productIds.Select(Describe))}. Poll dn_store_download_status.";
        }

        [CliCommand("dn_store_download_status", "Report the progress of the Asset Store downloads started by dn_store_download, and where each finished package was saved")]
        private static string DownloadStatus()
        {
            IReadOnlyList<long> tracked = AssetStoreDownloads.Tracked;

            if (tracked.Count == 0)
            {
                return "No downloads have been started this session.";
            }

            Dictionary<long, AssetStoreLibrary.LocalPackage> local = AssetStoreLibrary.LocalPackages();
            StringBuilder builder = new StringBuilder();
            int done = 0;
            int failed = 0;

            foreach (long id in tracked)
            {
                AssetStoreLibrary.DownloadState state = AssetStoreLibrary.GetDownloadState(id);
                bool inCache = local.TryGetValue(id, out AssetStoreLibrary.LocalPackage package);

                builder.Append('\n').Append(Describe(id)).Append(": ");

                if (state != null && !string.IsNullOrEmpty(state.error))
                {
                    failed++;
                    builder.Append("failed (").Append(state.error).Append(')');
                }
                else if (state != null && state.inProgress)
                {
                    builder.Append(state.state).Append(' ').Append(state.progress.ToString("0%", CultureInfo.InvariantCulture));
                }
                else if (inCache)
                {
                    done++;
                    builder.Append("downloaded to ").Append(package.path);
                }
                else
                {
                    builder.Append(state?.state ?? "queued");
                }
            }

            return $"{done}/{tracked.Count} downloaded{(failed > 0 ? $", {failed} failed" : string.Empty)}:" + builder;
        }

        [CliCommand("dn_store_import", "Import an Asset Store package into the open project. It has to be downloaded first: dn_store_download, then wait for dn_store_download_status")]
        private static string Import(
            [CliArg("id", "Product id from dn_store_list")] long id,
            [CliArg("interactive", "Show Unity's import window to pick files instead of importing everything")] bool interactive = false)
        {
            Dictionary<long, AssetStoreLibrary.LocalPackage> local = AssetStoreLibrary.LocalPackages();

            if (!local.TryGetValue(id, out AssetStoreLibrary.LocalPackage package) || string.IsNullOrEmpty(package.path) || !File.Exists(package.path))
            {
                throw new InvalidOperationException($"{Describe(id)} is not downloaded yet. Download it with dn_store_download (ids: {id}), wait for dn_store_download_status, then import.");
            }

#if UNITY_6000_6_OR_NEWER
            UnityEditor.AssetPackage.Package.Import(package.path, interactive);
#else
            AssetDatabase.ImportPackage(package.path, interactive);
#endif

            return interactive
                ? $"Opened Unity's import window for {Describe(id)} ({package.path})."
                : $"Importing everything in {Describe(id)} from {package.path}. Unity imports and recompiles in the background.";
        }

        private static bool Unready(out string message)
        {
            message = null;

            if (!AssetStoreLibrary.IsAvailable(out string reason))
            {
                message = reason;
                return true;
            }

            if (AssetStoreLibrary.IsRefreshing)
            {
                message = $"The library is still being fetched: {AssetStoreLibrary.RefreshState}. Try again in a few seconds.";
                return true;
            }

            if (AssetStoreLibrary.Items.Count == 0)
            {
                string state = AssetStoreLibrary.RefreshState.StartsWith("failed", StringComparison.Ordinal) ? $" The last refresh {AssetStoreLibrary.RefreshState}." : string.Empty;

                message = $"The Asset Store library has not been fetched yet.{state} Run dn_store_refresh first.";
                return true;
            }

            return false;
        }

        private static string Header(Dictionary<long, AssetStoreLibrary.LocalPackage> local)
        {
            int downloaded = AssetStoreLibrary.Items.Count(item => local.ContainsKey(item.id));

            return $"{AssetStoreLibrary.Items.Count} owned, {downloaded} downloaded (library fetched {AssetStoreLibrary.FetchedAt}; dn_store_refresh to update)";
        }

        private static List<AssetStoreLibrary.Item> Filter(string category, string search, string downloaded, bool includeHidden, Dictionary<long, AssetStoreLibrary.LocalPackage> local)
        {
            string wanted = (downloaded ?? "all").Trim().ToLowerInvariant();

            if (wanted != "all" && wanted != "yes" && wanted != "no")
            {
                throw new ArgumentException($"downloaded must be all, yes or no, not '{downloaded}'.");
            }

            return AssetStoreLibrary.Items
                .Where(item => includeHidden || !item.hidden)
                .Where(item => string.IsNullOrWhiteSpace(category) || InCategory(item, category.Trim()))
                .Where(item => string.IsNullOrWhiteSpace(search) || (item.name ?? string.Empty).IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(item => wanted == "all" || (wanted == "yes") == local.ContainsKey(item.id))
                .ToList();
        }

        /// <summary>The category itself or anything under it, so "audio" takes in "audio/sound-fx/foley".</summary>
        private static bool InCategory(AssetStoreLibrary.Item item, string category)
        {
            if (string.IsNullOrEmpty(item.category)) return false;

            return item.category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                   item.category.StartsWith(category.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string Describe(long id)
        {
            AssetStoreLibrary.Item item = AssetStoreLibrary.Items.FirstOrDefault(entry => entry.id == id);

            return item != null ? $"{item.name} ({id})" : id.ToString(CultureInfo.InvariantCulture);
        }
    }
}
