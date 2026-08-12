using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// The scene switcher dropped from a Hierarchy scene header. A window rather than a GenericMenu
    /// so the list reads as one flat set of names: a menu turns every slash in an asset path into a
    /// submenu, which buried scenes under whatever folders they happened to live in and made
    /// switching a matter of navigating the project rather than picking a scene.
    ///
    /// Starred scenes are listed above the rest with a divider between them. Open scenes are marked
    /// where they fall rather than lifted into a group of their own — the tick already says so, and
    /// a section for something the header above is already naming was a division too many.
    ///
    /// A search field sits above the list and takes focus on open, so a project with more scenes
    /// than fit on screen is narrowed by typing rather than by scrolling.
    /// </summary>
    internal class HierarchySceneMenuWindow : HelpfulEditorDropdownListWindow
    {
        private const float TickWidth = 18f;
        private const float StarWidth = 20f;
        private const float LabelPadding = 10f;

        private static GUIStyle _starStyle;

        private static GUIStyle StarStyle => _starStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 0)
        };

        private static Color StarColor => EditorGUIUtility.isProSkin
            ? new Color(1f, 0.8f, 0.25f)
            : new Color(0.75f, 0.55f, 0f);

        private readonly List<string> _paths = new List<string>();

        /// <summary>Which of the three groups each row belongs to, which is all a divider needs to know.</summary>
        private readonly List<int> _groups = new List<int>();

        private readonly GUIContent _rowContent = new GUIContent();
        private readonly GUIContent _starContent = new GUIContent();

        protected override int RowCount => _paths.Count;

        protected override bool ShowSearchField => true;

        protected override string EmptySearchText => "No scenes match";

        protected override void OnSearchChanged(string query) => Rebuild();

        /// <param name="activator">The header's name rect in screen space, so the list drops from the name it belongs to.</param>
        public static void Open(Rect activator)
        {
            HierarchySceneMenuWindow window = CreateInstance<HierarchySceneMenuWindow>();
            window.Rebuild();

            if (window.RowCount == 0)
            {
                DestroyImmediate(window);
                return;
            }

            window.SetHighlight(window._groups.IndexOf(0));
            window.ShowList(activator);
        }

        /// <summary>
        /// Rebuilt from scratch whenever the order could have changed, which starring a scene and
        /// typing in the search field both do. Group 0 is starred and 1 is everything else, since a
        /// search narrows the list rather than changing what is worth keeping at the top of it.
        ///
        /// Ordered by how well the name matches and then by the name itself — an empty query matches
        /// everything at the same cost, so the unsearched list reads alphabetically as it always did.
        /// </summary>
        private void Rebuild()
        {
            List<KeyValuePair<string, float>> matched = new List<KeyValuePair<string, float>>();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                // Matched on the name alone, not the path: the list shows names, and a query that
                // hit a folder somewhere up the path would look like it had matched nothing.
                if (!HelpfulEditorFuzzySearch.TryMatch(SceneName(path), SearchQuery, out float cost)) continue;

                matched.Add(new KeyValuePair<string, float>(path, cost));
            }

            matched.Sort((left, right) =>
            {
                int byCost = left.Value.CompareTo(right.Value);

                return byCost != 0
                    ? byCost
                    : string.Compare(SceneName(left.Key), SceneName(right.Key), StringComparison.OrdinalIgnoreCase);
            });

            _paths.Clear();
            _groups.Clear();

            for (int group = 0; group <= 1; group++)
            {
                foreach (KeyValuePair<string, float> entry in matched)
                {
                    if (GroupOf(entry.Key) != group) continue;

                    _paths.Add(entry.Key);
                    _groups.Add(group);
                }
            }
        }

        private static int GroupOf(string path) => IsFavorite(path) ? 0 : 1;

        private static string SceneName(string assetPath) => Path.GetFileNameWithoutExtension(assetPath);

        /// <summary>A divider wherever the group changes, so the three runs read apart.</summary>
        protected override float SeparatorBefore(int index)
        {
            if (index == 0) return 0f;

            return _groups[index] != _groups[index - 1] ? SeparatorHeight : 0f;
        }

        protected override float MeasureRowWidth(int index)
        {
            _rowContent.text = SceneName(_paths[index]);

            return TickWidth + EditorStyles.label.CalcSize(_rowContent).x + LabelPadding + StarWidth;
        }

        /// <summary>
        /// Opening a scene tears the running one down, so play mode gets the list to look at and
        /// nothing to click — the same answer Unity would give, said before the fact. The star is
        /// still live, since starring changes nothing about what is loaded.
        /// </summary>
        protected override bool IsRowEnabled(int index)
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        protected override void DrawRow(Rect rowRect, int index)
        {
            string path = _paths[index];
            bool open = IsOpen(path);

            Rect starRect = new Rect(rowRect.xMax - StarWidth, rowRect.y, StarWidth, rowRect.height);

            Color previous = GUI.color;
            if (!IsRowEnabled(index)) GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * 0.5f);

            if (open) GUI.Label(new Rect(rowRect.x, rowRect.y, TickWidth, rowRect.height), "✓", EditorStyles.boldLabel);

            _rowContent.text = SceneName(path);

            // The only place the middle-click is announced. It reveals rather than changes anything,
            // so it is worth offering and not worth a row of its own to advertise.
            _rowContent.tooltip = $"{path}\nMiddle-click to ping in the Project window";

            GUI.Label(Rect.MinMaxRect(rowRect.x + TickWidth, rowRect.y, starRect.x, rowRect.yMax),
                _rowContent, open ? EditorStyles.boldLabel : EditorStyles.label);

            GUI.color = previous;

            DrawStar(starRect, path, index);
        }

        /// <summary>
        /// Only on the row being pointed at, or on one already starred — a column of empty outlines
        /// down every row would read as clutter rather than as something to click.
        ///
        /// Consuming the click here is what keeps it off the row: the base checks for activation
        /// after the row has drawn, and an event already used is no longer a MouseDown by then.
        /// </summary>
        private void DrawStar(Rect starRect, string path, int index)
        {
            bool favorite = IsFavorite(path);
            if (!favorite && Highlighted != index) return;

            Event evt = Event.current;

            _starContent.text = favorite ? "★" : "☆";
            _starContent.tooltip = favorite ? "Remove from favourites" : "Add to favourites";

            Color previous = GUI.color;
            if (favorite) GUI.color = StarColor;

            GUI.Label(starRect, _starContent, StarStyle);
            GUI.color = previous;

            EditorGUIUtility.AddCursorRect(starRect, MouseCursor.Link);

            if (evt.type != EventType.MouseDown || evt.button != 0 || !starRect.Contains(evt.mousePosition)) return;

            evt.Use();
            ToggleFavorite(path);
        }

        private void ToggleFavorite(string path)
        {
            SetFavorite(path, !IsFavorite(path));

            // The row it was on belongs to a different group now, so the list is rebuilt rather than
            // left showing an order that no longer matches its own dividers.
            Rebuild();
            Repaint();
        }

        private static bool IsFavorite(string path) => EditorPrefs.GetBool(FavoriteKey(path), false);

        private static void SetFavorite(string path, bool favorite)
        {
            // Cleared rather than stored as false, so unstarring leaves nothing behind in a store
            // that outlives the project.
            if (favorite) EditorPrefs.SetBool(FavoriteKey(path), true);
            else EditorPrefs.DeleteKey(FavoriteKey(path));
        }

        /// <summary>
        /// Per user, which is what EditorPrefs is, and scoped to this project by its location on
        /// disk — EditorPrefs is shared by every project the editor opens, so an unqualified asset
        /// path would have two projects starring each other's scenes.
        /// </summary>
        private static string FavoriteKey(string path)
        {
            return $"DNExtensions.HelpfulEditor.SceneFavorite.{Application.dataPath.GetHashCode():X8}.{path}";
        }

        protected override void Activate(int index, bool additive)
        {
            string path = _paths[index];

            // An already loaded scene has nothing to add, and asking for it again would only reload it.
            if (additive && IsOpen(path)) return;

            Close();

            // Deferred: the single open asks about unsaved changes, and a modal dialog raised from
            // inside a dropdown's own GUI pass is not something to do to the editor.
            OpenSceneMode mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            EditorApplication.delayCall += () => OpenScene(path, mode);
        }

        /// <summary>
        /// Middle-click points at the scene asset in the Project window instead of opening it, which
        /// is how to find where a scene actually lives without loading it and losing what is open.
        /// Nothing is selected — a ping frames and flashes the row on its own, and taking the
        /// selection would redirect every locked Inspector along with it.
        /// </summary>
        protected override void AlternateActivate(int index)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(_paths[index]);
            if (!asset) return;

            // Closed first: the panel is a dropdown sitting over the Project window, so a ping behind
            // it would frame the row where it cannot be seen. Deferred for the same reason the scene
            // open is — reaching into another window from inside this one's GUI pass, as it is being
            // torn down, is not something to do to the editor.
            Close();

            EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
        }

        private static void OpenScene(string path, OpenSceneMode mode)
        {
            // Only the single open replaces what is loaded, so only that one has anything to save first.
            if (mode == OpenSceneMode.Single && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(path, mode);
        }

        private static bool IsOpen(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path == path) return true;
            }

            return false;
        }
    }
}
