using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Editor window for browsing navigation history and managing favorite assets.
    /// </summary>
    internal class QuickNavigationWindow : EditorWindow
    {
        private int _selectedTab = 1;
        private int _selectedFavoriteTab = 0;
        private readonly string[] _tabs = { "History", "Favorites" };
        private Vector2 _scrollPosition;

        private const float TabButtonHeight = 24f;
        private const float ListPadding = 8f;
        private const float MainTabHorizontalPadding = 8f;
        private const float BottomBarHeight = 20f;

        private Vector2 SideButtonSize => new Vector2(25f, 20f);
        private static readonly Color BottomBarDestructiveColor = new Color(0.9f, 0.6f, 0.6f);

        private ReorderableList _historyList;
        private ReorderableList _favoritesList;

        private string _dragOriginGuid = null;
        private string _selectedHistoryGuid = null;
        private int _dragOriginFavoriteIndex = -1;
        private int _dragOriginTabIndex = -1;
        private Vector2 _dragStartPos;
        private readonly List<int> _favoriteDragIndices = new List<int>();

        private readonly HashSet<int> _selectedFavoriteIndices = new HashSet<int>();
        private int _favoriteSelectionAnchor = -1;
        private int _lastFavoriteTabForSelection = -1;

        private bool _isCreatingTab = false;
        private bool _isRenamingTab = false;
        private int _renamingTabIndex = -1;
        private string _newTabName = "New Tab";
        private Color _newTabColor = Color.white;

        [MenuItem("Tools/DNExtensions/Quick Navigation")]
        public static void ShowWindow()
        {
            var window = GetWindow<QuickNavigationWindow>("Quick Navigation");
            window.minSize = new Vector2(250, 300);
        }

        public static void RefreshWindow()
        {
            if (HasOpenInstances<QuickNavigationWindow>())
            {
                var windows = Resources.FindObjectsOfTypeAll<QuickNavigationWindow>();
                if (windows != null && windows.Length > 0)
                {
                    windows[0].Repaint();
                }
            }
        }

        private void RefreshAndRebuild()
        {
            _favoritesList = null;
            ClearFavoriteSelection();
            RefreshWindow();
        }

        private void ClearFavoriteSelection()
        {
            _selectedFavoriteIndices.Clear();
            _favoriteSelectionAnchor = -1;
            _favoriteDragIndices.Clear();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            Undo.undoRedoPerformed += OnUndoRedo;
            if (QuickNavigationData.Instance.HealFavorites())
                QuickNavigationData.Instance.Save();
            InitLists();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            QuickNavigationData.Instance.Save();
            RefreshAndRebuild();
        }

        private void InitLists()
        {
            var data = QuickNavigationData.Instance;

            if (_historyList == null)
            {
                _historyList = new ReorderableList(data.HistoryPaths, typeof(string), false, false, false, false);
                _historyList.drawElementCallback = DrawHistoryElement;
                _historyList.showDefaultBackground = false;
                _historyList.headerHeight = 0;
                _historyList.elementHeight = 24;
                _historyList.drawNoneElementCallback = (Rect rect) => {
                    EditorGUI.LabelField(rect, "Click on any asset in the Project window to populate history.", EditorStyles.centeredGreyMiniLabel);
                };
            }

            var activeFavList = new List<FavoriteEntry>();
            if (data.FavoriteTabs != null && data.FavoriteTabs.Count > 0 && _selectedFavoriteTab >= 0 && _selectedFavoriteTab < data.FavoriteTabs.Count && !data.FavoriteTabs[_selectedFavoriteTab].IsArchived)
            {
                activeFavList = data.FavoriteTabs[_selectedFavoriteTab].GetEntries();
            }

            _favoritesList = new ReorderableList(activeFavList, typeof(FavoriteEntry), true, false, false, false);
            _favoritesList.drawElementCallback = DrawFavoriteElement;
            _favoritesList.showDefaultBackground = false;

            _favoritesList.onSelectCallback = (ReorderableList list) => {
                QuickNavigationData.Instance.RecordUndo("Reorder Favorites");
            };

            _favoritesList.onReorderCallback = (ReorderableList list) => {
                QuickNavigationData.Instance.Save();
                ClearFavoriteSelection();
            };
            _favoritesList.headerHeight = 0;
            _favoritesList.elementHeight = 24;
            _favoritesList.drawNoneElementCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Drag assets or external folders here to add to favorites.", EditorStyles.centeredGreyMiniLabel);
            };
        }

        private void DrawHistoryElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var data = QuickNavigationData.Instance;
            var list = data.HistoryPaths;
            if (index >= list.Count) return;

            string stored = list[index];
            string path = QuickNavigationAssets.ResolveToPath(stored);
            Object obj = QuickNavigationAssets.Load(stored);

            DrawAssetRow(
                rect,
                index,
                isActive,
                false,
                obj,
                path,
                isExternalFolder: false,
                isExternalResolved: false,
                externalDisplayName: null,
                onPrimaryClick: e => {
                    SelectHistoryItem(stored, obj);
                    e.Use();
                },
                beginDrag: () => {
                    _dragOriginGuid = stored;
                    _dragOriginFavoriteIndex = -1;
                    _dragStartPos = Event.current.mousePosition;
                },
                isDragSource: () => _dragOriginGuid == stored,
                resetDrag: () => {
                    _dragOriginGuid = null;
                    _dragStartPos = Event.current.mousePosition;
                },
                getDragPayload: () => obj != null && !string.IsNullOrEmpty(path) ? (new[] { obj }, new[] { path }) : null,
                drawSideButtons: entryRect => DrawHistorySideButtons(data, stored, entryRect),
                onAssignSelection: o => SelectHistoryItem(stored, o));
        }

        private void DrawFavoriteElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var data = QuickNavigationData.Instance;
            var list = (List<FavoriteEntry>)_favoritesList.list;
            if (index >= list.Count) return;

            var entry = list[index];
            string path = null;
            Object obj = null;
            bool isResolved = QuickNavigationAssets.TryResolve(entry, out path, out obj, out bool entryUpdated);
            if (entryUpdated)
                data.Save();

            bool isRowSelected = _selectedFavoriteIndices.Contains(index);
            bool isExternalFolder = entry != null && entry.IsExternal;

            DrawAssetRow(
                rect,
                index,
                isActive,
                isRowSelected,
                obj,
                path,
                isExternalFolder: isExternalFolder,
                isExternalResolved: isResolved,
                externalDisplayName: QuickNavigationAssets.GetDisplayName(entry, path),
                onPrimaryClick: e => HandleFavoriteSelection(index, e),
                beginDrag: () => BeginFavoriteDrag(index),
                isDragSource: () => _dragOriginFavoriteIndex == index,
                resetDrag: () => {
                    _dragOriginFavoriteIndex = -1;
                    _favoriteDragIndices.Clear();
                    _dragStartPos = Event.current.mousePosition;
                },
                getDragPayload: () => BuildFavoriteDragPayload(index),
                drawSideButtons: entryRect => DrawFavoriteSideButtons(index, entryRect));
        }

        private void HandleFavoriteSelection(int index, Event e)
        {
            if (e.button != 0 || e.clickCount > 1)
                return;

            bool rangeSelect = e.shift;
            bool toggleSelect = EditorGUI.actionKey;

            if (rangeSelect && _favoriteSelectionAnchor >= 0)
            {
                if (!toggleSelect)
                    _selectedFavoriteIndices.Clear();

                int start = Mathf.Min(_favoriteSelectionAnchor, index);
                int end = Mathf.Max(_favoriteSelectionAnchor, index);
                for (int i = start; i <= end; i++)
                    _selectedFavoriteIndices.Add(i);
            }
            else if (toggleSelect)
            {
                if (_selectedFavoriteIndices.Contains(index))
                    _selectedFavoriteIndices.Remove(index);
                else
                    _selectedFavoriteIndices.Add(index);

                _favoriteSelectionAnchor = index;
            }
            else
            {
                _selectedFavoriteIndices.Clear();
                _selectedFavoriteIndices.Add(index);
                _favoriteSelectionAnchor = index;
            }

            if (_favoritesList != null)
                _favoritesList.index = index;

            ApplyFavoriteSelectionToProject();
            Repaint();
            e.Use();
        }

        private void SelectHistoryItem(string stored, Object obj)
        {
            if (obj == null)
                return;

            _selectedHistoryGuid = stored;
            Selection.activeObject = obj;
        }

        private void SyncHistoryListSelection()
        {
            if (_historyList == null || string.IsNullOrEmpty(_selectedHistoryGuid))
                return;

            int index = QuickNavigationData.Instance.HistoryPaths.IndexOf(_selectedHistoryGuid);
            if (index >= 0)
                _historyList.index = index;
        }

        private void ApplyFavoriteSelectionToProject()
        {
            var list = GetActiveFavoriteEntries();
            if (list == null)
                return;

            var objects = new List<Object>();
            foreach (int i in _selectedFavoriteIndices.OrderBy(x => x))
            {
                if (i < 0 || i >= list.Count)
                    continue;

                if (list[i].IsExternal)
                    continue;

                if (QuickNavigationAssets.TryResolve(list[i], out _, out Object obj, out _) && obj != null)
                    objects.Add(obj);
            }

            Selection.objects = objects.ToArray();
        }

        private void SelectAllFavorites()
        {
            var list = GetActiveFavoriteEntries();
            if (list == null || list.Count == 0)
                return;

            _selectedFavoriteIndices.Clear();
            for (int i = 0; i < list.Count; i++)
                _selectedFavoriteIndices.Add(i);

            _favoriteSelectionAnchor = 0;

            if (_favoritesList != null)
                _favoritesList.index = list.Count - 1;

            ApplyFavoriteSelectionToProject();
            Repaint();
        }

        private List<FavoriteEntry> GetActiveFavoriteEntries()
        {
            var data = QuickNavigationData.Instance;
            if (data.FavoriteTabs.Count == 0 || _selectedFavoriteTab < 0 || _selectedFavoriteTab >= data.FavoriteTabs.Count)
                return null;

            return data.FavoriteTabs[_selectedFavoriteTab].GetEntries();
        }

        private void BeginFavoriteDrag(int index)
        {
            _dragOriginGuid = null;
            _dragOriginFavoriteIndex = index;
            _dragStartPos = Event.current.mousePosition;
            _favoriteDragIndices.Clear();

            if (_selectedFavoriteIndices.Contains(index) && _selectedFavoriteIndices.Count > 1)
                _favoriteDragIndices.AddRange(_selectedFavoriteIndices.OrderBy(i => i));
            else
                _favoriteDragIndices.Add(index);
        }

        private (Object[] objects, string[] paths)? BuildFavoriteDragPayload(int index)
        {
            var list = GetActiveFavoriteEntries();
            if (list == null)
                return null;

            var indices = _favoriteDragIndices.Count > 0 ? _favoriteDragIndices : new List<int> { index };

            var objectList = new List<Object>();
            var pathList = new List<string>();

            foreach (int i in indices)
            {
                if (i < 0 || i >= list.Count)
                    continue;

                if (!QuickNavigationAssets.TryResolve(list[i], out string path, out Object obj, out _))
                    continue;

                if (list[i].IsExternal)
                {
                    pathList.Add(path);
                    continue;
                }

                if (obj == null)
                    continue;

                objectList.Add(obj);
                pathList.Add(path);
            }

            if (pathList.Count == 0)
                return null;

            return (objectList.ToArray(), pathList.ToArray());
        }

        private void RemoveFavoritesAt(int index)
        {
            var data = QuickNavigationData.Instance;
            var list = GetActiveFavoriteEntries();
            if (list == null || index < 0 || index >= list.Count)
                return;

            data.RecordUndo("Remove Favorite");

            if (_selectedFavoriteIndices.Count > 1 && _selectedFavoriteIndices.Contains(index))
            {
                foreach (int i in _selectedFavoriteIndices.OrderByDescending(x => x))
                {
                    if (i >= 0 && i < list.Count)
                        list.RemoveAt(i);
                }

                ClearFavoriteSelection();
            }
            else
            {
                list.RemoveAt(index);
                AdjustFavoriteSelectionAfterRemove(index);
            }

            data.Save();
            RefreshAndRebuild();
        }

        private void RemoveSelectedFavorites()
        {
            var data = QuickNavigationData.Instance;
            var list = GetActiveFavoriteEntries();
            if (list == null || _selectedFavoriteIndices.Count == 0)
                return;

            data.RecordUndo("Remove Favorites");
            foreach (int i in _selectedFavoriteIndices.OrderByDescending(x => x))
            {
                if (i >= 0 && i < list.Count)
                    list.RemoveAt(i);
            }

            ClearFavoriteSelection();
            data.Save();
            RefreshAndRebuild();
        }

        private void AdjustFavoriteSelectionAfterRemove(int removedIndex)
        {
            var updated = new HashSet<int>();
            foreach (int i in _selectedFavoriteIndices)
            {
                if (i < removedIndex)
                    updated.Add(i);
                else if (i > removedIndex)
                    updated.Add(i - 1);
            }

            _selectedFavoriteIndices.Clear();
            foreach (int i in updated)
                _selectedFavoriteIndices.Add(i);

            if (_favoriteSelectionAnchor == removedIndex)
                _favoriteSelectionAnchor = _selectedFavoriteIndices.Count > 0 ? _selectedFavoriteIndices.Max() : -1;
            else if (_favoriteSelectionAnchor > removedIndex)
                _favoriteSelectionAnchor--;
        }

        private void DrawAssetRow(
            Rect rect,
            int index,
            bool isActive,
            bool isRowSelected,
            Object obj,
            string path,
            bool isExternalFolder,
            bool isExternalResolved,
            string externalDisplayName,
            System.Action<Event> onPrimaryClick,
            System.Action beginDrag,
            System.Func<bool> isDragSource,
            System.Action resetDrag,
            System.Func<(Object[] objects, string[] paths)?> getDragPayload,
            System.Action<Rect> drawSideButtons,
            System.Action<Object> onAssignSelection = null)
        {
            if (isRowSelected)
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.4f, 0.8f, 0.35f));
            else if (!isActive && rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.08f));

            rect.y += 2;
            rect.height -= 4;

            float btnW = SideButtonSize.x;
            float btnH = SideButtonSize.y;
            float btnY = rect.y + (rect.height - btnH) / 2f;
            float rightMargin = btnW + 5f;

            Rect iconRect = new Rect(rect.x, rect.y, 20, rect.height);
            Rect labelRect = new Rect(iconRect.xMax + 5, rect.y, rect.width - rightMargin - 30, rect.height);
            Rect interactRect = new Rect(rect.x + 28f, rect.y, rect.width - rightMargin - 28f, rect.height);
            Rect btnRect = new Rect(rect.x + rect.width - btnW - 2f, btnY, btnW, btnH);

            Event e = Event.current;

            if (obj == null && isExternalFolder)
            {
                bool isValidExternal = isExternalResolved && !string.IsNullOrEmpty(path);
                string label = isValidExternal
                    ? (string.IsNullOrEmpty(externalDisplayName) ? path : externalDisplayName)
                    : "Missing External Folder";

                if (isValidExternal)
                    QuickNavigationAssets.DrawExternalFolderIcon(iconRect);

                if (e.type == EventType.MouseDown && interactRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                    {
                        if (e.clickCount == 2 && isValidExternal)
                        {
                            beginDrag();
                            EditorUtility.RevealInFinder(path);
                            e.Use();
                        }
                        else if (e.clickCount == 1 && onPrimaryClick != null)
                        {
                            beginDrag();
                            onPrimaryClick(e);
                            e.Use();
                        }
                    }
                    else if (e.button == 2 && isValidExternal)
                    {
                        if (onPrimaryClick != null)
                            onPrimaryClick(e);
                        EditorUtility.RevealInFinder(path);
                        e.Use();
                    }
                }

                if (e.type == EventType.MouseDrag && e.button == 0 && isDragSource() && isValidExternal)
                {
                    if (Vector2.Distance(e.mousePosition, _dragStartPos) > 3f)
                    {
                        var payload = getDragPayload?.Invoke();
                        if (payload == null)
                            return;

                        int sourceTab = _dragOriginFavoriteIndex >= 0 ? _selectedFavoriteTab : -1;
                        resetDrag();
                        GUIUtility.hotControl = 0;

                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("QuickNav_InternalDrag", true);
                        DragAndDrop.SetGenericData("QuickNav_SourceTab", sourceTab);
                        DragAndDrop.objectReferences = payload.Value.objects ?? System.Array.Empty<Object>();
                        DragAndDrop.paths = payload.Value.paths;
                        string dragLabel = payload.Value.paths.Length > 1 ? $"{payload.Value.paths.Length} Folders" : "External Folder";
                        DragAndDrop.StartDrag(dragLabel);
                        e.Use();
                    }
                }

                if (!isValidExternal)
                {
                    GUI.contentColor = new Color(1f, 0.4f, 0.4f);
                    GUI.Label(labelRect, label);
                    GUI.contentColor = Color.white;
                }
                else
                {
                    GUI.Label(labelRect, label);
                }

                drawSideButtons(btnRect);
                return;
            }

            if (obj == null)
            {
                if (e.type == EventType.MouseDown && interactRect.Contains(e.mousePosition) && e.button == 0 && e.clickCount == 1 && onPrimaryClick != null)
                {
                    beginDrag();
                    onPrimaryClick(e);
                }

                GUI.contentColor = new Color(1f, 0.4f, 0.4f);
                GUI.Label(labelRect, "Missing/Deleted Asset");
                GUI.contentColor = Color.white;
                drawSideButtons(btnRect);
                return;
            }

            Texture icon = AssetPreview.GetMiniThumbnail(obj);
            if (icon != null) GUI.Label(iconRect, icon);

            GUI.Label(labelRect, obj.name);

            if (e.type == EventType.MouseDown && interactRect.Contains(e.mousePosition))
            {
                if (e.button == 0)
                {
                    if (e.clickCount == 2)
                    {
                        beginDrag();
                        AssetDatabase.OpenAsset(obj);
                        e.Use();
                    }
                    else if (e.clickCount == 1)
                    {
                        beginDrag();

                        if (onPrimaryClick != null)
                            onPrimaryClick(e);
                        else if (onAssignSelection != null)
                            onAssignSelection(obj);
                        else
                            Selection.activeObject = obj;
                    }
                }
                else if (e.button == 2)
                {
                    if (onPrimaryClick != null && _selectedFavoriteIndices.Contains(index))
                        ApplyFavoriteSelectionToProject();
                    else if (onAssignSelection != null)
                        onAssignSelection(obj);
                    else
                        Selection.activeObject = obj;

                    EditorGUIUtility.PingObject(obj);
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && isDragSource())
            {
                if (Vector2.Distance(e.mousePosition, _dragStartPos) > 3f)
                {
                    var payload = getDragPayload?.Invoke();
                    if (payload == null)
                        return;

                    int sourceTab = _dragOriginFavoriteIndex >= 0 ? _selectedFavoriteTab : -1;
                    resetDrag();
                    GUIUtility.hotControl = 0;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("QuickNav_InternalDrag", true);
                    DragAndDrop.SetGenericData("QuickNav_SourceTab", sourceTab);
                    DragAndDrop.objectReferences = payload.Value.objects ?? System.Array.Empty<Object>();
                    DragAndDrop.paths = payload.Value.paths;
                    string dragLabel = payload.Value.paths.Length > 1
                        ? $"{payload.Value.paths.Length} Items"
                        : (payload.Value.objects != null && payload.Value.objects.Length > 0 ? payload.Value.objects[0].name : "Favorite");
                    DragAndDrop.StartDrag(dragLabel);
                    e.Use();
                }
            }

            if (e.type == EventType.ContextClick && interactRect.Contains(e.mousePosition))
            {
                if (onPrimaryClick != null)
                {
                    if (!_selectedFavoriteIndices.Contains(index))
                    {
                        _selectedFavoriteIndices.Clear();
                        _selectedFavoriteIndices.Add(index);
                        _favoriteSelectionAnchor = index;
                    }

                    ApplyFavoriteSelectionToProject();
                }
                else
                {
                    Selection.activeObject = obj;
                }

                EditorUtility.DisplayPopupMenu(new Rect(e.mousePosition, Vector2.zero), "Assets/", null);
                e.Use();
            }

            drawSideButtons(btnRect);
        }

        private void DrawFavoriteSideButtons(int index, Rect btnRect)
        {
            GUI.backgroundColor = new Color(0.9f, 0.6f, 0.6f);
            if (GUI.Button(btnRect, "X", GUI.skin.button))
            {
                int capturedIndex = index;
                EditorApplication.delayCall += () => RemoveFavoritesAt(capturedIndex);
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawHistorySideButtons(QuickNavigationData data, string stored, Rect btnRect)
        {
            if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    var candidate = QuickNavigationAssets.CreateEntry(stored);

                    for (int i = 0; i < data.FavoriteTabs.Count; i++)
                    {
                        if (data.FavoriteTabs[i].IsArchived) continue;

                        int tabIndex = i;
                        string tabName = data.FavoriteTabs[i].TabName;

                        if (data.FavoriteTabs[i].ContainsEntry(candidate))
                            menu.AddDisabledItem(new GUIContent(tabName + " (Already Added)"));
                        else
                            menu.AddItem(new GUIContent(tabName), false, () => {
                                data.AddToFavorites(stored, tabIndex);
                                RefreshAndRebuild();
                            });
                    }

                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }

            var currentTab = data.FavoriteTabs.Count > 0 &&
                             _selectedFavoriteTab >= 0 &&
                             _selectedFavoriteTab < data.FavoriteTabs.Count
                ? data.FavoriteTabs[_selectedFavoriteTab]
                : null;

            bool inCurrentTab = currentTab != null && currentTab.ContainsEntry(QuickNavigationAssets.CreateEntry(stored));

            if (!inCurrentTab)
            {
                GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
                if (GUI.Button(btnRect, "+", GUI.skin.button))
                {
                    EditorApplication.delayCall += () => {
                        data.AddToFavorites(stored, _selectedFavoriteTab);
                        RefreshAndRebuild();
                    };
                }
                GUI.backgroundColor = Color.white;
            }
        }

        private void OnGUI()
        {
            if (Event.current != null)
            {
                if (Event.current.type == EventType.MouseMove) Repaint();

                if (_selectedTab == 1 && _selectedFavoriteTab != _lastFavoriteTabForSelection)
                {
                    ClearFavoriteSelection();
                    _lastFavoriteTabForSelection = _selectedFavoriteTab;
                }

                if (_selectedTab == 1 && Event.current.type == EventType.KeyDown &&
                    !EditorGUIUtility.editingTextField)
                {
                    if ((Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace) &&
                        _selectedFavoriteIndices.Count > 0)
                    {
                        RemoveSelectedFavorites();
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.A &&
                             (Event.current.command || Event.current.control))
                    {
                        SelectAllFavorites();
                        Event.current.Use();
                    }
                }

                if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.DragExited)
                {
                    _dragOriginGuid = null;
                    _dragOriginFavoriteIndex = -1;
                    _dragOriginTabIndex = -1;
                    _favoriteDragIndices.Clear();
                    DragAndDrop.SetGenericData("QuickNav_TabDrag", null);
                    DragAndDrop.SetGenericData("QuickNav_SourceTab", null);
                    DragAndDrop.SetGenericData("QuickNav_InternalDrag", null);
                }
            }

            if (_historyList == null || _favoritesList == null) InitLists();

            var data = QuickNavigationData.Instance;

            GUILayout.Space(5);
            DrawMainTabs();
            GUILayout.Space(5);

            if (_selectedTab == 0)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
                GUILayout.Space(ListPadding);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                SyncHistoryListSelection();
                _historyList.DoLayoutList();
                EditorGUILayout.EndScrollView();

                DrawBottomBarSeparator();
                DrawHistoryBottomBar(data);

                EditorGUILayout.EndVertical();
                GUILayout.Space(ListPadding);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                bool hasActiveFavoriteTab = data.FavoriteTabs.Count > 0 &&
                                            _selectedFavoriteTab >= 0 &&
                                            _selectedFavoriteTab < data.FavoriteTabs.Count &&
                                            !data.FavoriteTabs[_selectedFavoriteTab].IsArchived;

                EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
                GUILayout.Space(ListPadding);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

                DrawSubTabs(data);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

                if (hasActiveFavoriteTab)
                {
                    var activeTabList = data.FavoriteTabs[_selectedFavoriteTab].GetEntries();
                    if (_favoritesList.list != activeTabList)
                    {
                        _favoritesList = null;
                        InitLists();
                    }

                    if (_favoritesList != null)
                        _favoritesList.DoLayoutList();
                }

                HandleGlobalDragAndDrop();
                EditorGUILayout.EndScrollView();

                if (hasActiveFavoriteTab)
                {
                    DrawBottomBarSeparator();
                    DrawFavoritesBottomBar(data);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(ListPadding);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSubTabs(QuickNavigationData data)
        {
            if (data.FavoriteTabs.Count == 0) return;

            if (_selectedFavoriteTab < 0 || _selectedFavoriteTab >= data.FavoriteTabs.Count || data.FavoriteTabs[_selectedFavoriteTab].IsArchived)
            {
                _selectedFavoriteTab = data.FavoriteTabs.FindIndex(t => !t.IsArchived);
                if (_selectedFavoriteTab == -1) _selectedFavoriteTab = 0;
            }

            GUILayout.Space(4f);

            float maxWidth = EditorGUIUtility.currentViewWidth - (ListPadding * 2f) - 12f;
            if (maxWidth < 50f) maxWidth = 500f;

            float currentX = 0f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4f);

            for (int i = 0; i < data.FavoriteTabs.Count; i++)
            {
                if (data.FavoriteTabs[i].IsArchived) continue;

                GUIStyle style = (i == 0) ? EditorStyles.miniButtonLeft : EditorStyles.miniButtonMid;
                GUIContent content = new GUIContent(data.FavoriteTabs[i].TabName);

                float tabWidth = style.CalcSize(content).x;
                if (currentX + tabWidth > maxWidth && currentX > 0f)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(4f);
                    currentX = 0f;
                }

                Rect tabRect = GUILayoutUtility.GetRect(content, style);
                currentX += tabWidth;
                Event e = Event.current;

                if (e.type == EventType.MouseDown && e.button == 1 && tabRect.Contains(e.mousePosition))
                {
                    ShowTabContextMenu(data, i);
                    e.Use();
                }

                if (e.type == EventType.MouseDown && e.button == 0 && tabRect.Contains(e.mousePosition))
                {
                    _dragOriginTabIndex = i;
                    _dragStartPos = e.mousePosition;
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && _dragOriginTabIndex == i)
                {
                    if (Vector2.Distance(e.mousePosition, _dragStartPos) > 3f)
                    {
                        GUIUtility.hotControl = 0;
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.SetGenericData("QuickNav_TabDrag", i);
                        DragAndDrop.StartDrag("Reorder Tab");
                        _dragOriginTabIndex = -1;
                        e.Use();
                    }
                }

                Color tabColor = data.FavoriteTabs[i].TabColor;
                if (tabColor.a == 0f) tabColor = Color.white;

                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = tabColor;

                bool isSelected = (_selectedFavoriteTab == i);
                bool toggled = GUI.Toggle(tabRect, isSelected, content, style);

                GUI.backgroundColor = prevColor;

                if (toggled && !isSelected)
                {
                    _selectedFavoriteTab = i;
                    ClearFavoriteSelection();
                }

                if (tabRect.Contains(e.mousePosition) && DragAndDrop.GetGenericData("QuickNav_TabDrag") != null)
                {
                    bool isLeftHalf = e.mousePosition.x < tabRect.x + (tabRect.width / 2f);
                    int targetIndex = isLeftHalf ? i : i + 1;
                    int srcIndex = (int)DragAndDrop.GetGenericData("QuickNav_TabDrag");

                    if (e.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        e.Use();
                    }
                    else if (e.type == EventType.DragPerform)
                    {
                        if (srcIndex != targetIndex && srcIndex != targetIndex - 1)
                        {
                            var activeTabObj = data.FavoriteTabs[_selectedFavoriteTab];
                            var movedTab = data.FavoriteTabs[srcIndex];

                            EditorApplication.delayCall += () => {
                                data.RecordUndo("Reorder Tab");
                                data.FavoriteTabs.RemoveAt(srcIndex);
                                int adjTarget = srcIndex < targetIndex ? targetIndex - 1 : targetIndex;
                                data.FavoriteTabs.Insert(adjTarget, movedTab);

                                _selectedFavoriteTab = data.FavoriteTabs.IndexOf(activeTabObj);

                                data.Save();
                                RefreshAndRebuild();
                            };
                        }

                        DragAndDrop.AcceptDrag();
                        DragAndDrop.SetGenericData("QuickNav_TabDrag", null);
                        e.Use();
                    }
                    else if (e.type == EventType.Repaint)
                    {
                        float markerX = isLeftHalf ? tabRect.xMin : tabRect.xMax;
                        Rect markerRect = new Rect(markerX - 1.5f, tabRect.y, 3f, tabRect.height);
                        EditorGUI.DrawRect(markerRect, new Color(0.2f, 0.6f, 1f, 1f));
                    }
                }

                bool isAssetDrag = DragAndDrop.paths != null && DragAndDrop.paths.Length > 0 && HasAcceptableDraggedPaths(DragAndDrop.paths);
                bool isTabDrag = DragAndDrop.GetGenericData("QuickNav_TabDrag") != null;
                bool isNativeControlDrag = GUIUtility.hotControl != 0;

                if (tabRect.Contains(e.mousePosition) && isAssetDrag && !isTabDrag && !isNativeControlDrag)
                {
                    object rawSourceTab = DragAndDrop.GetGenericData("QuickNav_SourceTab");
                    int sourceTab = rawSourceTab != null ? (int)rawSourceTab : -1;
                    bool isMove = sourceTab != -1;

                    if (e.type == EventType.DragUpdated)
                    {
                        if (sourceTab == i) DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        else DragAndDrop.visualMode = isMove ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Copy;
                        e.Use();
                    }
                    else if (e.type == EventType.DragPerform)
                    {
                        if (sourceTab != i)
                        {
                            DragAndDrop.AcceptDrag();
                            string[] droppedPaths = DragAndDrop.paths;
                            int dropTarget = i;

                            EditorApplication.delayCall += () => {
                                data.RecordUndo("Move Asset to Tab");
                                foreach (string p in droppedPaths)
                                {
                                    if (string.IsNullOrEmpty(p))
                                        continue;

                                    var movedEntry = QuickNavigationAssets.CreateEntry(p);
                                    if (!QuickNavigationAssets.IsAddable(movedEntry))
                                        continue;

                                    if (isMove && sourceTab >= 0 && sourceTab < data.FavoriteTabs.Count)
                                        data.FavoriteTabs[sourceTab].RemoveMatchingEntry(movedEntry);

                                    data.AddToFavorites(p, dropTarget);
                                }

                                GUIUtility.hotControl = 0;
                                GUIUtility.keyboardControl = 0;

                                data.Save();
                                RefreshAndRebuild();
                            };
                        }

                        DragAndDrop.SetGenericData("QuickNav_InternalDrag", null);
                        DragAndDrop.SetGenericData("QuickNav_SourceTab", null);
                        e.Use();
                    }
                    else if (e.type == EventType.Repaint)
                    {
                        if (sourceTab != i) EditorGUI.DrawRect(tabRect, new Color(1f, 1f, 1f, 0.15f));
                    }
                }
            }

            bool hasArchivedTabs = data.FavoriteTabs.Any(t => t.IsArchived);

            float plusBtnWidth = 25f;
            float archiveBtnWidth = 25f;
            float totalExtraWidth = plusBtnWidth + (hasArchivedTabs ? archiveBtnWidth : 0f);

            if (currentX + totalExtraWidth > maxWidth && currentX > 0f)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4f);
            }

            GUIStyle plusStyle = hasArchivedTabs ? EditorStyles.miniButtonMid : EditorStyles.miniButtonRight;

            if (GUILayout.Button("+", plusStyle, GUILayout.Width(plusBtnWidth)))
            {
                _isCreatingTab = true;
                _isRenamingTab = false;
                _newTabName = "New Tab";
                _newTabColor = Color.white;
            }

            if (hasArchivedTabs)
            {
                GUIContent archiveIcon = new GUIContent(EditorGUIUtility.IconContent("Folder Icon").image, "View Archived Tabs");
                if (GUILayout.Button(archiveIcon, EditorStyles.miniButtonRight, GUILayout.Width(archiveBtnWidth), GUILayout.Height(18f)))
                {
                    ShowArchiveMenu(data);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (_isCreatingTab || _isRenamingTab)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4f);

                _newTabName = EditorGUILayout.TextField(_newTabName, GUILayout.ExpandWidth(true), GUILayout.MinWidth(50));
                _newTabColor = EditorGUILayout.ColorField(GUIContent.none, _newTabColor, false, false, false, GUILayout.Width(40));

                string btnText = _isCreatingTab ? "Add" : "Save";

                if (GUILayout.Button(btnText, GUILayout.Width(45)))
                {
                    if (_isCreatingTab)
                    {
                        data.RecordUndo("Create Tab");
                        data.FavoriteTabs.Add(new FavoriteTab { TabName = _newTabName, TabColor = _newTabColor });
                        _selectedFavoriteTab = data.FavoriteTabs.Count - 1;
                    }
                    else if (_isRenamingTab && _renamingTabIndex >= 0 && _renamingTabIndex < data.FavoriteTabs.Count)
                    {
                        data.RecordUndo("Edit Tab");
                        data.FavoriteTabs[_renamingTabIndex].TabName = _newTabName;
                        data.FavoriteTabs[_renamingTabIndex].TabColor = _newTabColor;
                    }

                    data.Save();
                    _isCreatingTab = false;
                    _isRenamingTab = false;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    _isCreatingTab = false;
                    _isRenamingTab = false;
                    GUI.FocusControl(null);
                }

                GUILayout.Space(4f);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }

        private void ShowTabContextMenu(QuickNavigationData data, int tabIndex)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Add External Folder"), false, () => {
                AddExternalFolder(data, tabIndex);
            });

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Clear Tab"), false, () => {
                if (EditorUtility.DisplayDialog("Clear Favorites", $"Remove all favorites in '{data.FavoriteTabs[tabIndex].TabName}'?", "Yes", "Cancel"))
                {
                    data.RecordUndo("Clear Tab");
                    data.FavoriteTabs[tabIndex].GetEntries().Clear();
                    data.Save();
                    RefreshAndRebuild();
                }
            });

            menu.AddItem(new GUIContent("Edit Tab"), false, () => {
                _isRenamingTab = true;
                _isCreatingTab = false;
                _renamingTabIndex = tabIndex;
                _newTabName = data.FavoriteTabs[tabIndex].TabName;
                _newTabColor = data.FavoriteTabs[tabIndex].TabColor;
                if (_newTabColor.a == 0f) _newTabColor = Color.white;
                RefreshWindow();
            });

            menu.AddItem(new GUIContent("Duplicate Tab"), false, () => {
                int newIndex = data.DuplicateFavoriteTab(tabIndex);
                if (newIndex >= 0)
                {
                    _selectedFavoriteTab = newIndex;
                    ClearFavoriteSelection();
                    RefreshAndRebuild();
                }
            });

            menu.AddItem(new GUIContent("Archive Tab"), false, () => {
                data.RecordUndo("Archive Tab");
                data.FavoriteTabs[tabIndex].IsArchived = true;
                _selectedFavoriteTab = data.FavoriteTabs.FindIndex(t => !t.IsArchived);
                if (_selectedFavoriteTab == -1) _selectedFavoriteTab = 0;
                data.Save();
                RefreshAndRebuild();
            });

            if (data.FavoriteTabs.Count > 1)
            {
                menu.AddItem(new GUIContent("Delete Tab"), false, () => {
                    if (EditorUtility.DisplayDialog("Delete Tab", $"Delete tab '{data.FavoriteTabs[tabIndex].TabName}' and all its favorites?", "Yes", "Cancel"))
                    {
                        data.RecordUndo("Delete Tab");
                        data.FavoriteTabs.RemoveAt(tabIndex);

                        _selectedFavoriteTab = data.FavoriteTabs.FindIndex(t => !t.IsArchived);
                        if (_selectedFavoriteTab == -1) _selectedFavoriteTab = 0;

                        if (_renamingTabIndex == tabIndex) _isRenamingTab = false;

                        data.Save();
                        RefreshAndRebuild();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Delete Tab"));
            }

            menu.ShowAsContext();
        }

        private void ShowArchiveMenu(QuickNavigationData data)
        {
            GenericMenu menu = new GenericMenu();
            bool hasArchives = false;

            for (int i = 0; i < data.FavoriteTabs.Count; i++)
            {
                if (data.FavoriteTabs[i].IsArchived)
                {
                    hasArchives = true;
                    int tabIndex = i;

                    menu.AddItem(new GUIContent($"Restore '{data.FavoriteTabs[i].TabName}'"), false, () => {
                        data.RecordUndo("Restore Tab");
                        data.FavoriteTabs[tabIndex].IsArchived = false;
                        _selectedFavoriteTab = tabIndex;
                        data.Save();
                        RefreshAndRebuild();
                    });
                }
            }

            if (!hasArchives)
            {
                menu.AddDisabledItem(new GUIContent("No Archived Tabs"));
            }

            menu.ShowAsContext();
        }

        private void DrawMainTabs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(MainTabHorizontalPadding);

            Rect toolbarRect = GUILayoutUtility.GetRect(0, TabButtonHeight, GUILayout.ExpandWidth(true));
            Event e = Event.current;

            if (IsToolbarRightClick(e, toolbarRect))
            {
                int tabIndex = GetToolbarTabIndexAt(toolbarRect, e.mousePosition.x);
                if (tabIndex == 0)
                {
                    ShowHistoryContextMenu();
                    e.Use();
                }
            }

            _selectedTab = GUI.Toolbar(toolbarRect, _selectedTab, _tabs);

            GUILayout.Space(MainTabHorizontalPadding);
            EditorGUILayout.EndHorizontal();
        }

        private static bool IsToolbarRightClick(Event e, Rect rect)
        {
            if (!rect.Contains(e.mousePosition))
                return false;

            return (e.type == EventType.MouseDown && e.button == 1) || e.type == EventType.ContextClick;
        }

        private int GetToolbarTabIndexAt(Rect toolbarRect, float mouseX)
        {
            float tabWidth = toolbarRect.width / _tabs.Length;
            return Mathf.Clamp(Mathf.FloorToInt((mouseX - toolbarRect.x) / tabWidth), 0, _tabs.Length - 1);
        }

        private void ShowHistoryContextMenu()
        {
            var data = QuickNavigationData.Instance;
            var menu = new GenericMenu();

            if (data.HistoryPaths.Count > 0)
            {
                menu.AddItem(new GUIContent("Clear History"), false, () => {
                    if (EditorUtility.DisplayDialog("Clear History", "Are you sure you want to clear your navigation history?", "Yes", "Cancel"))
                    {
                        data.ClearHistory();
                        _selectedHistoryGuid = null;
                        RefreshWindow();
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Clear History"));
            }

            menu.ShowAsContext();
        }

        private void DrawHistoryBottomBar(QuickNavigationData data)
        {
            int itemCount = data.HistoryPaths.Count;
            int invalidCount = 0;

            foreach (string stored in data.HistoryPaths)
            {
                if (!QuickNavigationAssets.IsValid(stored))
                    invalidCount++;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Height(BottomBarHeight));
            GUILayout.Space(4f);

            DrawBottomBarStatus(itemCount, invalidCount, 0);

            GUILayout.FlexibleSpace();
            GUILayout.Space(4f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void DrawFavoritesBottomBar(QuickNavigationData data)
        {
            if (data.FavoriteTabs.Count == 0 || _selectedFavoriteTab < 0 || _selectedFavoriteTab >= data.FavoriteTabs.Count || data.FavoriteTabs[_selectedFavoriteTab].IsArchived)
                return;

            var entries = data.FavoriteTabs[_selectedFavoriteTab].GetEntries();
            int itemCount = entries.Count;
            int invalidCount = 0;

            foreach (var entry in entries)
            {
                if (!QuickNavigationAssets.IsValid(entry))
                    invalidCount++;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Height(BottomBarHeight));
            GUILayout.Space(4f);

            DrawBottomBarStatus(itemCount, invalidCount, _selectedFavoriteIndices.Count);

            GUILayout.FlexibleSpace();

            if (invalidCount > 0)
            {
                if (DrawBottomBarActionButton("Clear Invalid", BottomBarDestructiveColor))
                {
                    data.RecordUndo("Clear Invalid Favorites");
                    entries.RemoveAll(entry => !QuickNavigationAssets.IsValid(entry));
                    data.Save();
                    RefreshAndRebuild();
                }
            }

            if (_selectedFavoriteIndices.Count > 1)
            {
                if (DrawBottomBarActionButton("Remove Selected", BottomBarDestructiveColor))
                    RemoveSelectedFavorites();
            }
            GUILayout.Space(4f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void AddExternalFolder(QuickNavigationData data, int tabIndex)
        {
            if (data.FavoriteTabs.Count == 0 || tabIndex < 0 || tabIndex >= data.FavoriteTabs.Count || data.FavoriteTabs[tabIndex].IsArchived)
                return;

            string pickedPath = EditorUtility.OpenFolderPanel("Add External Folder", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(pickedPath))
                return;

            data.AddToFavorites(pickedPath, tabIndex);
            RefreshAndRebuild();
        }

        private void DrawBottomBarSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            rect.x += 4f;
            rect.width -= 8f;

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.35f));
        }

        private void DrawBottomBarStatus(int itemCount, int invalidCount, int selectedCount)
        {
            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };

            var detailStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            var labelHeight = GUILayout.Height(BottomBarHeight);
            bool allInvalid = itemCount > 0 && invalidCount == itemCount;

            if (allInvalid)
            {
                Color prev = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.45f, 0.45f);
                string invalidLabel = invalidCount == 1 ? "1 invalid" : $"{invalidCount} invalid";
                GUILayout.Label(invalidLabel, statusStyle, GUILayout.ExpandWidth(false), labelHeight);
                GUI.contentColor = prev;
            }
            else
            {
                string countLabel = itemCount == 1 ? "1 item" : $"{itemCount} items";
                GUILayout.Label(countLabel, statusStyle, GUILayout.ExpandWidth(false), labelHeight);

                if (invalidCount > 0)
                {
                    Color prev = GUI.contentColor;
                    GUI.contentColor = new Color(1f, 0.45f, 0.45f);
                    string invalidLabel = invalidCount == 1 ? "1 invalid" : $"{invalidCount} invalid";
                    GUILayout.Label($"  ·  {invalidLabel}", detailStyle, GUILayout.ExpandWidth(false), labelHeight);
                    GUI.contentColor = prev;
                }
            }

            if (selectedCount > 1)
            {
                string selectedLabel = $"{selectedCount} selected";
                GUILayout.Label($"  ·  {selectedLabel}", detailStyle, GUILayout.ExpandWidth(false), labelHeight);
            }
        }

        private bool DrawBottomBarActionButton(string label, Color backgroundColor)
        {
            GUI.backgroundColor = backgroundColor;
            bool clicked = GUILayout.Button(label, GUILayout.Height(BottomBarHeight));
            GUI.backgroundColor = Color.white;
            return clicked;
        }

        private void HandleGlobalDragAndDrop()
        {
            if (GUIUtility.hotControl != 0) return;
            Event currentEvent = Event.current;

            if (DragAndDrop.GetGenericData("QuickNav_InternalDrag") != null) return;
            if (DragAndDrop.GetGenericData("QuickNav_TabDrag") != null) return;
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0) return;
            if (!HasAcceptableDraggedPaths(DragAndDrop.paths)) return;

            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    string[] droppedPaths = DragAndDrop.paths;
                    int targetTab = _selectedFavoriteTab;

                    EditorApplication.delayCall += () => {
                        QuickNavigationData.Instance.RecordUndo("Add Favorites");
                        foreach (string droppedPath in droppedPaths)
                        {
                            if (!string.IsNullOrEmpty(droppedPath))
                                QuickNavigationData.Instance.AddToFavorites(droppedPath, targetTab);
                        }
                        RefreshAndRebuild();
                    };

                    currentEvent.Use();
                }
            }
        }

        private static bool HasAcceptableDraggedPaths(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return false;

            foreach (string draggedPath in paths)
            {
                if (QuickNavigationAssets.CanAcceptDraggedPath(draggedPath))
                    return true;
            }

            return false;
        }
    }
}
