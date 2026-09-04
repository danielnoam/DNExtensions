using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Turns the Hierarchy's scene headers into a scene switcher: click the name and every scene in
    /// the project drops down, listed by name with the open ones ticked.
    ///
    /// Only the name is clickable, not the whole row. The rest of the header still selects the scene
    /// the way it always did, and right-click still belongs to Unity — which is where Set Active,
    /// Save and Remove live, and none of those are worth taking over.
    /// </summary>
    internal static class HierarchySceneMenu
    {
        /// <summary>Gap between the end of the name and the arrow, and the arrow's own box.</summary>
        private const float ArrowGap = 3f;
        private const float ArrowWidth = 15f;

        /// <summary>
        /// The caret Unity draws in its own dropdown chrome, and the one behind the USS
        /// --unity-icons-dropdown. Present in every version the suite supports, with an @2x beside it
        /// that LoadIcon picks up on a high-DPI display.
        /// </summary>
        private const string DropdownIcon = "icon dropdown";

        private const float CaretWidth = 7f;
        private const float CaretHeight = 4f;

        /// <summary>
        /// How long after the list closes a press on the same header is swallowed. The dropdown gives
        /// up focus before the click reaches the Hierarchy, so without this a click on an open menu's
        /// own arrow would close the list and immediately open it again, which reads as nothing having
        /// happened at all.
        /// </summary>
        private const double ReopenGuard = 0.25;

        private static GUIStyle _labelStyle;

        /// <summary>Which header is being pressed, so only that one's arrow lights up. Cleared on release.</summary>
        private static object _pressedId;

        /// <summary>Which header's list is open, so its arrow stays lit for as long as the list is up.</summary>
        private static object _openId;

        private static object _closedId;
        private static double _closedAt;

        /// <summary>Called for rows that resolved to no object, which in the Hierarchy means a scene header.</summary>
        public static void Draw(object rawId, Rect rowRect, bool hovered)
        {
            if (!TryGetScene(rawId, out Scene scene)) return;

            Event evt = Event.current;
            if (evt == null) return;

            Rect hit = HitRect(rowRect, scene);
            bool pressed = _pressedId != null && _pressedId.Equals(rawId);
            bool open = _openId != null && _openId.Equals(rawId);

            switch (evt.type)
            {
                case EventType.Repaint:
                    // Claimed whether or not the cursor is on it: without this the window only repaints
                    // when the hovered row changes, so moving along a row and onto the name would show
                    // nothing until something else happened to ask for a repaint.
                    HelpfulEditorGUI.MarkInteractive(hit);

                    // Nothing is drawn over the row itself — only the arrow, past the end of the name,
                    // which is why the icon and label need no standing in for any more.
                    bool lit = pressed || open;
                    if (lit || (hovered && hit.Contains(evt.mousePosition))) DrawArrow(hit, lit);

                    return;

                // The menu waits for the release so the press has somewhere to show: taken on the way
                // down it would open under the cursor before the arrow ever changed colour.
                case EventType.MouseDown when evt.button == 0 && hit.Contains(evt.mousePosition):
                    // A press that only just dismissed this header's own list is the closing half of a
                    // toggle, so it is used up rather than allowed to open the list a second time.
                    if (!JustClosed(rawId)) _pressedId = rawId;

                    evt.Use();
                    EditorApplication.RepaintHierarchyWindow();

                    return;

                case EventType.MouseUp when pressed:
                    _pressedId = null;

                    if (hit.Contains(evt.mousePosition)) Show(rawId, hit);

                    evt.Use();
                    EditorApplication.RepaintHierarchyWindow();

                    return;
            }
        }

        private static bool JustClosed(object rawId)
        {
            return _closedId != null
                   && _closedId.Equals(rawId)
                   && EditorApplication.timeSinceStartup - _closedAt < ReopenGuard;
        }

        /// <summary>
        /// Scene rows carry the scene's handle as their id, which is the only way to tell which header
        /// was drawn — the row resolves to no object, so there is nothing else to ask.
        /// </summary>
        private static bool TryGetScene(object rawId, out Scene scene)
        {
            scene = default;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (!HelpfulEditorObjectId.MatchesScene(rawId, candidate)) continue;

                scene = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The name, and the arrow that follows it. Measured rather than assumed so a long scene name
        /// stays clickable and a short one does not claim half an empty row.
        ///
        /// The text is measured without the label style's own padding and then placed with it, because
        /// CalcSize hands back padding on both sides — counting that as text puts the arrow adrift of
        /// the name it belongs to.
        /// </summary>
        private static Rect HitRect(Rect rowRect, Scene scene)
        {
            EnsureLabelStyle();

            float text = _labelStyle.CalcSize(new GUIContent(Label(scene))).x - _labelStyle.padding.horizontal;
            float contentEnd = rowRect.x + HierarchyModule.IconWidth + _labelStyle.padding.left + text;
            float right = Mathf.Min(contentEnd + ArrowGap + ArrowWidth, rowRect.xMax);

            return new Rect(rowRect.x, rowRect.y, Mathf.Max(0f, right - rowRect.x), rowRect.height);
        }

        /// <summary>
        /// A chip with a caret in it, at the end of the name. Nothing covers the row's own icon or
        /// label, so there is nothing left to line up with what is underneath — which is the whole
        /// appeal.
        ///
        /// The caret is Unity's own, rather than the glyph this used to write or a triangle of our own
        /// pixels. Both of those had the same problem from opposite ends: the glyph took its shape
        /// from whatever font the skin handed over, and a hand-stacked triangle is fixed at one size
        /// and so goes visibly blocky the moment the display is not at 1x. The built-in icon ships an
        /// @2x beside it and is the shape every other dropdown in the editor already uses.
        ///
        /// It is placed at its own size rather than stretched to fill the box — an icon scaled by a
        /// fraction is exactly the softness this was meant to be rid of.
        ///
        /// Nothing is drawn behind it. The row has no other boxes on it, and a filled chip to press
        /// against would be a heavier mark than a scene header wants, so the state is carried by the
        /// caret alone: faint until it is worth looking at, full while pressed or while the list is
        /// open — which is the only reason it is drawn at all when the cursor is elsewhere.
        /// </summary>
        private static void DrawArrow(Rect hitRect, bool lit)
        {
            Rect arrow = new Rect(hitRect.xMax - ArrowWidth, hitRect.y, ArrowWidth, hitRect.height);

            bool pro = EditorGUIUtility.isProSkin;
            float ink = pro ? 1f : 0f;

            Texture2D icon = HelpfulEditorGUI.LoadIcon(DropdownIcon);

            // Nothing to fall back to but our own triangle. It is not as good, which is the point of
            // preferring the icon, but it is better than a header with no arrow on it at all.
            if (!icon)
            {
                DrawCaret(arrow, new Color(ink, ink, ink, lit ? 1f : 0.55f));
                return;
            }

            Color previous = GUI.color;

            // Alpha only: the icon already comes in the skin's own colour, so tinting it would be
            // recolouring a glyph that is already the right one.
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * (lit ? 1f : 0.6f));

            GUI.DrawTexture(IconRect(arrow, icon), icon, ScaleMode.ScaleToFit);

            GUI.color = previous;
        }

        /// <summary>
        /// The icon at one texel per device pixel, centred in the arrow's box. LoadIcon hands back the
        /// @2x texture whenever the display is above 1x, so the pixel size is divided back down to
        /// points before it is placed — drawing the doubled texture at its pixel size would render it
        /// at twice the size it is meant to be.
        /// </summary>
        private static Rect IconRect(Rect area, Texture2D icon)
        {
            float scale = EditorGUIUtility.pixelsPerPoint > 1f ? 2f : 1f;

            Rect rect = new Rect(0f, 0f,
                Mathf.Min(icon.width / scale, area.width),
                Mathf.Min(icon.height / scale, area.height))
            {
                center = area.center
            };

            rect.x = Mathf.Round(rect.x);
            rect.y = Mathf.Round(rect.y);

            return rect;
        }

        /// <summary>A downward caret built from whole-pixel rows, seven wide narrowing to one.</summary>
        private static void DrawCaret(Rect area, Color color)
        {
            float left = Mathf.Round(area.center.x - CaretWidth * 0.5f);
            float top = Mathf.Round(area.center.y - CaretHeight * 0.5f);

            for (int row = 0; row < CaretHeight; row++)
            {
                EditorGUI.DrawRect(new Rect(left + row, top + row, CaretWidth - row * 2f, 1f), color);
            }
        }

        /// <summary>The name as the Hierarchy writes it, suffixes and all, so the arrow lands past all of it.</summary>
        private static string Label(Scene scene)
        {
            if (!scene.isLoaded) return $"{scene.name} (not loaded)";

            return scene.isDirty ? $"{scene.name}*" : scene.name;
        }

        /// <summary>
        /// The tree's own line style, bold because that is what the Hierarchy uses for a scene header.
        /// Looked up by skin name rather than rebuilt: it is measuring the label Unity drew, so the
        /// metrics have to be that label's.
        /// </summary>
        private static void EnsureLabelStyle()
        {
            if (_labelStyle != null) return;

            GUIStyle skinStyle = GUI.skin.FindStyle("TV LineBold") ?? GUI.skin.FindStyle("TV Line");

            _labelStyle = skinStyle != null ? new GUIStyle(skinStyle) : new GUIStyle(EditorStyles.boldLabel);
        }

        /// <summary>
        /// Handed the name's rect in screen space, so the list drops from the name rather than from
        /// wherever the cursor happened to be. Deferred because ShowAsDropDown rebuilds focus, which
        /// is not safe from inside the Hierarchy's own GUI pass.
        ///
        /// The header is marked open here rather than when the window actually appears — the delay is
        /// a frame or more, and an arrow that unlit in between would flicker on every open.
        /// </summary>
        private static void Show(object rawId, Rect nameRect)
        {
            Rect activator = GUIUtility.GUIToScreenRect(nameRect);

            _openId = rawId;

            EditorApplication.delayCall += () => HierarchySceneMenuWindow.Open(activator, () => Closed(rawId));
        }

        /// <summary>
        /// The list is gone, so the arrow goes back to how it was. Ignored when another header has
        /// opened since, which happens when a click on one arrow closes another one's list — the
        /// closing window would otherwise unlight the header that just took its place.
        /// </summary>
        private static void Closed(object rawId)
        {
            if (_openId != null && !_openId.Equals(rawId)) return;

            _openId = null;
            _closedId = rawId;
            _closedAt = EditorApplication.timeSinceStartup;

            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
