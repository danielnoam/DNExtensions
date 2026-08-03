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

        /// <summary>Cached per project change: every row asks about the same handful of folders.</summary>
        private static readonly Dictionary<string, bool> SymlinkCache = new Dictionary<string, bool>();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectChanged -= Invalidate;
            EditorApplication.projectChanged += Invalidate;

            EditorApplication.delayCall += WarnAboutBrokenLinks;
        }

        private static void Invalidate() => SymlinkCache.Clear();

        /// <summary>
        /// One report per editor session. A folder that has lost its link is a real problem, but it
        /// is not one that gets fixed by saying so on every domain reload.
        /// </summary>
        private static void WarnAboutBrokenLinks()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled || !settings.linkedAssetsEnabled) return;

            foreach (string folder in settings.linkedAssetFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                if (!Directory.Exists(FullPath(folder))) continue;

                if (!IsSymlink(folder))
                {
                    Debug.LogError($"[HelpfulEditor] Assets/{folder} is a real folder, not a symlink. " +
                                   "Anything placed there will not reach the linked location. Recreate the link from Tools/DNExtensions/Linked Assets.");
                }
            }
        }

        /// <summary>The tracked folder this row is, or null when the row is not one of them.</summary>
        public static string MatchFolder(string assetPath, ProjectModuleSettings settings)
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
            if (!Directory.Exists(FullPath(folder))) return;

            bool linked = IsSymlink(folder);

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

            float nameWidth = EditorStyles.label.CalcSize(new GUIContent(folder)).x;
            return new Rect(rowRect.x + HelpfulEditorGUI.IndentWidth + nameWidth + 10f, rowRect.y, 72f, rowRect.height);
        }

        private static bool IsSymlink(string folder)
        {
            if (SymlinkCache.TryGetValue(folder, out bool cached)) return cached;

            bool result = false;

            try
            {
                DirectoryInfo info = new DirectoryInfo(FullPath(folder));
                result = info.Exists && (info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }
            catch (Exception)
            {
                // An unreadable folder is reported as unlinked rather than crashing the row draw.
            }

            SymlinkCache[folder] = result;
            return result;
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
