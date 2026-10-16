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
    /// Starred scenes are listed above the rest with a divider between them, in an order the user
    /// sets by dragging a starred row, or with Alt+Up/Down. Open scenes are marked
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

        /// <summary>Every starred scene in the user's order, searched or not, which is what a move is measured against.</summary>
        private readonly List<string> _favorites = new List<string>();

        /// <summary>Which of the three groups each row belongs to, which is all a divider needs to know.</summary>
        private readonly List<int> _groups = new List<int>();

        private readonly GUIContent _rowContent = new GUIContent();
        private readonly GUIContent _starContent = new GUIContent();

        /// <summary>
        /// Told once the window is gone, however it went — picked, dismissed, escaped or destroyed
        /// before it ever showed. The header that opened it keeps its arrow lit until this fires.
        /// </summary>
        private Action _closed;

        protected override int RowCount => _paths.Count;

        protected override bool ShowSearchField => true;

        protected override string EmptySearchText => "No scenes match";

        protected override void OnSearchChanged(string query) => Rebuild();

        /// <param name="activator">The header's name rect in screen space, so the list drops from the name it belongs to.</param>
        /// <param name="closed">
        /// Run when the window goes away. Attached before anything can fail, so the empty-project case
        /// below reports through OnDestroy like every other close rather than needing a path of its own.
        /// </param>
        public static void Open(Rect activator, Action closed = null)
        {
            HierarchySceneMenuWindow window = CreateInstance<HierarchySceneMenuWindow>();
            window._closed = closed;

            window.Rebuild();

            if (window.RowCount == 0)
            {
                DestroyImmediate(window);
                return;
            }

            window.SetHighlight(window._groups.IndexOf(0));
            window.ShowList(activator);
        }

        private void OnDestroy()
        {
            Action closed = _closed;
            _closed = null;

            closed?.Invoke();
        }

        /// <summary>
        /// Rebuilt from scratch whenever the order could have changed, which starring, reordering and
        /// typing in the search field all do. Group 0 is starred and 1 is everything else, since a
        /// search narrows the list rather than changing what is worth keeping at the top of it.
        ///
        /// Starred scenes keep the user's order, searched or not. The rest are ordered by how well the
        /// name matches and then by the name itself — an empty query matches everything at the same
        /// cost, so the unsearched list reads alphabetically as it always did.
        /// </summary>
        private void Rebuild()
        {
            List<KeyValuePair<string, float>> matched = new List<KeyValuePair<string, float>>();
            List<string> favorites = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                if (IsFavorite(path)) favorites.Add(path);

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

            _favorites.Clear();
            _favorites.AddRange(OrderFavorites(favorites));

            HashSet<string> matchedPaths = new HashSet<string>();
            foreach (KeyValuePair<string, float> entry in matched) matchedPaths.Add(entry.Key);

            _paths.Clear();
            _groups.Clear();

            foreach (string path in _favorites)
            {
                if (!matchedPaths.Contains(path)) continue;

                _paths.Add(path);
                _groups.Add(0);
            }

            foreach (KeyValuePair<string, float> entry in matched)
            {
                if (IsFavorite(entry.Key)) continue;

                _paths.Add(entry.Key);
                _groups.Add(1);
            }
        }

        /// <summary>
        /// The stored order first, then any starred scene it does not name — one starred before the
        /// order was kept, or moved since — alphabetically after it.
        /// </summary>
        private static List<string> OrderFavorites(List<string> favorites)
        {
            List<string> stored = LoadFavoriteOrder();

            favorites.Sort((left, right) =>
            {
                int leftIndex = stored.IndexOf(left);
                int rightIndex = stored.IndexOf(right);

                if (leftIndex < 0) leftIndex = int.MaxValue;
                if (rightIndex < 0) rightIndex = int.MaxValue;

                int byOrder = leftIndex.CompareTo(rightIndex);

                return byOrder != 0
                    ? byOrder
                    : string.Compare(SceneName(left), SceneName(right), StringComparison.OrdinalIgnoreCase);
            });

            return favorites;
        }

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
            _rowContent.tooltip = _groups[index] == 0
                ? $"{path}\nMiddle-click to ping in the Project window\nDrag to reorder"
                : $"{path}\nMiddle-click to ping in the Project window";

            GUI.Label(Rect.MinMaxRect(rowRect.x + TickWidth, rowRect.y, starRect.x, rowRect.yMax),
                _rowContent, open ? EditorStyles.boldLabel : EditorStyles.label);

            GUI.color = previous;

            DrawStar(starRect, path, index);
        }

        /// <summary>Only the starred run, which is the only part of the list whose order is the user's.</summary>
        protected override bool IsRowReorderable(int index) => _groups[index] == 0;

        /// <summary>
        /// Placed against the visible neighbour it was dropped beside, so under a search the scene
        /// lands where it was seen to land, with any starred scenes the search hides keeping their places.
        /// </summary>
        protected override void MoveRow(int from, int to)
        {
            int neighbor = _favorites.IndexOf(_paths[to]);

            MoveFavorite(_paths[from], to < from ? neighbor : neighbor + 1);
        }

        /// <summary>
        /// Alt+Up/Down moves the highlighted starred scene past its visible neighbour, which under a
        /// search may step over starred scenes the search has hidden.
        /// </summary>
        protected override bool HandleRowKey(int index, Event evt)
        {
            if (!evt.alt || _groups[index] != 0) return false;
            if (evt.keyCode != KeyCode.UpArrow && evt.keyCode != KeyCode.DownArrow) return false;

            string path = _paths[index];
            int neighbor = evt.keyCode == KeyCode.UpArrow ? index - 1 : index + 1;

            // Consumed even at either end of the run, so the key does not fall through to moving the
            // highlight and read as the row having refused to move.
            if (neighbor < 0 || neighbor >= RowCount || _groups[neighbor] != 0) return true;

            int target = _favorites.IndexOf(_paths[neighbor]);
            MoveFavorite(path, evt.keyCode == KeyCode.UpArrow ? target : target + 1);

            SetHighlight(_paths.IndexOf(path));
            return true;
        }

        /// <param name="insertIndex">Where in the starred order the scene goes, counted before it is lifted out.</param>
        private void MoveFavorite(string path, int insertIndex)
        {
            List<string> order = new List<string>(_favorites);

            int from = order.IndexOf(path);
            if (from < 0) return;

            order.RemoveAt(from);
            if (insertIndex > from) insertIndex--;

            order.Insert(Mathf.Clamp(insertIndex, 0, order.Count), path);

            SaveFavoriteOrder(order);
            Rebuild();
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

            // A new star goes to the bottom of the starred run rather than wherever its name sorts,
            // so starring never shuffles an order the user has already arranged.
            List<string> order = LoadFavoriteOrder();
            order.Remove(path);
            if (favorite) order.Add(path);

            SaveFavoriteOrder(order);
        }

        private static List<string> LoadFavoriteOrder()
        {
            string stored = EditorPrefs.GetString(FavoriteOrderKey, string.Empty);

            return new List<string>(stored.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static void SaveFavoriteOrder(List<string> order)
        {
            if (order.Count == 0) EditorPrefs.DeleteKey(FavoriteOrderKey);
            else EditorPrefs.SetString(FavoriteOrderKey, string.Join("\n", order));
        }

        /// <summary>Scoped to the project the same way each scene's star is.</summary>
        private static string FavoriteOrderKey =>
            $"DNExtensions.HelpfulEditor.SceneFavoriteOrder.{Application.dataPath.GetHashCode():X8}";

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
