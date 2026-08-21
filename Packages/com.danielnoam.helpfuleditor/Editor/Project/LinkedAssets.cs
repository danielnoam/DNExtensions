using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Marks folders that are meant to be symlinks with their real state. A symlink that has been
    /// replaced by an ordinary folder still looks completely normal in the Project window, and
    /// everything dropped into it silently stops syncing — this is what makes that visible.
    /// </summary>
    internal static class LinkedAssets
    {
        private const string SessionKey = "DNExtensions.HelpfulEditor.LinkedAssetsChecked";
        private const string AssetsPrefix = "Assets/";

        private static readonly Color LinkedColor = new Color(0.18f, 0.8f, 0.44f);
        private static readonly Color BrokenColor = new Color(0.91f, 0.3f, 0.24f);
        private static readonly Color BrokenRowTint = new Color(1f, 0f, 0f, 0.08f);

        private static readonly GUIContent BadgeContent = new GUIContent();

        private static GUIStyle _badgeStyle;

        /// <summary>What a tracked folder turned out to be on disk.</summary>
        private enum LinkState
        {
            Missing,
            RealFolder,
            Symlink
        }

        /// <summary>
        /// Cached per project change: every row asks about the same handful of folders.
        ///
        /// Existence is kept alongside the link state rather than asked separately. One probe of the
        /// directory answers both, and the row needs both — a folder that is not there draws nothing,
        /// a real one draws the broken badge — so splitting them meant a Directory.Exists on every
        /// repaint of every tracked row, uncached, beside a link check that was already cached.
        /// </summary>
        private static readonly Dictionary<string, LinkState> StateCache = new Dictionary<string, LinkState>();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;

            EditorApplication.delayCall += WarnAboutBrokenLinks;
        }

        private static void Invalidate() => StateCache.Clear();

        /// <summary>
        /// One report per editor session. A folder that has lost its link is a real problem, but it
        /// is not one that gets fixed by saying so on every domain reload.
        /// </summary>
        private static void WarnAboutBrokenLinks()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            ProjectSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled || !settings.linkedAssetsEnabled) return;

            foreach (string folder in settings.linkedAssetFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                if (StateOf(folder) != LinkState.RealFolder) continue;

                Debug.LogError($"[HelpfulEditor] Assets/{folder} is a real folder, not a symlink. " +
                               "Anything placed there will not reach the linked location. Recreate the link from Tools/DNExtensions/Linked Assets.");
            }
        }

        /// <summary>The tracked folder this row is, or null when the row is not one of them.</summary>
        public static string MatchFolder(string assetPath, ProjectSettings settings)
        {
            if (!settings.linkedAssetsEnabled || string.IsNullOrEmpty(assetPath)) return null;

            foreach (string folder in settings.linkedAssetFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                if (string.Equals(assetPath, AssetsPrefix + folder, StringComparison.OrdinalIgnoreCase)) return folder;
            }

            return null;
        }

        public static void Draw(Rect rowRect, string folder, bool isListView)
        {
            LinkState state = StateOf(folder);
            if (state == LinkState.Missing) return;

            bool linked = state == LinkState.Symlink;

            if (!linked) EditorGUI.DrawRect(rowRect, BrokenRowTint);

            if (Event.current.type != EventType.Repaint) return;

            EnsureStyle();
            _badgeStyle.normal.textColor = linked ? LinkedColor : BrokenColor;

            BadgeContent.text = linked ? "● LINKED" : "■ BROKEN";
            BadgeContent.tooltip = linked
                ? "Symlink. Anything placed here also lands in the linked location."
                : "A real folder, not a symlink. Nothing placed here is syncing. Delete it and recreate the link.";

            Rect badgeRect = GetBadgeRect(rowRect, folder, isListView);

            _badgeStyle.alignment = isListView ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            GUI.Label(badgeRect, BadgeContent, _badgeStyle);
        }

        /// <summary>
        /// List rows put the badge after the name; icon rows have no room beside it, so it goes in
        /// the top-left corner of the thumbnail instead.
        /// </summary>
        private static Rect GetBadgeRect(Rect rowRect, string folder, bool isListView)
        {
            if (!isListView) return new Rect(rowRect.x + 4f, rowRect.y + 4f, 64f, 16f);

            float nameWidth = HelpfulEditorGUI.LabelWidth(folder);
            return new Rect(rowRect.x + HelpfulEditorGUI.IndentWidth + nameWidth + 10f, rowRect.y, 72f, rowRect.height);
        }

        private static LinkState StateOf(string folder)
        {
            if (StateCache.TryGetValue(folder, out LinkState cached)) return cached;

            LinkState state = LinkState.Missing;

            try
            {
                DirectoryInfo info = new DirectoryInfo(FullPath(folder));

                if (info.Exists)
                {
                    state = (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint
                        ? LinkState.Symlink
                        : LinkState.RealFolder;
                }
            }
            catch (Exception)
            {
                // An unreadable folder is reported as missing rather than crashing the row draw,
                // which is what Directory.Exists answered for one too — it swallows the same faults.
            }

            StateCache[folder] = state;
            return state;
        }

        public static string FullPath(string folder) => Path.Combine(Application.dataPath, folder);

        private static void EnsureStyle()
        {
            _badgeStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
        }
    }
}
