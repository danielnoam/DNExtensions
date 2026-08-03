using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Owns every Project window row overlay. Like the Hierarchy module, all passes run from a
    /// single callback so their draw order is explicit.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectModule
    {
        private const float ListViewRowHeightLimit = 20f;
        private const double HoverTimeout = 0.25;

        // Rebuilt whenever assets change. These lookups allocate and touch disk, and this runs per
        // row per repaint, so results are kept until the project changes.
        private static readonly Dictionary<string, (bool subfolders, bool children)> FoldoutCache =
            new Dictionary<string, (bool, bool)>();

        private static readonly List<Rect> IconRects = new List<Rect>();
        private static readonly GUIContent OverflowContent = new GUIContent();

        private static double _lastHoverTime;
        private static Rect _listAreaRect;
        private static bool _hasListAreaRect;
        private static string _lastMainGuid;
        private static float _lastMainX;
        private static float _lastRowY;
        private static float _treeBaseX;
        private static bool _hasTreeBaseX;

        public static string HoveredPath { get; private set; }

        /// <summary>Whether the hovered row was in the two-column right pane rather than the folder tree.</summary>
        public static bool HoveredInListArea { get; private set; }

        /// <summary>Top of the hovered row, used to reach rows that have no asset path of their own.</summary>
        public static float HoveredRowY { get; private set; }

        static ProjectModule()
        {
            HelpfulEditorHooks.ProjectItem -= OnProjectItem;
            HelpfulEditorHooks.ProjectItem += OnProjectItem;

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        /// <summary>
        /// Unity only repaints the Project window when it has a reason to, and it has no hover state
        /// of its own to trigger one — so the hover cache would only refresh whenever something else
        /// happened to redraw, which is the lag. Driving a repaint while the cursor is inside the
        /// window keeps the highlight immediate, and dropping a hover that has gone stale keeps
        /// keybinds from acting on a row the cursor has already left.
        /// </summary>
        private static void OnUpdate()
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled) return;

            // Captured once: mouseOverWindow is re-evaluated on every read and can go null between
            // the guard and the repaint. A null reading means the editor cannot say where the cursor
            // is, not that it left, so the hover is only dropped on positive evidence.
            EditorWindow window = EditorWindow.mouseOverWindow;
            if (!window) return;

            if (!HelpfulEditorWindows.MouseOverProject)
            {
                ClearHover();
                return;
            }

            ProjectFolderHistory.RecordCurrentFolder();

            _hasListAreaRect = HelpfulEditorTreeReflection.TryGetProjectListAreaRect(out _listAreaRect);

            if (EditorApplication.timeSinceStartup - _lastHoverTime > HoverTimeout) ClearHover();

            if (settings.hoverHighlightEnabled) window.Repaint();
        }

        /// <summary>
        /// Only rows that actually draw a foldout arrow need the elbow to stop short — an empty
        /// folder has nothing in that column, so its line should run to the icon like any other row.
        /// Assets count too: an FBX or prefab with sub-assets is an expandable row.
        /// </summary>
        private static bool HasFoldout(string path, bool isFolder)
        {
            if (!FoldoutCache.TryGetValue(path, out (bool subfolders, bool children) info))
            {
                bool subfolders = isFolder && AssetDatabase.GetSubFolders(path).Length > 0;

                info = isFolder
                    ? (subfolders, subfolders || FolderHasFiles(path))
                    : (false, AssetDatabase.LoadAllAssetRepresentationsAtPath(path).Length > 0);

                FoldoutCache[path] = info;
            }

            // The two-column folder tree lists nothing but folders, so only a subfolder puts an
            // arrow on a row there. The one-column tree lists assets as well, so a folder holding
            // only files gets one too — which is why the elbow was running into some arrows.
            return _hasListAreaRect ? info.subfolders : info.children;
        }

        private static bool FolderHasFiles(string path)
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot)) return false;

                string fullPath = Path.Combine(projectRoot, path);
                if (!Directory.Exists(fullPath)) return false;

                foreach (string file in Directory.EnumerateFiles(fullPath))
                {
                    if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch (Exception)
            {
                // An unreadable folder simply gets no arrow assumption.
            }

            return false;
        }

        private static void OnProjectChanged() => FoldoutCache.Clear();

        private static void ClearHover()
        {
            if (HoveredPath == null) return;

            HoveredPath = null;
            EditorApplication.RepaintProjectWindow();
        }

        private static void OnProjectItem(string guid, Rect rowRect)
        {
            ProjectModuleSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled) return;

            // Structural rows such as the Packages root have no asset behind them. They still need
            // hover tracking and a highlight, so the path check gates only the overlays below.
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool hasAsset = !string.IsNullOrEmpty(path);

            bool isListView = rowRect.height <= ListViewRowHeightLimit;
            bool inListArea = IsInListArea(rowRect);
            bool hovered = TrackHover(path, rowRect, inListArea);

            ProjectKeybinds.HandleRowInput(path, rowRect);

            bool selected = Array.IndexOf(Selection.assetGUIDs, guid) >= 0;

            if (!selected)
            {
                // Unlike the Hierarchy, Unity draws no hover tint here, so the highlight is ours and
                // it replaces the stripe on that row rather than stacking with it.
                if (hovered && settings.hoverHighlightEnabled)
                {
                    Color hover = settings.hoverColor;
                    hover.a *= settings.hoverOpacity;
                    if (hover.a > 0f) EditorGUI.DrawRect(rowRect, hover);
                }
                else if (!hovered && settings.zebraStripesEnabled && isListView)
                {
                    HelpfulEditorGUI.DrawZebra(rowRect, settings.zebraColorEven, settings.zebraColorOdd, settings.zebraOpacity, false);
                }
            }

            if (!hasAsset) return;

            bool isFolder = AssetDatabase.IsValidFolder(path);

            int pathDepth = GetPathDepth(path);
            CalibrateTreeBase(path, pathDepth, rowRect);

            // Only the folder tree is a real tree. Right-pane rows are laid out by the browsed
            // folder's contents, not by nesting, so guides there would invent levels.
            if (settings.treeLinesEnabled && isListView && IsTreeRow(rowRect, pathDepth))
            {
                // The extra step drops the guide for the Assets root itself: its direct children sit
                // at the top level and get no line, exactly as the Hierarchy's scene roots do.
                int depth = Mathf.Max(0, pathDepth - 1);
                HelpfulEditorGUI.DrawDepthLines(rowRect, depth, rowRect.x - HelpfulEditorGUI.IndentWidth * depth,
                    settings.treeLineColor, settings.treeLineStyle, HasFoldout(path, isFolder));
            }

            // Sub-asset rows report their parent's guid, so the path here is the parent file's. The
            // overlay would draw the FBX's name and extension over a mesh or material row.
            bool subAsset = IsSubAssetRow(guid, rowRect);

            if (!subAsset) ProjectNameOverlay.Draw(rowRect, path, isListView, isFolder, settings);

            // Tested by indent rather than by the list area rect: that rect and the row rects are not
            // in the same coordinate space, so only some right-pane rows ever matched it — which is
            // why turning the object view off left the icons showing.
            bool treeRow = IsTreeRow(rowRect, pathDepth);

            if (settings.folderContentIconsEnabled && isFolder && isListView && !subAsset &&
                (treeRow || settings.folderContentIconsInObjectView))
            {
                DrawFolderContentIcons(rowRect, path, settings);
            }

            if (isFolder && !subAsset)
            {
                string linkedFolder = LinkedAssets.MatchFolder(path, settings);
                if (linkedFolder != null) LinkedAssets.Draw(rowRect, linkedFolder, isListView);
            }
        }

        private static bool TrackHover(string path, Rect rowRect, bool inListArea)
        {
            Event evt = Event.current;
            if (evt == null || !rowRect.Contains(evt.mousePosition)) return false;

            HoveredPath = path;
            HoveredInListArea = inListArea;
            HoveredRowY = rowRect.y;
            _lastHoverTime = EditorApplication.timeSinceStartup;
            return true;
        }

        /// <summary>
        /// Unity hands the callback a guid, and every sub-asset of a file reports the file's own
        /// guid — an expanded FBX draws one row per mesh, all claiming to be the FBX. Sub-asset rows
        /// always follow their parent and sit one indent step deeper, which is enough to tell them
        /// apart without needing to know where a repaint pass begins.
        /// </summary>
        private static bool IsSubAssetRow(string guid, Rect rowRect)
        {
            // Rows arrive top to bottom, so a row that is not below the previous one starts a new
            // pass. Without this the first row of a pass is compared against the last row of the one
            // before, and an expandable asset sitting at the top of the list reads as a sub-asset of
            // whatever happened to be drawn last.
            bool newPass = rowRect.y <= _lastRowY;
            _lastRowY = rowRect.y;

            if (!newPass && guid == _lastMainGuid && rowRect.x > _lastMainX) return true;

            _lastMainGuid = guid;
            _lastMainX = rowRect.x;
            return false;
        }

        /// <summary>
        /// Right-aligned strip showing which asset types the folder holds, most common first.
        /// </summary>
        private static void DrawFolderContentIcons(Rect rowRect, string path, ProjectModuleSettings settings)
        {
            Texture[] icons = ProjectFolderContents.Get(path, settings.folderContentRecursive);
            if (icons.Length == 0) return;

            float size = settings.folderContentIconSize;
            int visible = settings.folderContentMaxIcons > 0
                ? Mathf.Min(icons.Length, settings.folderContentMaxIcons)
                : icons.Length;

            float width = visible * (size + 1f);
            if (visible < icons.Length) width += size + 6f;

            Rect area = new Rect(rowRect.xMax - width, rowRect.y, width, rowRect.height);
            HelpfulEditorGUI.LayoutIconStrip(area, icons.Length, size, settings.folderContentMaxIcons,
                IconRects, out int shown, out Rect overflowRect);

            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * HelpfulEditorGUI.IconStripOpacity);

            for (int i = 0; i < shown; i++)
            {
                if (icons[i]) GUI.DrawTexture(IconRects[i], icons[i], ScaleMode.ScaleToFit);
            }

            if (shown < icons.Length)
            {
                OverflowContent.text = $"+{icons.Length - shown}";
                GUI.Label(overflowRect, OverflowContent, HelpfulEditorGUI.BadgeStyle);
            }

            GUI.color = previous;
        }

        /// <summary>
        /// Records where a root row ("Assets", "Packages") sits, giving the tree's left edge without
        /// assuming one. Comparing m_ListAreaRect against row rects is not reliable for this — they
        /// are not in the same coordinate space, so only some right-pane rows ever matched.
        /// </summary>
        private static void CalibrateTreeBase(string path, int pathDepth, Rect rowRect)
        {
            if (pathDepth != 0) return;

            _treeBaseX = rowRect.x;
            _hasTreeBaseX = true;
        }

        /// <summary>
        /// A row belongs to the tree when its indent matches its nesting depth. Right-pane rows show
        /// the contents of one folder, so they all sit at the root indent while their paths are
        /// several levels deep — the mismatch is what rules them out.
        /// </summary>
        private static bool IsTreeRow(Rect rowRect, int pathDepth)
        {
            if (!_hasTreeBaseX) return false;

            float expectedX = _treeBaseX + HelpfulEditorGUI.IndentWidth * pathDepth;
            return Mathf.Abs(rowRect.x - expectedX) < HelpfulEditorGUI.IndentWidth * 0.5f;
        }

        /// <summary>Nesting level of an asset path: "Assets" is 0, "Assets/Foo" is 1, and so on.</summary>
        private static int GetPathDepth(string path)
        {
            int depth = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '/') depth++;
            }

            return depth;
        }

        private static bool IsInListArea(Rect rowRect)
        {
            return _hasListAreaRect && _listAreaRect.Overlaps(rowRect);
        }
    }
}
