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
        private const float ArrowWidth = 14f;
        private const int ArrowFontSize = 14;

        private static GUIStyle _labelStyle;
        private static GUIStyle _arrowStyle;

        /// <summary>Which header is being pressed, so only that one's arrow lights up. Cleared on release.</summary>
        private static object _pressedId;

        /// <summary>Called for rows that resolved to no object, which in the Hierarchy means a scene header.</summary>
        public static void Draw(object rawId, Rect rowRect, bool hovered)
        {
            if (!TryGetScene(rawId, out Scene scene)) return;

            Event evt = Event.current;
            if (evt == null) return;

            Rect hit = HitRect(rowRect, scene);
            bool pressed = _pressedId != null && _pressedId.Equals(rawId);

            switch (evt.type)
            {
                case EventType.Repaint:
                    // Claimed whether or not the cursor is on it: without this the window only repaints
                    // when the hovered row changes, so moving along a row and onto the name would show
                    // nothing until something else happened to ask for a repaint.
                    HelpfulEditorGUI.MarkInteractive(hit);

                    // Nothing is drawn over the row itself — only the arrow, past the end of the name,
                    // which is why the icon and label need no standing in for any more.
                    if (pressed || (hovered && hit.Contains(evt.mousePosition))) DrawArrow(hit, pressed);

                    return;

                // The menu waits for the release so the press has somewhere to show: taken on the way
                // down it would open under the cursor before the arrow ever changed colour.
                case EventType.MouseDown when evt.button == 0 && hit.Contains(evt.mousePosition):
                    _pressedId = rawId;

                    evt.Use();
                    EditorApplication.RepaintHierarchyWindow();

                    return;

                case EventType.MouseUp when pressed:
                    _pressedId = null;

                    if (hit.Contains(evt.mousePosition)) Show(hit);

                    evt.Use();
                    EditorApplication.RepaintHierarchyWindow();

                    return;
            }
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
        /// Only the arrow, at the end of the name. Nothing covers the row's own icon or label any more,
        /// so there is nothing left to line up with what is underneath — which is the whole appeal.
        /// </summary>
        private static void DrawArrow(Rect hitRect, bool pressed)
        {
            EnsureArrowStyle();

            Rect arrow = new Rect(hitRect.xMax - ArrowWidth, hitRect.y, ArrowWidth, hitRect.height);
            Color previous = GUI.color;

            bool pro = EditorGUIUtility.isProSkin;

            GUI.color = pressed
                ? (pro ? Color.white : Color.black)
                : new Color(pro ? 1f : 0f, pro ? 1f : 0f, pro ? 1f : 0f, 0.55f);

            GUI.Label(arrow, "▾", _arrowStyle);
            GUI.color = previous;
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

        private static void EnsureArrowStyle()
        {
            if (_arrowStyle != null) return;

            _arrowStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = ArrowFontSize,
                padding = new RectOffset(0, 0, 0, 0)
            };

            // Tinted through GUI.color rather than the style, so one style covers both states.
            _arrowStyle.normal.textColor = Color.white;
        }

        /// <summary>
        /// Handed the name's rect in screen space, so the list drops from the name rather than from
        /// wherever the cursor happened to be. Deferred because ShowAsDropDown rebuilds focus, which
        /// is not safe from inside the Hierarchy's own GUI pass.
        /// </summary>
        private static void Show(Rect nameRect)
        {
            Rect activator = GUIUtility.GUIToScreenRect(nameRect);

            EditorApplication.delayCall += () => HierarchySceneMenuWindow.Open(activator);
        }
    }
}
