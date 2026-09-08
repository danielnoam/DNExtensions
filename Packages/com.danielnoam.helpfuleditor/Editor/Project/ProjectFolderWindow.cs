using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// A tab that is one folder and nothing else — Unity's own object view with no folder tree beside
    /// it, no search bar and no selection to drift with.
    ///
    /// It replaces opening a folder in a second Project window, which only ever approximated this: a
    /// Project window has no identity of its own, so it had to be locked to stop it following the
    /// selection and renamed to stop it reading "Project". This window is the folder, so neither is
    /// needed — it names itself, and there is nothing for it to drift onto.
    /// </summary>
    internal sealed class ProjectFolderWindow : EditorWindow
    {
        private const float HeaderHeight = 20f;
        private const float FooterHeight = 18f;
        private const float ZoomSliderWidth = 64f;
        private const float NewFolderButtonWidth = 34f;
        private const int ListModeGridSize = 16;
        private const int MaxGridSize = 96;
        private const int DefaultGridSize = 64;
        private const int MaxCreateAttempts = 3;

        /// <summary>
        /// Shared by every folder tab and kept across sessions, so the zoom is a preference rather
        /// than something each tab discovers for itself. Per-window it would reset on every folder
        /// opened, which is the opposite of useful.
        /// </summary>
        private const string GridSizeKey = "DNExtensions.HelpfulEditor.FolderTab.GridSize";

        /// <summary>Keeps the grid's control id its own, rather than whatever position it lands in.</summary>
        private static readonly int ListHint = "DNExtensions.HelpfulEditor.FolderTab.List".GetHashCode();

        [SerializeField] private string _folderPath;

        private HelpfulEditorObjectListArea _listArea;
        private GUIStyle _breadcrumbStyle;
        private GUIContent _newFolderContent;
        private bool _internalSelectionChange;
        private int _failedAttempts;

        private static int StoredGridSize
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(GridSizeKey, DefaultGridSize), ListModeGridSize, MaxGridSize);
            set => EditorPrefs.SetInt(GridSizeKey, value);
        }

        public string FolderPath => _folderPath;

        /// <summary>
        /// Whether a folder tab can be opened at all. False on a Unity version whose object view this
        /// cannot host, which is the caller's cue to fall back to a Project window.
        /// </summary>
        public static bool Supported => HelpfulEditorObjectListArea.Available;

        public static ProjectFolderWindow Create(string folderPath)
        {
            ProjectFolderWindow window = CreateInstance<ProjectFolderWindow>();
            window.SetFolder(folderPath);

            return window;
        }

        public void SetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;

            _folderPath = folderPath;

            ApplyTitle();
            Repaint();
        }

        private void OnEnable()
        {
            // The view reads the folder once and caches it, so anything added, removed or renamed
            // has to be announced — otherwise a tab shows whatever was there when it was opened.
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;

            ApplyTitle();
        }

        /// <summary>
        /// Built on the first OnGUI rather than in OnEnable, and rebuilt the same way after every
        /// domain reload — the view holds native state that does not survive one, and the folder
        /// path, which does, is all it takes to put it back.
        ///
        /// It has to be OnGUI: constructing the view reads EditorStyles, which has no current skin
        /// outside a GUI context. Doing it in OnEnable worked when a tab was opened — that happens
        /// inside the click that asked for it — and threw on every reload afterwards, which is what
        /// left a restored tab claiming the Unity version was at fault.
        /// </summary>
        private HelpfulEditorObjectListArea EnsureListArea()
        {
            if (_listArea != null || _failedAttempts >= MaxCreateAttempts) return _listArea;

            _listArea = HelpfulEditorObjectListArea.Create(this, Repaint, OnItemSelected, OnListAreaKeyboard);

            if (_listArea == null)
            {
                // Bounded rather than retried forever: a version that genuinely cannot host the view
                // would otherwise construct and throw on every repaint for as long as the tab is open.
                _failedAttempts++;
                return null;
            }

            _listArea.GridSize = StoredGridSize;
            _listArea.SetSelection(Selection.objects);

            return _listArea;
        }

        /// <summary>
        /// Also the moment to notice the folder itself has gone. Falling back to the nearest ancestor
        /// that still exists keeps the tab usable, which is better than leaving it on a dead path
        /// until it is closed and reopened.
        /// </summary>
        private void OnProjectChanged()
        {
            if (!string.IsNullOrEmpty(_folderPath) && !AssetDatabase.IsValidFolder(_folderPath))
            {
                string surviving = NearestExistingAncestor(_folderPath);

                if (surviving != null)
                {
                    SetFolder(surviving);
                    return;
                }
            }

            _listArea?.Invalidate();
            ApplyTitle();
            Repaint();
        }

        private static string NearestExistingAncestor(string folderPath)
        {
            for (int slash = folderPath.LastIndexOf('/'); slash > 0; slash = folderPath.LastIndexOf('/', slash - 1))
            {
                string candidate = folderPath.Substring(0, slash);
                if (AssetDatabase.IsValidFolder(candidate)) return candidate;
            }

            return null;
        }

        /// <summary>
        /// A click selects, so the Inspector follows the window the way it would in the Project
        /// window. A double click opens — and on a folder that means showing it here rather than
        /// bouncing the user into a different window, which is the whole point of the tab.
        /// </summary>
        private void OnItemSelected(bool doubleClicked)
        {
            Object[] selection = _listArea.GetSelection();
            if (selection.Length == 0) return;

            // Flagged so the selection change this causes is not mirrored straight back into the view
            // — it is already showing it, and re-initialising its selection mid-click would drop the
            // drag the user may be starting.
            _internalSelectionChange = true;
            Selection.objects = selection;
            _internalSelectionChange = false;

            if (doubleClicked) OpenSelection();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;

            if (_listArea != null) StoredGridSize = _listArea.GridSize;
        }

        /// <summary>Releases the preview manager the view owns, the way the Project window does.</summary>
        private void OnDestroy()
        {
            _listArea?.Destroy();
        }

        /// <summary>
        /// What loads the icon previews. Without it a folder of prefabs or materials shows generic
        /// icons that never resolve, because nothing is ticking the view's preview queue.
        /// </summary>
        private void OnInspectorUpdate()
        {
            _listArea?.OnInspectorUpdate();
        }

        /// <summary>
        /// Keeps the highlight in step with a selection made elsewhere — clicking the same asset in a
        /// Project window, or the Inspector's lock following something new.
        /// </summary>
        private void OnSelectionChange()
        {
            if (_listArea == null || _internalSelectionChange) return;

            _listArea.SetSelection(Selection.objects);
            Repaint();
        }

        /// <summary>
        /// The keys the Project window answers in its own list, which this had no handler for at all:
        /// F2 (Return on Windows) renames, Return opens on Mac, and Backspace — Cmd+Up on Mac — goes
        /// up a folder, which is the keyboard version of the breadcrumb.
        ///
        /// Rename lives here because the Assets/Rename menu item cannot reach this window: it asks
        /// ProjectBrowser for whichever browser was last interacted with, and a folder tab is not
        /// one. The view itself has always been able to rename — nothing was ever asking it to.
        /// </summary>
        private void OnListAreaKeyboard()
        {
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown || _listArea == null) return;

            // The rename field owns the keyboard while it is open, and Return there means "commit
            // this name", not "start renaming again".
            if (_listArea.IsRenaming()) return;

            switch (evt.keyCode)
            {
                case KeyCode.F2:
                    if (_listArea.BeginRename()) evt.Use();
                    return;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
#if UNITY_EDITOR_OSX
                    evt.Use();
                    OpenSelection();
#else
                    if (_listArea.BeginRename()) evt.Use();
#endif
                    return;

                case KeyCode.Backspace:
                case KeyCode.UpArrow when evt.command:
                    GoToParentFolder();
                    evt.Use();
                    return;

                case KeyCode.DownArrow when evt.command:
                    evt.Use();
                    OpenSelection();
                    return;
            }
        }

        /// <summary>Opens what is selected, showing a folder here rather than handing it to Unity.</summary>
        private void OpenSelection()
        {
            Object[] selection = _listArea.GetSelection();
            if (selection.Length == 0) return;

            string path = AssetDatabase.GetAssetPath(selection[0]);

            if (AssetDatabase.IsValidFolder(path)) SetFolder(path);
            else AssetDatabase.OpenAsset(selection);
        }

        private void GoToParentFolder()
        {
            int slash = _folderPath.LastIndexOf('/');
            if (slash <= 0) return;

            SetFolder(_folderPath.Substring(0, slash));
        }

        /// <summary>
        /// Named and iconed after its folder. A package folder's reverse-DNS name says nothing, so the
        /// same display-name rule the window titles use applies here too.
        /// </summary>
        private void ApplyTitle()
        {
            if (string.IsNullOrEmpty(_folderPath)) return;

            Texture icon = AssetDatabase.GetCachedIcon(_folderPath);

            titleContent = new GUIContent(
                FolderName(_folderPath),
                icon ? icon : EditorGUIUtility.FindTexture("Folder Icon"),
                _folderPath);
        }

        private void OnGUI()
        {
            // Taken before anything else draws, the way the Project window takes its own. The
            // breadcrumb's buttons come and go with the folder depth, so an id allocated after them
            // would change on every navigation — and the grid would lose keyboard focus, and with it
            // F2 and the arrow keys, each time the tab moved.
            int listControlId = GUIUtility.GetControlID(ListHint, FocusType.Keyboard);

            if (string.IsNullOrEmpty(_folderPath))
            {
                EditorGUILayout.HelpBox("This tab has no folder.", MessageType.Info);
                return;
            }

            if (EnsureListArea() == null)
            {
                EditorGUILayout.HelpBox(
                    "The folder view could not be hosted on this Unity version. Open the folder in a Project window instead.",
                    MessageType.Warning);
                return;
            }

            // The folder can be deleted or moved while its tab is open, and an empty grid would be a
            // confusing way to say so.
            if (!AssetDatabase.IsValidFolder(_folderPath))
            {
                EditorGUILayout.HelpBox($"{_folderPath} no longer exists.", MessageType.Warning);
                return;
            }

            // First look at the event, as in the Project window's own OnGUI: this is what hands the
            // rename field the Return that commits a name and the Escape that abandons it.
            _listArea.OnEvent();

            HandleCommands();

            Rect header = new Rect(0f, 0f, position.width, HeaderHeight);
            Rect grid = new Rect(0f, HeaderHeight, position.width, position.height - HeaderHeight - FooterHeight);
            Rect footer = new Rect(0f, position.height - FooterHeight, position.width, FooterHeight);

            DrawHeader(header);

            // Re-asserted after the folder is applied: the grid keeps its size across an Init today,
            // but the zoom surviving a folder change is the point of storing it, not a side effect of
            // where the view happens to hold it.
            _listArea.SetFolder(_folderPath, grid);
            _listArea.GridSize = StoredGridSize;

            _listArea.OnGUI(grid, listControlId);

            HandleContextClick(grid);

            DrawFooter(footer);
        }

        /// <summary>
        /// Unity's own Assets menu, opened the same way and in the same place the Project window opens
        /// it — so Create, Reimport, Show in <see cref="HelpfulEditorPlatform.FileManagerName"/>, Find
        /// References and the rest all read the selection this tab just set.
        ///
        /// Rename is the one entry that cannot land here: it asks ProjectBrowser for the browser the
        /// user last interacted with, and a folder tab is not one. F2 and a slow second click are what
        /// rename in this window.
        /// </summary>
        private void HandleContextClick(Rect gridRect)
        {
            Event evt = Event.current;
            if (evt.type != EventType.ContextClick || !gridRect.Contains(evt.mousePosition)) return;

            GUIUtility.hotControl = 0;

            // Right-clicking empty space selects the folder itself, so Create makes its asset here
            // rather than wherever the previous selection happened to live.
            if (_listArea.GetSelection().Length == 0)
            {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(_folderPath);
                if (folder) Selection.activeObject = folder;
            }

            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0f, 0f), "Assets/", null);
            evt.Use();
        }

        /// <summary>
        /// The keyboard shortcuts arrive as command events rather than key presses, and a window that
        /// does not answer them gets none of them — which is why Delete did nothing here.
        ///
        /// Validate has to claim the command for Execute to follow, so both phases are answered. The
        /// work itself is Unity's own, so the dialogs, the trash-versus-permanent split and the dimmed
        /// look of a cut asset all behave as they do in the Project window.
        /// </summary>
        private void HandleCommands()
        {
            Event evt = Event.current;
            if (evt.type != EventType.ValidateCommand && evt.type != EventType.ExecuteCommand) return;

            // A rename field is reading these keys as text. Claiming Delete while a name is being
            // edited would delete the asset the user is in the middle of naming.
            if (_listArea.IsRenaming()) return;

            bool execute = evt.type == EventType.ExecuteCommand;
            Object[] selection = _listArea.GetSelection();

            switch (evt.commandName)
            {
                case "Delete":
                case "SoftDelete":
                    if (selection.Length == 0) return;
                    evt.Use();

                    if (!execute) return;

                    // SoftDelete is the plain Delete key and is the one that asks first, which reads
                    // backwards until you notice Shift+Delete arrives as "Delete".
                    if (HelpfulEditorAssetCommands.Delete(selection, evt.commandName == "SoftDelete"))
                    {
                        Selection.objects = Array.Empty<Object>();
                    }

                    GUIUtility.ExitGUI();
                    return;

                case "Duplicate":
                    if (selection.Length == 0) return;
                    evt.Use();

                    if (!execute) return;

                    HelpfulEditorAssetCommands.Duplicate();
                    GUIUtility.ExitGUI();
                    return;

                case "Cut":
                case "Copy":
                    if (selection.Length == 0) return;
                    evt.Use();

                    if (!execute) return;

                    HelpfulEditorAssetCommands.CutOrCopy(evt.commandName == "Cut");
                    Repaint();
                    return;

                case "Paste":
                    evt.Use();

                    if (!execute) return;

                    // Paste lands where the selection is, so with nothing selected in this tab the
                    // folder itself stands in for it — otherwise it would fall back to asking which
                    // folder a Project window is showing, which is not this one.
                    if (selection.Length == 0) Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_folderPath);

                    HelpfulEditorAssetCommands.Paste();
                    GUIUtility.ExitGUI();
                    return;

                case "SelectAll":
                    evt.Use();

                    if (execute) SelectAll();
                    return;
            }
        }

        private void SelectAll()
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { _folderPath });
            Object[] assets = new Object[guids.Length];
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // FindAssets searches the whole subtree, but the view lists one folder — selecting
                // things it is not showing would be a selection the user cannot see.
                if (path.LastIndexOf('/') != _folderPath.Length) continue;

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset) assets[count++] = asset;
            }

            Array.Resize(ref assets, count);

            Selection.objects = assets;
            _listArea.SetSelection(assets);

            Repaint();
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.DrawRect(rect, HelpfulEditorGUI.WindowBackground);

            ProjectSettings settings = HelpfulEditorSettings.Project;
            bool showNewFolder = settings.moduleEnabled && settings.createFolderButtonEnabled;

            float breadcrumbWidth = rect.width - (showNewFolder ? NewFolderButtonWidth + 6f : 0f);

            DrawBreadcrumb(new Rect(rect.x, rect.y, breadcrumbWidth, rect.height));

            if (showNewFolder) DrawNewFolderButton(new Rect(rect.xMax - NewFolderButtonWidth - 4f, rect.y + 2f, NewFolderButtonWidth, rect.height - 4f));
        }

        /// <summary>
        /// The same + button the Project window's breadcrumb carries, creating in this tab's folder.
        /// Drawn here rather than injected as a visual element, because this window's header is IMGUI
        /// and it knows its own folder — there is nothing to infer.
        /// </summary>
        private void DrawNewFolderButton(Rect rect)
        {
            // Icon and glyph in one content, the way the Project window's button reads.
            _newFolderContent ??= new GUIContent(
                "+",
                HelpfulEditorGUI.LoadIcon("Folder Icon"),
                "New folder in this folder");

            // No right-click create menu, unlike the Project window's button: Unity's Assets/Create
            // items land in whatever folder a Project browser is showing, which is not this one.
            if (GUI.Button(rect, _newFolderContent, EditorStyles.miniButton))
            {
                ProjectCreateFolderButton.CreateFolderIn(_folderPath);
            }
        }

        /// <summary>
        /// The path as clickable segments. This is the only way back up — the view lists one folder,
        /// so double-clicking a subfolder is the way down and this is the way out of it.
        /// </summary>
        private void DrawBreadcrumb(Rect rect)
        {
            _breadcrumbStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };

            string[] segments = _folderPath.Split('/');
            float x = 4f;

            for (int i = 0; i < segments.Length; i++)
            {
                GUIContent content = new GUIContent(i == 0 ? segments[i] : $"› {segments[i]}");
                float width = _breadcrumbStyle.CalcSize(content).x;

                Rect segmentRect = new Rect(x, rect.y, width, rect.height);
                bool last = i == segments.Length - 1;

                using (new EditorGUI.DisabledScope(last))
                {
                    if (GUI.Button(segmentRect, content, _breadcrumbStyle) && !last)
                    {
                        SetFolder(string.Join("/", segments, 0, i + 1));
                        GUIUtility.ExitGUI();
                    }
                }

                x += width;
                if (x > rect.width) break;
            }
        }

        /// <summary>
        /// The zoom slider, which is also the icon/list switch — the view goes to list mode at its
        /// minimum, the same as the Project window's own.
        /// </summary>
        private void DrawFooter(Rect rect)
        {
            EditorGUI.DrawRect(rect, HelpfulEditorGUI.WindowBackground);

            Rect sliderRect = new Rect(rect.xMax - ZoomSliderWidth - 4f, rect.y + 2f, ZoomSliderWidth, rect.height - 4f);

            int current = _listArea.GridSize;
            int size = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, current, ListModeGridSize, MaxGridSize));

            if (size == current) return;

            _listArea.GridSize = size;
            StoredGridSize = size;
        }

        private static string FolderName(string folderPath)
        {
            int slash = folderPath.LastIndexOf('/');
            string name = slash >= 0 ? folderPath.Substring(slash + 1) : folderPath;

            if (!name.StartsWith("com.", StringComparison.Ordinal)) return name;

            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(folderPath);
            return package != null ? package.displayName : name;
        }
    }
}
