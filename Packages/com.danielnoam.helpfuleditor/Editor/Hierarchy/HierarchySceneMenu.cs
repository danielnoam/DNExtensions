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
        private const float NamePadding = 8f;

        /// <summary>Leaves a pixel of row above and below, so the highlight reads as a button in the row rather than as a filled row.</summary>
        private const float VerticalInset = 1f;

        /// <summary>Rebuilt on demand rather than cached: scenes are added and deleted rarely, and a stale list is worse than a scan.</summary>
        private static readonly List<string> PathBuffer = new List<string>();

        /// <summary>Called for rows that resolved to no object, which in the Hierarchy means a scene header.</summary>
        public static void Draw(object rawId, Rect rowRect, bool hovered)
        {
            if (!TryGetScene(rawId, out Scene scene)) return;

            Event evt = Event.current;
            if (evt == null) return;

            Rect hit = NameRect(rowRect, scene);

            if (evt.type == EventType.Repaint)
            {
                // Lit only under the cursor rather than for the whole row, which is what says the name
                // is the part that does something and the rest of the header is still the header.
                if (hovered && hit.Contains(evt.mousePosition)) DrawHoverBackground(hit);

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
        private static Rect NameRect(Rect rowRect, Scene scene)
        {
            float text = EditorStyles.label.CalcSize(new GUIContent(scene.name)).x;
            float width = Mathf.Min(HierarchyModule.IconWidth + text + NamePadding, rowRect.width);

            return new Rect(rowRect.x, rowRect.y, width, rowRect.height);
        }

        /// <summary>
        /// Lightens in the dark skin and darkens in the light one, because the row it sits on goes the
        /// other way in each — a fixed tint is invisible in one of the two.
        /// </summary>
        private static void DrawHoverBackground(Rect nameRect)
        {
            Rect background = new Rect(nameRect.x, nameRect.y + VerticalInset,
                nameRect.width, Mathf.Max(0f, nameRect.height - VerticalInset * 2f));

            Color tint = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.09f)
                : new Color(0f, 0f, 0f, 0.09f);

            EditorGUI.DrawRect(background, tint);
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
