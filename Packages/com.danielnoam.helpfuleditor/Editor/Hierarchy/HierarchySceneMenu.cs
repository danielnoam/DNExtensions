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
        /// <summary>
        /// Room around the icon and name. Deliberately uneven: the icon carries its own transparent
        /// margin on the left while the text ends where it ends, so equal numbers do not look equal.
        /// </summary>
        private const float LeftPadding = 2f;
        private const float RightPadding = 6f;

        /// <summary>Keeps the button off the row's edges, and sits it a shade high rather than centred.</summary>
        private const float TopInset = 1f;
        private const float BottomInset = 2f;

        /// <summary>The row icon's own size — this one stands in for it, so it matches.</summary>
        private const float IconSize = 16f;

        /// <summary>Rebuilt on demand rather than cached: scenes are added and deleted rarely, and a stale list is worse than a scan.</summary>
        private static readonly List<string> PathBuffer = new List<string>();

        private static GUIStyle _labelStyle;
        private static Texture _sceneIcon;
        private static bool _iconResolved;

        /// <summary>Called for rows that resolved to no object, which in the Hierarchy means a scene header.</summary>
        public static void Draw(object rawId, Rect rowRect, bool hovered)
        {
            if (!TryGetScene(rawId, out Scene scene)) return;

            Event evt = Event.current;
            if (evt == null) return;

            Rect hit = ButtonRect(rowRect, scene);

            if (evt.type == EventType.Repaint)
            {
                // Claimed whether or not the cursor is on it: without this the window only repaints
                // when the hovered row changes, so moving along a row and onto the name would light
                // nothing up until something else happened to ask for a repaint.
                HelpfulEditorGUI.MarkInteractive(hit);

                // Lit only under the cursor rather than for the whole row, which is what says the name
                // is the part that does something and the rest of the header is still the header.
                if (hovered && hit.Contains(evt.mousePosition)) DrawButton(rowRect, hit, scene);

                return;
            }

            if (evt.type != EventType.MouseDown || evt.button != 0) return;
            if (!hit.Contains(evt.mousePosition)) return;

            Show(hit);
            evt.Use();
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
        /// The icon and the name, and nothing past them. Measured rather than assumed so a long scene
        /// name stays clickable and a short one does not claim half an empty row.
        /// </summary>
        /// <summary>
        /// The button's box: Unity's icon and name with the same room either side. The content itself is
        /// left exactly where the row already drew it and the button grows around it, so nothing shifts
        /// when it appears under the cursor.
        ///
        /// The text is measured without the label style's own padding and then placed with it, because
        /// CalcSize hands back padding on both sides — counting that as text is what left the button
        /// running well past the name while sitting flush against the icon.
        /// </summary>
        private static Rect ButtonRect(Rect rowRect, Scene scene)
        {
            EnsureLabelStyle();

            float text = _labelStyle.CalcSize(new GUIContent(Label(scene))).x - _labelStyle.padding.horizontal;
            float contentEnd = rowRect.x + HierarchyModule.IconWidth + _labelStyle.padding.left + text;

            float left = Mathf.Max(0f, rowRect.x - LeftPadding);
            float right = Mathf.Min(contentEnd + RightPadding, rowRect.xMax);

            return new Rect(left, rowRect.y, Mathf.Max(0f, right - left), rowRect.height);
        }

        /// <summary>
        /// A row is fully drawn by the time this callback runs, so there is no getting underneath it —
        /// the button covers Unity's icon and name, and both are put back on top of it. That is also why
        /// the label is drawn in the tree's own bold line style rather than a lookalike: it is standing
        /// in for the one underneath, and any difference in font or spacing would read as a jump.
        ///
        /// The background is the editor's own mini button drawn in its hover state, rather than a fill
        /// and an outline of our own — the rounding, the shading and both skins come with it, and it is
        /// the shape everything else in the editor uses to say "this is a button".
        /// </summary>
        private static void DrawButton(Rect rowRect, Rect buttonRect, Scene scene)
        {
            Rect background = new Rect(buttonRect.x, buttonRect.y + TopInset,
                buttonRect.width, Mathf.Max(0f, buttonRect.height - TopInset - BottomInset));

            // Drawn in its hover state, which is the state it is in — it only exists under the cursor.
            EditorStyles.miniButton.Draw(background, true, false, false, false);

            Texture icon = SceneIcon();

            if (icon)
            {
                // Where the row's own icon is, not where the button would centre it, so the two line up.
                Rect iconRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - IconSize) * 0.5f, IconSize, IconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            Rect labelRect = new Rect(rowRect.x + HierarchyModule.IconWidth, rowRect.y,
                Mathf.Max(0f, buttonRect.xMax - (rowRect.x + HierarchyModule.IconWidth)), rowRect.height);

            EnsureLabelStyle();
            _labelStyle.Draw(labelRect, Label(scene), false, false, false, false);
        }

        /// <summary>The name as the Hierarchy writes it, suffixes and all, so the redraw says the same thing.</summary>
        private static string Label(Scene scene)
        {
            if (!scene.isLoaded) return $"{scene.name} (not loaded)";

            return scene.isDirty ? $"{scene.name}*" : scene.name;
        }

        /// <summary>
        /// The tree's own line style, bold because that is what the Hierarchy uses for a scene header.
        /// Looked up by skin name rather than rebuilt: the metrics have to match the label being covered.
        /// </summary>
        private static void EnsureLabelStyle()
        {
            if (_labelStyle != null) return;

            GUIStyle skinStyle = GUI.skin.FindStyle("TV LineBold") ?? GUI.skin.FindStyle("TV Line");

            _labelStyle = skinStyle != null ? new GUIStyle(skinStyle) : new GUIStyle(EditorStyles.boldLabel);
        }

        private static Texture SceneIcon()
        {
            if (_iconResolved) return _sceneIcon;
            _iconResolved = true;

            foreach (string name in new[] { "SceneAsset Icon", "UnityLogo" })
            {
                GUIContent content = EditorGUIUtility.IconContent(name);
                if (!content?.image) continue;

                _sceneIcon = content.image;
                break;
            }

            return _sceneIcon;
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
