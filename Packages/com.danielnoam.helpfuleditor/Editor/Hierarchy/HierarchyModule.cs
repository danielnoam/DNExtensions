using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Owns every Hierarchy row overlay. All passes run from a single callback so their draw order
    /// is explicit rather than dependent on subscription order.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyModule
    {
        /// <summary>Width of the row's own object icon, which is where its label starts.</summary>
        internal const float IconWidth = 18f;
        private const float BadgeWidth = 26f;
        private const float IconBadgeSize = 11f;

        // All reused: these run for every visible row on every repaint.
        private static readonly List<Transform> AncestorBuffer = new List<Transform>();
        private static readonly List<bool> LastOnPathBuffer = new List<bool>();
        private static readonly GUIContent BadgeContent = new GUIContent();
        private static readonly string[] CachedCounts = BuildCountLabels();

        private static float _lastRowY;

        public static GameObject HoveredObject { get; private set; }

        /// <summary>Id of the hovered row, which is set for scene headers too — unlike HoveredObject.</summary>
        public static object HoveredRawId { get; private set; }

        static HierarchyModule()
        {
            HelpfulEditorHooks.HierarchyItem -= OnHierarchyItem;
            HelpfulEditorHooks.HierarchyItem += OnHierarchyItem;

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// The hover cache is only written while rows are drawn, so moving the cursor out of the
        /// window would otherwise leave the last row latched — and a keybind pressed elsewhere would
        /// act on it. No staleness timeout here, unlike the Project window: the Hierarchy is not
        /// force-repainted, so a cursor resting on a row would time itself out.
        /// </summary>
        private static void OnUpdate()
        {
            // Nothing below draws or tracks anything while the module is off, and the repaint it
            // would otherwise force runs on every editor tick the cursor spends over the window.
            if (!HelpfulEditorSettings.Hierarchy.moduleEnabled) return;

            // Only acts on positive evidence: mouseOverWindow is null whenever the editor cannot
            // say where the cursor is, and treating that as "left the window" would clear the hover
            // between ticks and leave the keybinds with nothing to act on.
            if (!EditorWindow.mouseOverWindow) return;

            if (HelpfulEditorWindows.MouseOverHierarchy)
            {
                // The component strip's hover lift and its link cursor are both only established
                // while a row is being drawn, and the Hierarchy does not repaint as the pointer
                // moves across it. Rows ask the editor to by marking themselves interactive; only
                // where that is unavailable does the repaint have to be driven from here, which is
                // the polling fallback HelpfulEditorGUI.MarkInteractive describes. Forcing one
                // every tick regardless made resting the cursor over the Hierarchy a continuous
                // full redraw of every visible row.
                if (!HelpfulEditorGUI.HotRegionAvailable) EditorApplication.RepaintHierarchyWindow();

                return;
            }

            HierarchyComponentStrip.DiscardPendingQuickEdit();
            ClearHover();
        }

        private static void ClearHover()
        {
            if (HoveredRawId == null) return;

            HoveredObject = null;
            HoveredRawId = null;
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnHierarchyItem(object rawId, Object item, Rect rowRect)
        {
            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
            if (!settings.moduleEnabled) return;

            BeginRow(rowRect);

            HierarchyComponentStrip.ProcessPendingQuickEdit();

            GameObject gameObject = item as GameObject;
            bool hovered = TrackHover(rawId, gameObject, rowRect);

            // Registers the row as interactive so the editor repaints on mouse move by itself. The
            // whole row, not just the strip inside it: the hover state gates the zebra stripe and
            // the child-count tooltip as well, so anywhere on the row is somewhere a move matters.
            HelpfulEditorGUI.MarkInteractive(HelpfulEditorGUI.FullRowRect(rowRect));

            bool selected = gameObject && Selection.Contains(gameObject);

            // Unity tints the row under the cursor by lightening it, which is exactly what a light
            // stripe looks like. Leaving the hovered row alone keeps the two readable apart.
            if (settings.zebraStripesEnabled && !selected && !hovered)
            {
                HelpfulEditorGUI.DrawZebra(rowRect, settings.zebraColorEven, settings.zebraColorOdd, settings.zebraOpacity);
            }

            if (!gameObject)
            {
                // Rows with no object are scene headers, which is the one place the scene menu belongs.
                if (settings.sceneMenuEnabled) HierarchySceneMenu.Draw(rawId, rowRect, hovered);

                return;
            }

            if (settings.treeLinesEnabled)
            {
                DrawTreeLines(rowRect, gameObject.transform, settings);
            }

            float rightEdge = rowRect.xMax;

            if (settings.componentStripEnabled)
            {
                float stripWidth = HierarchyComponentStrip.Draw(rowRect, gameObject, settings);
                if (stripWidth > 0f) rightEdge -= stripWidth + 4f;
            }

            if (settings.childCountEnabled)
            {
                DrawChildCount(gameObject, rowRect, rightEdge, settings, hovered);
            }
        }

        /// <summary>
        /// Fills in which node the path passes through at each level and hands the drawing to the
        /// shared connector routine, which the Project window feeds from its own tree rows.
        /// </summary>
        private static void DrawTreeLines(Rect rowRect, Transform transform, HierarchySettings settings)
        {
            Color color = settings.treeLineColor;
            if (color.a <= 0f) return;

            AncestorBuffer.Clear();
            for (Transform parent = transform.parent; parent; parent = parent.parent)
            {
                AncestorBuffer.Add(parent);
            }

            int depth = AncestorBuffer.Count;
            if (depth == 0) return;

            AncestorBuffer.Reverse();

            LastOnPathBuffer.Clear();
            for (int level = 0; level < depth; level++)
            {
                LastOnPathBuffer.Add(IsLastSibling(level + 1 < depth ? AncestorBuffer[level + 1] : transform));
            }

            // A row with children owns the indent step left of its icon — that is where Unity draws
            // the foldout arrow — so the elbow stops at the arrow's edge rather than running
            // underneath the glyph.
            HelpfulEditorGUI.DrawTreeConnectors(rowRect, rowRect.x - HelpfulEditorGUI.IndentWidth * depth,
                LastOnPathBuffer, color, settings.treeLineStyle, transform.childCount > 0);
        }

        /// <summary>
        /// Rows arrive top to bottom, so a row that is not below the previous one starts a new pass.
        /// Queued folds are released here because starting one reaches into the tree's GUI state,
        /// which only exists while the window is drawing — and on layout rather than repaint, since
        /// that is the phase the tree expects its row set to change in.
        /// </summary>
        private static void BeginRow(Rect rowRect)
        {
            bool newPass = rowRect.y <= _lastRowY;
            _lastRowY = rowRect.y;

            if (!newPass) return;
            if (Event.current == null || Event.current.type != EventType.Layout) return;

            HierarchyExpandQueue.Pump();
        }

        private static bool IsLastSibling(Transform transform)
        {
            Transform parent = transform.parent;
            if (!parent) return true;

            return transform.GetSiblingIndex() >= parent.childCount - 1;
        }

        private static void DrawChildCount(GameObject gameObject, Rect rowRect, float rightEdge, HierarchySettings settings, bool hovered)
        {
            int count = gameObject.transform.childCount;
            if (count == 0) return;
            if (settings.childCountHideWhenOneOrZero && count <= 1) return;

            BadgeContent.text = CountLabel(count);
            BadgeContent.tooltip = hovered ? BuildTooltip(gameObject.transform, count) : string.Empty;

            if (settings.childCountPosition == BadgePosition.OnIcon)
            {
                DrawIconBadge(rowRect, BadgeContent);
                return;
            }

            Rect badgeRect;
            if (settings.childCountPosition == BadgePosition.LeftOfName)
            {
                float nameWidth = HelpfulEditorGUI.LabelWidth(gameObject.name);
                badgeRect = new Rect(rowRect.x + IconWidth + nameWidth + 4f, rowRect.y, BadgeWidth, rowRect.height);
            }
            else
            {
                badgeRect = new Rect(rightEdge - BadgeWidth, rowRect.y, BadgeWidth, rowRect.height);
            }

            if (badgeRect.xMax > rowRect.xMax || badgeRect.x < rowRect.x) return;

            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, 0.6f);
            GUI.Label(badgeRect, BadgeContent, HelpfulEditorGUI.BadgeStyle);
            GUI.color = previous;
        }

        /// <summary>
        /// Built only for the row under the cursor. The recursive walk is the expensive half, but
        /// even the string itself is worth skipping — this runs for every visible row on every
        /// repaint, and only one row's tooltip can ever be shown.
        /// </summary>
        private static string BuildTooltip(Transform transform, int directCount)
        {
            int total = CountDescendants(transform);
            if (total == directCount) return $"{directCount} direct child object(s)";

            return $"{directCount} direct child object(s)\n{total} total in subtree";
        }

        private static string CountLabel(int count)
        {
            return count < CachedCounts.Length ? CachedCounts[count] : count.ToString();
        }

        private static string[] BuildCountLabels()
        {
            string[] labels = new string[100];
            for (int i = 0; i < labels.Length; i++) labels[i] = i.ToString();

            return labels;
        }

        private static int CountDescendants(Transform transform)
        {
            int total = transform.childCount;

            for (int i = 0; i < transform.childCount; i++)
            {
                total += CountDescendants(transform.GetChild(i));
            }

            return total;
        }

        /// <summary>Corner badge tucked into the object icon itself, backed so it stays legible over any icon.</summary>
        private static void DrawIconBadge(Rect rowRect, GUIContent content)
        {
            Rect iconRect = new Rect(rowRect.x, rowRect.y, rowRect.height, rowRect.height);
            Rect badgeRect = new Rect(iconRect.xMax - IconBadgeSize, iconRect.yMax - IconBadgeSize, IconBadgeSize, IconBadgeSize);

            EditorGUI.DrawRect(badgeRect, HelpfulEditorGUI.WindowBackground);
            GUI.Label(badgeRect, content, HelpfulEditorGUI.BadgeStyle);
        }

        private static bool TrackHover(object rawId, GameObject gameObject, Rect rowRect)
        {
            Event evt = Event.current;
            if (evt == null) return false;
            if (!HelpfulEditorGUI.FullRowRect(rowRect).Contains(evt.mousePosition)) return false;

            HoveredObject = gameObject;
            HoveredRawId = rawId;
            return true;
        }
    }
}
