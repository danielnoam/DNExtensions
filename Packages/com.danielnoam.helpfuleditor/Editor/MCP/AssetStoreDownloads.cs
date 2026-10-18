using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;

namespace DNExtensions.HelpfulEditor.MCP
{
    /// <summary>
    /// Which products dn_store_download has asked for this session, so the status command can report
    /// on exactly those. Kept in SessionState because an import or a script change reloads the domain
    /// while downloads are still running, and the Package Manager carries on regardless.
    /// </summary>
    internal static class AssetStoreDownloads
    {
        private const string TrackedKey = "DNExtensions.HelpfulEditor.MCP.AssetStoreDownloads";

        public static IReadOnlyList<long> Tracked
        {
            get
            {
                string stored = SessionState.GetString(TrackedKey, string.Empty);

                return stored.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => long.Parse(part, CultureInfo.InvariantCulture))
                    .ToList();
            }
        }

        public static void Track(IEnumerable<long> productIds)
        {
            List<long> tracked = Tracked.ToList();

            foreach (long id in productIds)
            {
                if (!tracked.Contains(id)) tracked.Add(id);
            }

            SessionState.SetString(TrackedKey, string.Join(",", tracked.Select(id => id.ToString(CultureInfo.InvariantCulture))));
        }
    }
}
