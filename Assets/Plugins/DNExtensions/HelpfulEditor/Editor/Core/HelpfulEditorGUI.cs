using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>Drawing primitives shared by the Hierarchy and Project row overlays.</summary>
    internal static class HelpfulEditorGUI
    {
        public const float IndentWidth = 14f;

        private const float DashSegment = 2f;
        private const float DashGap = 2f;

        private static readonly Dictionary<Type, Texture> IconCache = new Dictionary<Type, Texture>();
        private static GUIStyle _badgeStyle;

        public static GUIStyle BadgeStyle
        {
            get
            {
                if (_badgeStyle == null)
                {
                    _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }

                return _badgeStyle;
            }
        }

        public static Color WindowBackground => EditorGUIUtility.isProSkin
            ? new Color(0.22f, 0.22f, 0.22f)
            : new Color(0.76f, 0.76f, 0.76f);

        /// <summary>Expands a row's selection rect to span the full window width so backgrounds cover the whole row.</summary>
        public static Rect FullRowRect(Rect rowRect)
        {
            float right = Mathf.Max(rowRect.xMax, EditorGUIUtility.currentViewWidth);
            return new Rect(0f, rowRect.y, right, rowRect.height);
        }

        private static int RowIndex(Rect rowRect)
        {
            float height = Mathf.Max(1f, rowRect.height);
            return Mathf.FloorToInt(rowRect.y / height);
        }

        /// <param name="fullWidth">
        /// True for the Hierarchy, where the row owns the whole window width. False for the Project
        /// window, where widening the stripe would bleed across the pane divider.
        /// </param>
        public static void DrawZebra(Rect rowRect, Color even, Color odd, float opacity, bool fullWidth = true)
        {
            if (opacity <= 0f) return;

            Color color = RowIndex(rowRect) % 2 == 0 ? even : odd;
            color.a *= opacity;
            if (color.a <= 0f) return;

            EditorGUI.DrawRect(fullWidth ? FullRowRect(rowRect) : rowRect, color);
        }

        /// <summary>
        /// Centre of the guide column for an ancestor level. The guides sit in the foldout gutter
        /// half an indent step left of the level's content, which is where Unity draws the arrow —
        /// putting them on the content column instead makes them cut through icons and labels.
        /// </summary>
        public static float GuideColumnX(float leftEdge, int level)
        {
            return SnapToPixel(leftEdge + IndentWidth * level - IndentWidth * 0.5f);
        }

        /// <summary>
        /// Rounds a line's cross-axis coordinate to a whole pixel. A 1px rect sitting on a half
        /// pixel gets blended across two device pixels and reads as a fainter, thinner line than one
        /// that happens to land on a boundary — which is what made the vertical guides look lighter
        /// than the horizontal ones.
        /// </summary>
        public static float SnapToPixel(float value)
        {
            return Mathf.Round(value);
        }

        /// <summary>
        /// Full-height guide per ancestor level plus an elbow into the row's icon. Used by the
        /// Project window, which has no cheap way to know whether a row is its parent's last child;
        /// the Hierarchy draws proper terminating elbows instead.
        /// </summary>
        /// <param name="stopBeforeFoldout">
        /// True for rows that draw their own foldout arrow, so the elbow ends short of the arrow's
        /// column instead of running underneath the glyph.
        /// </param>
        public static void DrawDepthLines(Rect rowRect, int depth, float leftEdge, Color color, LineStyle style, bool stopBeforeFoldout, float thickness = 1f)
        {
            if (depth <= 0 || color.a <= 0f) return;

            for (int level = 0; level < depth; level++)
            {
                DrawVerticalLine(GuideColumnX(leftEdge, level), rowRect.y, rowRect.yMax, color, style, thickness);
            }

            float midY = SnapToPixel(rowRect.y + rowRect.height * 0.5f);
            float elbowEnd = stopBeforeFoldout ? rowRect.x - IndentWidth : rowRect.x - 2f;

            DrawHorizontalLine(GuideColumnX(leftEdge, depth - 1), elbowEnd, midY, color, style, thickness);
        }

        public static void DrawVerticalLine(float x, float top, float bottom, Color color, LineStyle style, float thickness = 1f)
        {
            if (bottom <= top) return;

            thickness = Mathf.Max(1f, Mathf.Round(thickness));

            if (style == LineStyle.Solid)
            {
                EditorGUI.DrawRect(new Rect(x, top, thickness, bottom - top), color);
                return;
            }

            for (float y = top; y < bottom; y += DashSegment + DashGap)
            {
                EditorGUI.DrawRect(new Rect(x, y, thickness, Mathf.Min(DashSegment, bottom - y)), color);
            }
        }

        public static void DrawHorizontalLine(float left, float right, float y, Color color, LineStyle style, float thickness = 1f)
        {
            if (right <= left) return;

            thickness = Mathf.Max(1f, Mathf.Round(thickness));

            if (style == LineStyle.Solid)
            {
                EditorGUI.DrawRect(new Rect(left, y, right - left, thickness), color);
                return;
            }

            for (float x = left; x < right; x += DashSegment + DashGap)
            {
                EditorGUI.DrawRect(new Rect(x, y, Mathf.Min(DashSegment, right - x), thickness), color);
            }
        }

        public static void DrawBorder(Rect rect, Color color, float thickness = 1f)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static Texture GetIcon(Component component)
        {
            if (!component) return null;

            Type type = component.GetType();
            if (IconCache.TryGetValue(type, out Texture cached) && cached) return cached;

            GUIContent content = EditorGUIUtility.ObjectContent(component, type);
            Texture icon = content?.image;
            IconCache[type] = icon;
            return icon;
        }

        private static bool IsExcluded(Component component, List<string> excludedTypeNames)
        {
            if (!component) return true;
            if (excludedTypeNames == null || excludedTypeNames.Count == 0) return false;

            Type type = component.GetType();
            foreach (string excluded in excludedTypeNames)
            {
                if (string.IsNullOrEmpty(excluded)) continue;
                if (string.Equals(type.Name, excluded, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(type.FullName, excluded, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>Components of a GameObject in inspector order, minus excluded types and missing scripts.</summary>
        public static List<Component> GetDisplayComponents(GameObject gameObject, List<string> excludedTypeNames)
        {
            List<Component> result = new List<Component>();
            GetDisplayComponents(gameObject, excludedTypeNames, result);
            return result;
        }

        /// <summary>
        /// Buffer-filling overload for the per-row callers. Both the returned list and the array
        /// from GetComponents&lt;T&gt;() allocate, and the Hierarchy runs this for every visible row on
        /// every repaint — enough churn to be worth avoiding.
        /// </summary>
        public static void GetDisplayComponents(GameObject gameObject, List<string> excludedTypeNames, List<Component> buffer)
        {
            buffer.Clear();
            if (!gameObject) return;

            gameObject.GetComponents(buffer);

            for (int i = buffer.Count - 1; i >= 0; i--)
            {
                if (!buffer[i] || IsExcluded(buffer[i], excludedTypeNames)) buffer.RemoveAt(i);
            }
        }

        /// <summary>
        /// Lays out right-aligned icon slots. Returns the rect of each slot, plus an overflow rect
        /// when the list is truncated.
        /// </summary>
        public static void LayoutIconStrip(Rect area, int count, float iconSize, int maxIcons, List<Rect> buffer, out int shown, out Rect overflowRect)
        {
            const float spacing = 1f;
            shown = maxIcons > 0 ? Mathf.Min(count, maxIcons) : count;
            bool truncated = shown < count;

            float overflowWidth = truncated ? iconSize + 6f : 0f;
            float totalWidth = shown * (iconSize + spacing) + overflowWidth;
            float startX = area.xMax - totalWidth;
            float y = area.y + (area.height - iconSize) * 0.5f;

            buffer.Clear();
            for (int i = 0; i < shown; i++)
            {
                buffer.Add(new Rect(startX + i * (iconSize + spacing), y, iconSize, iconSize));
            }

            overflowRect = truncated
                ? new Rect(startX + shown * (iconSize + spacing), y, overflowWidth, iconSize)
                : Rect.zero;
        }
    }
}
