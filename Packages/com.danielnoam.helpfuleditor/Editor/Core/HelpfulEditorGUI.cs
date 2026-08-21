using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>Drawing primitives shared by the Hierarchy and Project row overlays.</summary>
    internal static class HelpfulEditorGUI
    {
        public const float IndentWidth = 14f;

        /// <summary>Weight of the Hierarchy and Project tree guides. Shared so the two cannot drift apart.</summary>
        public const float TreeLineThickness = 2f;

        /// <summary>
        /// Opacity of the row icon strips. They are supporting information, so they sit back from
        /// the row's own icon and label rather than competing with them.
        /// </summary>
        public const float IconStripOpacity = 0.7f;

        private const float BaseDashSegment = 2f;
        private const float DashGap = 2f;
        private const BindingFlags AnyMember = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, Texture> IconCache = new Dictionary<Type, Texture>();
        private static readonly Dictionary<string, Texture2D> BuiltInIcons = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> MissingIcons = new HashSet<string>();
        private static GUIStyle _badgeStyle;

        /// <summary>
        /// Widths of strings drawn in the standard label style. Measuring text is the expensive part
        /// of placing anything beside a row's own name, and the rows that need it — the Hierarchy's
        /// component strip, the Project window's folder icons and badges — ask for the same handful
        /// of names on every repaint.
        /// </summary>
        private static readonly Dictionary<string, float> LabelWidths = new Dictionary<string, float>();

        /// <summary>
        /// Past this the cache is dropped whole. Names are only added as rows draw them, so reaching
        /// it at all means a project big enough that holding every name measured this session is the
        /// larger cost of the two.
        /// </summary>
        private const int MaxLabelWidths = 4096;

        private static readonly GUIContent MeasureContent = new GUIContent();
        private static GUIStyle _measuredStyle;

        private static MethodInfo _loadIcon;
        private static bool _loadIconResolved;
        private static bool _builtInIconsAreProSkin;

        private static PropertyInfo _guiViewCurrent;
        private static MethodInfo _markHotRegion;
        private static MethodInfo _unclipToWindow;
        private static bool _hotRegionResolved;
        private static bool _hotRegionAvailable;

        // The bound form of the two calls above, which is what MarkInteractive uses when the runtime
        // will hand it one. MarkHotRegion is bound closed over the view rather than left open — its
        // declaring type is internal and cannot be named in a delegate signature here.
        private static Func<Rect, Rect> _unclipBound;
        private static Action<Rect> _markHotRegionBound;
        private static object _boundView;
        private static bool _bindingUnavailable;

        // Reused by the fallback path. Fresh arrays there were the bulk of what marking a row cost,
        // and the calls run one after another rather than nested, so one buffer each is enough.
        private static readonly object[] UnclipArgs = new object[1];
        private static readonly object[] MarkArgs = new object[1];

        /// <summary>
        /// Whether rows can register themselves as interactive, which is what makes the editor
        /// repaint the window on mouse move. Callers that draw a hover state need to know: without
        /// it the window only repaints when something else happens to ask it to, and the hover has
        /// to be driven by forcing repaints from the update loop instead.
        /// </summary>
        public static bool HotRegionAvailable
        {
            get
            {
                ResolveHotRegion();
                return _hotRegionAvailable;
            }
        }

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

        public static Color PanelOuterBorder => EditorGUIUtility.isProSkin
            ? new Color(0.08f, 0.08f, 0.08f)
            : new Color(0.35f, 0.35f, 0.35f);

        public static Color PanelInnerBorder => EditorGUIUtility.isProSkin
            ? new Color(0.42f, 0.42f, 0.42f)
            : new Color(0.72f, 0.72f, 0.72f);

        public static Color PanelSeparator => EditorGUIUtility.isProSkin
            ? new Color(0.25f, 0.25f, 0.25f)
            : new Color(0.65f, 0.65f, 0.65f);

        /// <summary>Expands a row's selection rect to span the full window width so backgrounds cover the whole row.</summary>
        /// <summary>
        /// Width of a string drawn in the standard label style, remembered between calls.
        ///
        /// Callers use this to find where a row's own name ends so they can place something after
        /// it, which means measuring the same name on every repaint of every row that has one.
        /// CalcSize is not cheap enough to spend that way.
        ///
        /// The cache is dropped when the style is rebuilt — which the editor does on a theme change
        /// and across a play mode transition — since a width measured against the old font says
        /// nothing about the new one. Comparing the style instance catches both without this having
        /// to subscribe to either.
        /// </summary>
        public static float LabelWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            GUIStyle style = EditorStyles.label;

            if (!ReferenceEquals(style, _measuredStyle))
            {
                _measuredStyle = style;
                LabelWidths.Clear();
            }

            if (LabelWidths.TryGetValue(text, out float cached)) return cached;

            MeasureContent.text = text;
            float width = style.CalcSize(MeasureContent).x;

            if (LabelWidths.Count >= MaxLabelWidths) LabelWidths.Clear();
            LabelWidths[text] = width;

            return width;
        }

        public static Rect FullRowRect(Rect rowRect)
        {
            float right = Mathf.Max(rowRect.xMax, EditorGUIUtility.currentViewWidth);
            return new Rect(0f, rowRect.y, right, rowRect.height);
        }

        /// <summary>
        /// Registers a rect as interactive content so the editor repaints the window while the
        /// cursor moves across it. IMGUI drawn from a row callback is invisible to that machinery
        /// otherwise, which is what leaves a hover highlight lagging until something else forces a
        /// repaint. Only meaningful during repaint, when the region list is being rebuilt.
        /// </summary>
        public static void MarkInteractive(Rect rect)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;

            ResolveHotRegion();
            if (!_hotRegionAvailable) return;

            try
            {
                object view = _guiViewCurrent.GetValue(null);
                if (view == null) return;

                if (TryBind(view))
                {
                    _markHotRegionBound(_unclipBound(rect));
                    return;
                }

                UnclipArgs[0] = rect;
                MarkArgs[0] = _unclipToWindow.Invoke(null, UnclipArgs);
                _markHotRegion.Invoke(view, MarkArgs);
            }
            catch (Exception e)
            {
                // One failure means the internals moved, so the whole mechanism is abandoned rather
                // than retried for every row of every repaint.
                _hotRegionAvailable = false;
                Debug.LogWarning($"[HelpfulEditor] Hover repaints fall back to polling on this Unity version. ({e.Message})");
            }
        }

        /// <summary>
        /// Binds the two internals as delegates, which is what takes marking a row from two
        /// reflective invocations and four allocations down to two direct calls and none. Worth the
        /// trouble because this runs for every visible row of every repaint, and the Hierarchy
        /// repaints on every mouse move across it.
        ///
        /// The view is only rebound when the drawing moves to another window, so one binding covers
        /// a whole window's sweep rather than a single row of it.
        ///
        /// False where the runtime declines to bind — MarkHotRegion is internal, and binding a
        /// non-public method is a stricter ask than invoking one. That leaves the reflective path to
        /// do the work, which is slower rather than broken, so it is deliberately not a reason to
        /// give up on hover repaints the way a genuinely missing member is.
        /// </summary>
        private static bool TryBind(object view)
        {
            if (_bindingUnavailable) return false;

            try
            {
                _unclipBound ??= (Func<Rect, Rect>)_unclipToWindow.CreateDelegate(typeof(Func<Rect, Rect>));

                if (!ReferenceEquals(view, _boundView))
                {
                    _markHotRegionBound = (Action<Rect>)_markHotRegion.CreateDelegate(typeof(Action<Rect>), view);
                    _boundView = view;
                }

                return true;
            }
            catch (Exception)
            {
                _bindingUnavailable = true;
                _unclipBound = null;
                _markHotRegionBound = null;
                _boundView = null;

                return false;
            }
        }

        private static void ResolveHotRegion()
        {
            if (_hotRegionResolved) return;
            _hotRegionResolved = true;

            try
            {
                Type guiViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView");
                Type guiClipType = typeof(GUI).Assembly.GetType("UnityEngine.GUIClip");
                if (guiViewType == null || guiClipType == null) return;

                _guiViewCurrent = guiViewType.GetProperty("current", AnyMember);
                _markHotRegion = guiViewType.GetMethod("MarkHotRegion", AnyMember, null, new[] { typeof(Rect) }, null);
                _unclipToWindow = guiClipType.GetMethod("UnclipToWindow", AnyMember, null, new[] { typeof(Rect) }, null);

                _hotRegionAvailable = _guiViewCurrent != null && _markHotRegion != null && _unclipToWindow != null;
            }
            catch (Exception)
            {
                _hotRegionAvailable = false;
            }
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

        /// <summary>The whole-pixel thickness a line will actually be drawn at.</summary>
        public static float LineThickness(float thickness)
        {
            return Mathf.Max(1f, Mathf.Round(thickness));
        }

        /// <summary>
        /// Where an elbow's horizontal should begin. Solid abuts the vertical exactly — overlapping
        /// would double-draw a translucent corner. Dotted leaves a further gap, so the turn reads as
        /// the dash pattern continuing round the corner rather than as a solid blob at the join.
        /// </summary>
        public static float ElbowStart(float guideX, float thickness, LineStyle style)
        {
            float weight = LineThickness(thickness);
            return style == LineStyle.Dotted ? guideX + weight + DashGap : guideX + weight;
        }

        /// <summary>
        /// Classic tree connectors: one guide per ancestor level, drawn only while that ancestor
        /// still has rows below it, and an elbow into the row's icon. A last child terminates its
        /// guide at the elbow instead of running it to the bottom of the row.
        /// </summary>
        /// <param name="lastOnPath">
        /// One entry per level, holding whether the node the path passes through just below that
        /// level is its parent's last child. The final entry describes the row itself. Its length is
        /// the row's depth.
        /// </param>
        /// <param name="stopBeforeFoldout">
        /// True for rows that draw their own foldout arrow, so the elbow ends short of the arrow's
        /// column instead of running underneath the glyph.
        /// </param>
        public static void DrawTreeConnectors(Rect rowRect, float leftEdge, IReadOnlyList<bool> lastOnPath, Color color, LineStyle style, bool stopBeforeFoldout)
        {
            if (lastOnPath == null || color.a <= 0f) return;

            int depth = lastOnPath.Count;
            if (depth <= 0) return;

            float midY = SnapToPixel(rowRect.y + rowRect.height * 0.5f);
            float weight = LineThickness(TreeLineThickness);

            for (int level = 0; level < depth; level++)
            {
                bool lastSibling = lastOnPath[level];
                float x = GuideColumnX(leftEdge, level);

                if (level < depth - 1)
                {
                    if (!lastSibling) DrawVerticalLine(x, rowRect.y, rowRect.yMax, color, style, TreeLineThickness, midY);
                    continue;
                }

                // A terminating guide runs to the far side of the elbow so it owns the corner
                // outright; the horizontal then starts past it. Overlapping the two would draw a
                // translucent colour twice and leave a dark notch at the join.
                float bottom = lastSibling ? midY + weight : rowRect.yMax;
                DrawVerticalLine(x, rowRect.y, bottom, color, style, TreeLineThickness, midY);

                float elbowEnd = stopBeforeFoldout ? rowRect.x - IndentWidth : rowRect.x - 2f;
                DrawHorizontalLine(ElbowStart(x, TreeLineThickness, style), elbowEnd, midY, color, style, TreeLineThickness);
            }
        }

        /// <summary>
        /// Fallback for rows whose sibling position is unknown: a full-height guide per ancestor
        /// level plus an elbow, with nothing terminating. Used when the tree's own row data cannot
        /// be read, which is the only case that leaves the topology unavailable.
        /// </summary>
        public static void DrawDepthLines(Rect rowRect, int depth, float leftEdge, Color color, LineStyle style, bool stopBeforeFoldout)
        {
            if (depth <= 0 || color.a <= 0f) return;

            float midY = SnapToPixel(rowRect.y + rowRect.height * 0.5f);

            for (int level = 0; level < depth; level++)
            {
                DrawVerticalLine(GuideColumnX(leftEdge, level), rowRect.y, rowRect.yMax, color, style, TreeLineThickness, midY);
            }

            float elbowEnd = stopBeforeFoldout ? rowRect.x - IndentWidth : rowRect.x - 2f;

            DrawHorizontalLine(ElbowStart(GuideColumnX(leftEdge, depth - 1), TreeLineThickness, style), elbowEnd, midY,
                color, style, TreeLineThickness);
        }

        /// <param name="dashAnchorY">
        /// A y a dash must start on, used to line the dashes up with an elbow. Without it the dash
        /// phase runs from the top of the line and the corner lands in a gap as often as not, so the
        /// elbow reads as two disconnected strokes.
        /// </param>
        public static void DrawVerticalLine(float x, float top, float bottom, Color color, LineStyle style, float thickness = 1f, float dashAnchorY = float.NaN)
        {
            if (bottom <= top) return;

            float weight = LineThickness(thickness);

            if (style == LineStyle.Solid)
            {
                EditorGUI.DrawRect(new Rect(x, top, weight, bottom - top), color);
                return;
            }

            float segment = DashSegment(weight);
            float step = segment + DashGap;
            float start = float.IsNaN(dashAnchorY) ? top : dashAnchorY + Mathf.Floor((top - dashAnchorY) / step) * step;

            for (float y = start; y < bottom; y += step)
            {
                float segmentTop = Mathf.Max(y, top);
                float segmentBottom = Mathf.Min(y + segment, bottom);
                if (segmentBottom > segmentTop) EditorGUI.DrawRect(new Rect(x, segmentTop, weight, segmentBottom - segmentTop), color);
            }
        }

        public static void DrawHorizontalLine(float left, float right, float y, Color color, LineStyle style, float thickness = 1f)
        {
            if (right <= left) return;

            float weight = LineThickness(thickness);

            if (style == LineStyle.Solid)
            {
                EditorGUI.DrawRect(new Rect(left, y, right - left, weight), color);
                return;
            }

            float segment = DashSegment(weight);

            for (float x = left; x < right; x += segment + DashGap)
            {
                EditorGUI.DrawRect(new Rect(x, y, Mathf.Min(segment, right - x), weight), color);
            }
        }

        /// <summary>Dashes are never shorter than the line is thick, so a corner dash fills the join.</summary>
        private static float DashSegment(float weight)
        {
            return Mathf.Max(BaseDashSegment, weight);
        }

        /// <summary>
        /// A border whose strokes are a whole number of device pixels, with its edges snapped to the
        /// device grid.
        ///
        /// The obvious version — four rects one point thick — is only crisp at 100% scaling. At 125%
        /// or 150% a one-point stroke covers a fractional number of device pixels, and whether it
        /// resolves to one or two of them depends on where that particular edge happens to land. That
        /// is what leaves a panel looking like its verticals are a different weight from its
        /// horizontals: nothing about the drawing is asymmetric, only where the four edges fall.
        /// </summary>
        public static void DrawBorder(Rect rect, Color color, float thickness = 1f)
        {
            float scale = DeviceScale;
            float weight = BorderWeight(thickness);

            float left = SnapToDevice(rect.x, scale);
            float top = SnapToDevice(rect.y, scale);
            float right = SnapToDevice(rect.xMax, scale);
            float bottom = SnapToDevice(rect.yMax, scale);

            float width = right - left;
            float height = bottom - top;
            if (width <= 0f || height <= 0f) return;

            EditorGUI.DrawRect(new Rect(left, top, width, weight), color);
            EditorGUI.DrawRect(new Rect(left, bottom - weight, width, weight), color);
            EditorGUI.DrawRect(new Rect(left, top, weight, height), color);
            EditorGUI.DrawRect(new Rect(right - weight, top, weight, height), color);
        }

        /// <summary>What a stroke of this many points really measures once rounded to whole device pixels.</summary>
        public static float BorderWeight(float thickness = 1f)
        {
            float scale = DeviceScale;
            return Mathf.Max(1f, Mathf.Round(thickness * scale)) / scale;
        }

        /// <summary>
        /// The background and edge of a dropdown panel, which Unity gives no chrome of its own: a
        /// dark outer stroke to separate it from whatever is behind, and a lighter inner one so the
        /// edge stays visible against a dark background too. The inset is the outer stroke's real
        /// width rather than a flat point, so the two stay touching at any display scale.
        /// </summary>
        public static void DrawPanelFrame(Rect rect)
        {
            EditorGUI.DrawRect(rect, WindowBackground);
            DrawBorder(rect, PanelOuterBorder);

            float inset = BorderWeight();
            DrawBorder(new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f), PanelInnerBorder);
        }

        private static float DeviceScale
        {
            get
            {
                float scale = EditorGUIUtility.pixelsPerPoint;
                return scale > 0f ? scale : 1f;
            }
        }

        private static float SnapToDevice(float value, float scale)
        {
            return Mathf.Round(value * scale) / scale;
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

        /// <summary>
        /// A built-in editor icon by name, or null where this editor has no icon of that name.
        ///
        /// The built-in names come and go between Unity versions, so anything the suite reaches for
        /// is a name that may simply not be there — and EditorGUIUtility.IconContent answers a name
        /// it cannot find with a console error of its own rather than with a null. No try/catch can
        /// intercept that, because nothing is thrown, which is how an optional icon turned into a
        /// permanent red line in projects on the versions that lack it. This takes the internal
        /// lookup underneath IconContent instead, which returns null and says nothing.
        ///
        /// Several names may be given and the first that resolves wins, which is how a caller offers
        /// a choice of glyphs and lets the version in hand pick. Skin (d_) and @2x variants are the
        /// lookup's own business, so plain names are enough.
        /// </summary>
        public static Texture2D LoadIcon(params string[] names)
        {
            if (names == null) return null;

            // Icons are per-skin, and the textures behind the old ones are dropped when the skin
            // changes, so what was cached under the other skin is worse than useless.
            if (_builtInIconsAreProSkin != EditorGUIUtility.isProSkin)
            {
                _builtInIconsAreProSkin = EditorGUIUtility.isProSkin;

                BuiltInIcons.Clear();
                MissingIcons.Clear();
            }

            foreach (string name in names)
            {
                Texture2D icon = LoadSingleIcon(name);
                if (icon) return icon;
            }

            return null;
        }

        /// <summary>
        /// The same lookup wrapped for the toolbar buttons, which want a tooltip beside the glyph.
        /// Null rather than an empty content when nothing resolved, so the caller can put its own
        /// word there instead and leave a button that still reads.
        /// </summary>
        public static GUIContent IconContent(string tooltip, params string[] names)
        {
            Texture2D icon = LoadIcon(names);

            return icon ? new GUIContent(icon, tooltip ?? string.Empty) : null;
        }

        /// <summary>
        /// Misses are remembered as well as hits: a name this editor does not carry will not start
        /// carrying it, and these run from OnGUI. Hits are re-resolved if the texture behind one has
        /// gone, which a cached null would otherwise hide.
        /// </summary>
        private static Texture2D LoadSingleIcon(string name)
        {
            if (string.IsNullOrEmpty(name) || MissingIcons.Contains(name)) return null;
            if (BuiltInIcons.TryGetValue(name, out Texture2D cached) && cached) return cached;

            Texture2D icon = null;

            if (LoadIconMethod != null)
            {
                try
                {
                    icon = LoadIconMethod.Invoke(null, new object[] { name }) as Texture2D;
                }
                catch (Exception)
                {
                    // Left null, and remembered below as a miss so the call is not tried again.
                }
            }

            if (icon) BuiltInIcons[name] = icon;
            else MissingIcons.Add(name);

            return icon;
        }

        /// <summary>
        /// EditorGUIUtility.LoadIcon, which is the quiet half of the public IconContent. Internal in
        /// every version the suite supports, hence the reflection; absent it there is nothing else to
        /// fall back to, since the public path is the one that logs.
        /// </summary>
        private static MethodInfo LoadIconMethod
        {
            get
            {
                if (_loadIconResolved) return _loadIcon;

                _loadIconResolved = true;
                _loadIcon = typeof(EditorGUIUtility).GetMethod("LoadIcon", AnyMember, null, new[] { typeof(string) }, null);

                return _loadIcon;
            }
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
        /// Lays out right-aligned icon slots within the area, dropping any that would not fit.
        /// Returns the rect of each slot, plus an overflow rect when the list is truncated.
        /// </summary>
        public static void LayoutIconStrip(Rect area, int count, float iconSize, int maxIcons, List<Rect> buffer, out int shown, out Rect overflowRect)
        {
            const float spacing = 1f;
            int wanted = maxIcons > 0 ? Mathf.Min(count, maxIcons) : count;

            // The area is the space the strip is allowed to occupy, so anything past its left edge
            // is dropped rather than drawn over whatever owns that space — the row's own label, in
            // the Project window's case. Walked down a slot at a time because dropping an icon for
            // width is itself a truncation, and the badge that announces it needs room of its own.
            shown = 0;
            for (int limit = wanted; limit > 0; limit--)
            {
                float badge = limit < count ? iconSize + 6f : 0f;
                if (limit * (iconSize + spacing) + badge > area.width) continue;

                shown = limit;
                break;
            }

            // A badge with no icons beside it reads as a stray number rather than a count.
            if (shown == 0)
            {
                buffer.Clear();
                overflowRect = Rect.zero;
                return;
            }

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
