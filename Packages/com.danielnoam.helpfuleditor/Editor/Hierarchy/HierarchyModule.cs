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
        private const float IconWidth = 18f;
        private const float BadgeWidth = 26f;
        private const float IconBadgeSize = 11f;

        // All reused: these run for every visible row on every repaint.
        private static readonly List<Transform> AncestorBuffer = new List<Transform>();
        private static readonly GUIContent BadgeContent = new GUIContent();
        private static readonly GUIContent MeasureContent = new GUIContent();
        private static readonly string[] CachedCounts = BuildCountLabels();

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
            // Only acts on positive evidence: mouseOverWindow is null whenever the editor cannot
            // say where the cursor is, and treating that as "left the window" would clear the hover
            // between ticks and leave the keybinds with nothing to act on.
            if (!EditorWindow.mouseOverWindow || HelpfulEditorWindows.MouseOverHierarchy) return;

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

            HierarchyComponentStrip.ProcessPendingQuickEdit();

            GameObject gameObject = item as GameObject;
            bool hovered = TrackHover(rawId, gameObject, rowRect);

            bool selected = gameObject && Selection.Contains(gameObject);

            // Unity tints the row under the cursor by lightening it, which is exactly what a light
            // stripe looks like. Leaving the hovered row alone keeps the two readable apart.
            if (settings.zebraStripesEnabled && !selected && !hovered)
            {
                HelpfulEditorGUI.DrawZebra(rowRect, settings.zebraColorEven, settings.zebraColorOdd, settings.zebraOpacity);
            }

            if (!gameObject) return;

            if (settings.treeDepthLinesEnabled)
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
        /// Classic tree connectors: one vertical guide per ancestor level, drawn only while that
        /// ancestor still has rows below it, and an elbow into each row's icon. A last child
        /// terminates its guide at the elbow instead of running it to the bottom of the row.
        /// </summary>
        private static void DrawTreeLines(Rect rowRect, Transform transform, HierarchySettings settings)
        {
            Color color = settings.treeDepthLineColor;
            if (color.a <= 0f) return;

            AncestorBuffer.Clear();
            for (Transform parent = transform.parent; parent; parent = parent.parent)
            {
                AncestorBuffer.Add(parent);
            }

            List<Transform> ancestors = AncestorBuffer;
            int depth = ancestors.Count;
            if (depth == 0) return;

            ancestors.Reverse();

            float leftEdge = rowRect.x - HelpfulEditorGUI.IndentWidth * depth;
            float midY = HelpfulEditorGUI.SnapToPixel(rowRect.y + rowRect.height * 0.5f);

            for (int level = 0; level < depth; level++)
            {
                Transform onPath = level + 1 < depth ? ancestors[level + 1] : transform;
                bool lastSibling = IsLastSibling(onPath);
                float x = HelpfulEditorGUI.GuideColumnX(leftEdge, level);

                const float thickness = HelpfulEditorGUI.TreeLineThickness;
                float weight = HelpfulEditorGUI.LineThickness(thickness);

                if (level < depth - 1)
                {
                    if (!lastSibling) HelpfulEditorGUI.DrawVerticalLine(x, rowRect.y, rowRect.yMax, color, settings.treeDepthLineStyle, thickness, midY);
                    continue;
                }

                // A terminating guide runs to the far side of the elbow so it owns the corner
                // outright; the horizontal then starts past it. Overlapping the two would draw a
                // translucent colour twice and leave a dark notch at the join.
                float bottom = lastSibling ? midY + weight : rowRect.yMax;
                HelpfulEditorGUI.DrawVerticalLine(x, rowRect.y, bottom, color, settings.treeDepthLineStyle, thickness, midY);

                // A row with children owns the indent step left of its icon — that is where Unity
                // draws the foldout arrow — so the elbow stops at the arrow's edge rather than
                // running underneath the glyph.
                float elbowEnd = transform.childCount > 0
                    ? rowRect.x - HelpfulEditorGUI.IndentWidth
                    : rowRect.x - 2f;

                float elbowStart = HelpfulEditorGUI.ElbowStart(x, thickness, settings.treeDepthLineStyle);
                HelpfulEditorGUI.DrawHorizontalLine(elbowStart, elbowEnd, midY, color, settings.treeDepthLineStyle, thickness);
            }
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
                MeasureContent.text = gameObject.name;
                float nameWidth = EditorStyles.label.CalcSize(MeasureContent).x;
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
