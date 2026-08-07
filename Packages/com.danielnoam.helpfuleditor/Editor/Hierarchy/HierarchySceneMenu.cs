using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Turns the Hierarchy's scene headers into a scene switcher: click the name and every scene in
    /// the project drops down, foldered as it is on disk, with the open ones ticked.
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

        /// <summary>Rebuilt on demand rather than cached: scenes are added and deleted rarely, and a stale list is worse than a scan.</summary>
        private static readonly List<string> PathBuffer = new List<string>();

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

        private static void Show(Rect nameRect)
        {
            GenericMenu menu = new GenericMenu();

            CollectScenePaths();

            // Opening a scene tears the running one down, so play mode gets the list to look at and
            // nothing to click — the same as Unity refusing the call, but said before the fact.
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;

            foreach (string path in PathBuffer)
            {
                GUIContent label = new GUIContent(MenuPath(path));
                bool open = IsOpen(path);
                string captured = path;

                if (playing) menu.AddDisabledItem(label, open);
                else menu.AddItem(label, open, () => Open(captured, OpenSceneMode.Single));
            }

            menu.AddSeparator(string.Empty);

            foreach (string path in PathBuffer)
            {
                GUIContent label = new GUIContent($"Open Additive/{MenuPath(path)}");
                bool open = IsOpen(path);
                string captured = path;

                // An already loaded scene has nothing to add, and asking for it again would only
                // reload it — shown ticked and disabled rather than left out, so the list stays put.
                if (playing || open) menu.AddDisabledItem(label, open);
                else menu.AddItem(label, false, () => Open(captured, OpenSceneMode.Additive));
            }

            menu.DropDown(nameRect);
        }

        private static void CollectScenePaths()
        {
            PathBuffer.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path)) PathBuffer.Add(path);
            }

            PathBuffer.Sort(System.StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The asset path as a menu path: the folders become submenus because that is what a slash
        /// means to a GenericMenu, which is the whole reason the grouping is free.
        /// </summary>
        private static string MenuPath(string assetPath)
        {
            string path = assetPath;

            if (path.StartsWith("Assets/", System.StringComparison.Ordinal)) path = path.Substring("Assets/".Length);

            return path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
                ? path.Substring(0, path.Length - ".unity".Length)
                : path;
        }

        private static bool IsOpen(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path == path) return true;
            }

            return false;
        }

        private static void Open(string path, OpenSceneMode mode)
        {
            // Only the single open replaces what is loaded, so only that one has anything to save first.
            if (mode == OpenSceneMode.Single && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(path, mode);
        }
    }
}
