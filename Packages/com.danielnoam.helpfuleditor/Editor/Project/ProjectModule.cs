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
        private const float LabelIconGap = 2f;
        private const float LabelPadding = 6f;

        private static readonly List<Rect> IconRects = new List<Rect>();
        private static readonly GUIContent OverflowContent = new GUIContent();
        private static readonly GUIContent MeasureContent = new GUIContent();

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
        }

        /// <summary>
        /// Keeps the cached hover honest. Rows register themselves as interactive content while they
        /// draw, which is what gets the window repainted as the cursor moves across it — but nothing
        /// draws once the cursor has left, so leaving is noticed here. Where that registration is
        /// unavailable the repaints have to be driven from here instead, and a hover that stops
        /// being refreshed is then treated as stale rather than left latched on a row the cursor has
        /// long since left.
        /// </summary>
        private static void OnUpdate()
        {
            ProjectSettings settings = HelpfulEditorSettings.Project;
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

            if (HelpfulEditorGUI.HotRegionAvailable) return;

            if (EditorApplication.timeSinceStartup - _lastHoverTime > HoverTimeout) ClearHover();

            if (settings.hoverHighlightEnabled) window.Repaint();
        }

        /// <summary>
        /// Only rows that actually draw a foldout arrow need the elbow to stop short — an empty
        /// folder has nothing in that column, so its line should run to the icon like any other row.
        /// Assets count too: an FBX or prefab with sub-assets is an expandable row.
        ///
        /// Only reached when the tree's own rows could not be read; otherwise the tree is asked
        /// directly, which is both cheaper and exactly what it draws.
        /// </summary>
        private static bool HasFoldout(string path, bool isFolder)
        {
            ProjectCache.FolderEntry entry = ProjectCache.instance.GetOrCreate(path);

            if (!entry.foldoutKnown)
            {
                bool subfolders = isFolder && AssetDatabase.GetSubFolders(path).Length > 0;

                entry.hasSubfolders = subfolders;
                entry.hasChildren = isFolder
                    ? subfolders || FolderHasFiles(path)
                    : AssetDatabase.LoadAllAssetRepresentationsAtPath(path).Length > 0;
                entry.foldoutKnown = true;

                ProjectCache.instance.MarkDirty();
            }

            // The two-column folder tree lists nothing but folders, so only a subfolder puts an
            // arrow on a row there. The one-column tree lists assets as well, so a folder holding
            // only files gets one too — which is why the elbow was running into some arrows.
            return _hasListAreaRect ? entry.hasSubfolders : entry.hasChildren;
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

        private static void ClearHover()
        {
            if (HoveredPath == null) return;

            HoveredPath = null;
            EditorApplication.RepaintProjectWindow();
        }

        private static void OnProjectItem(string guid, Rect rowRect)
        {
            ProjectSettings settings = HelpfulEditorSettings.Project;
            if (!settings.moduleEnabled) return;

            bool newPass = BeginRow(rowRect, settings.treeLinesEnabled);

            // Structural rows such as the Packages root have no asset behind them. They still need
            // hover tracking and a highlight, so the path check gates only the overlays below.
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool hasAsset = !string.IsNullOrEmpty(path);

            bool isListView = rowRect.height <= ListViewRowHeightLimit;
            bool inListArea = IsInListArea(rowRect);
            bool hovered = TrackHover(path, rowRect, inListArea);

            // Registers the row as interactive so the editor repaints on mouse move by itself,
            // rather than the module having to repaint the window on a timer to keep up.
            if (settings.hoverHighlightEnabled) HelpfulEditorGUI.MarkInteractive(rowRect);

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

            DrawNavigationHighlight(rowRect, path);

            bool isFolder = AssetDatabase.IsValidFolder(path);

            int pathDepth = GetPathDepth(path);
            CalibrateTreeBase(path, pathDepth, rowRect);

            // Only the folder tree is a real tree. Right-pane rows are laid out by the browsed
            // folder's contents, not by nesting, so guides there would invent levels.
            bool treeRow = IsTreeRow(rowRect, pathDepth);

            if (settings.treeLinesEnabled && isListView && treeRow) DrawTreeLines(rowRect, path, isFolder, pathDepth, settings);

            // Sub-asset rows report their parent's guid, so the path here is the parent file's. The
            // overlay would draw the FBX's name and extension over a mesh or material row.
            bool subAsset = IsSubAssetRow(guid, rowRect, newPass);

            if (!subAsset) ProjectNameOverlay.Draw(rowRect, path, isListView, isFolder, settings);

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

        /// <summary>
        /// Rows arrive top to bottom, so a row that is not below the previous one starts a new pass.
        /// Both the sub-asset test and the tree topology are stateful across a pass and need to know
        /// where one begins.
        /// </summary>
        private static bool BeginRow(Rect rowRect, bool needsTopology)
        {
            bool newPass = rowRect.y <= _lastRowY;
            _lastRowY = rowRect.y;

            if (!newPass) return false;

            // Reading the tree means finding the window, so it is skipped entirely when nothing is
            // going to ask for the answer.
            if (needsTopology) ProjectTreeTopology.BeginPass();

            // Queued folds are released from here because starting one reaches into the tree's GUI
            // state, which only exists while the window is drawing. Layout rather than repaint: it
            // is the phase the tree expects its row set to change in, and the one a click on a
            // foldout arrow effectively uses.
            if (Event.current != null && Event.current.type == EventType.Layout) ProjectExpandQueue.Pump();

            return true;
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
        /// apart. The pass flag is what stops an expandable asset at the top of the list being
        /// compared against whatever was drawn last on the previous sweep.
        /// </summary>
        private static bool IsSubAssetRow(string guid, Rect rowRect, bool newPass)
        {
            if (!newPass && guid == _lastMainGuid && rowRect.x > _lastMainX) return true;

            _lastMainGuid = guid;
            _lastMainX = rowRect.x;
            return false;
        }

        /// <summary>
        /// Proper tree connectors where the tree's own rows can be read, which is what makes a last
        /// child terminate its guide instead of running it past the end of the branch. The depth-only
        /// fallback keeps the guides drawn, just without the terminating elbows.
        /// </summary>
        private static void DrawTreeLines(Rect rowRect, string path, bool isFolder, int pathDepth, ProjectSettings settings)
        {
            // The extra step drops the guide for the Assets root itself: its direct children sit at
            // the top level and get no line, exactly as the Hierarchy's scene roots do.
            int depth = Mathf.Max(0, pathDepth - 1);
            float leftEdge = rowRect.x - HelpfulEditorGUI.IndentWidth * depth;

            if (ProjectTreeTopology.TryGet(path, out IReadOnlyList<bool> lastOnPath, out bool hasChildren))
            {
                HelpfulEditorGUI.DrawTreeConnectors(rowRect, leftEdge, lastOnPath, settings.treeLineColor,
                    settings.treeLineStyle, hasChildren);
                return;
            }

            HelpfulEditorGUI.DrawDepthLines(rowRect, depth, leftEdge, settings.treeLineColor, settings.treeLineStyle,
                HasFoldout(path, isFolder));
        }

        /// <summary>Brief flash on the row a back/forward jump landed on, so the move is visible.</summary>
        private static void DrawNavigationHighlight(Rect rowRect, string path)
        {
            if (ProjectNavigationAnimator.HighlightPath != path) return;

            float amount = ProjectNavigationAnimator.HighlightAmount;
            if (amount <= 0f) return;

            float brightness = EditorGUIUtility.isProSkin ? 0.16f : 0.35f;

            EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, brightness * amount));
        }

        /// <summary>
        /// Right-aligned strip showing which asset types the folder holds, most common first. It is
        /// only allowed the space to the right of the row's own label — aligning it against the row
        /// alone puts icons on top of long folder names.
        /// </summary>
        private static void DrawFolderContentIcons(Rect rowRect, string path, ProjectSettings settings)
        {
            Texture[] icons = ProjectFolderContents.Get(path, settings.folderContentRecursive);
            if (icons.Length == 0) return;

            float labelEnd = rowRect.x + rowRect.height + LabelIconGap + LabelWidth(path) + LabelPadding;
            if (labelEnd >= rowRect.xMax) return;

            Rect area = Rect.MinMaxRect(labelEnd, rowRect.y, rowRect.xMax, rowRect.yMax);

            HelpfulEditorGUI.LayoutIconStrip(area, icons.Length, settings.folderContentIconSize,
                settings.folderContentMaxIcons, IconRects, out int shown, out Rect overflowRect);

            if (shown == 0) return;

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

        private static float LabelWidth(string path)
        {
            int slash = path.LastIndexOf('/');
            MeasureContent.text = slash >= 0 ? path.Substring(slash + 1) : path;

            return EditorStyles.label.CalcSize(MeasureContent).x;
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
