using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Hosts Unity's own object view — the icon and list grid that the right-hand pane of the Project
    /// window is made of — inside any EditorWindow.
    ///
    /// Worth the reflection rather than drawing a grid by hand, because of what comes with it:
    /// previews, renaming in place, drag and drop both ways, the asset context menu, multi-select and
    /// the icon/list switch. None of that is reachable any other way, and a hand-drawn grid would be a
    /// worse version of all of it.
    ///
    /// ObjectListArea is internal, but the shape of what is needed here has not moved between 2022.3
    /// and 6000.5 — only the id type changed, and HelpfulEditorObjectId already bridges that. Every
    /// lookup is resolved once and the whole thing reports itself unavailable rather than throwing, so
    /// a version that moves it leaves the caller a fallback rather than a broken window.
    /// </summary>
    internal sealed class HelpfulEditorObjectListArea
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Assembly EditorAssembly = typeof(EditorWindow).Assembly;

        private static Type _listAreaType;
        private static Type _listAreaStateType;
        private static Type _searchFilterType;
        private static Type _hierarchyTypeType;
        private static Type _searchAreaType;

        private static ConstructorInfo _constructor;
        private static MethodInfo _init;
        private static MethodInfo _onGui;
        private static MethodInfo _initSelection;
        private static MethodInfo _getSelection;
        private static MethodInfo _setFolders;
        private static MethodInfo _setSearchArea;
        private static PropertyInfo _gridSize;

        private static bool _resolved;
        private static bool _available;
        private static bool _warned;

        private readonly object _listArea;

        private string _folder;
        private Rect _lastRect;

        private HelpfulEditorObjectListArea(object listArea)
        {
            _listArea = listArea;
        }

        public static bool Available
        {
            get
            {
                Resolve();
                return _available;
            }
        }

        /// <summary>
        /// Resolved once per domain. Every member here is looked up by name on an internal type, so
        /// the whole feature stands or falls together — a partial resolve is treated as unavailable
        /// rather than left to fail at the first call.
        /// </summary>
        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                _listAreaType = EditorAssembly.GetType("UnityEditor.ObjectListArea");
                _listAreaStateType = EditorAssembly.GetType("UnityEditor.ObjectListAreaState");
                _searchFilterType = EditorAssembly.GetType("UnityEditor.SearchFilter");
                _hierarchyTypeType = EditorAssembly.GetType("UnityEditor.HierarchyType");
                _searchAreaType = _searchFilterType?.GetNestedType("SearchArea", BindingFlags.Public | BindingFlags.NonPublic);

                if (_listAreaType == null || _listAreaStateType == null || _searchFilterType == null ||
                    _hierarchyTypeType == null || _searchAreaType == null)
                {
                    return;
                }

                _constructor = _listAreaType.GetConstructor(AnyInstance, null,
                    new[] { _listAreaStateType, typeof(EditorWindow), typeof(bool) }, null);

                // The four-argument overload, not the one that also takes SearchSessionOptions: it is
                // present on every version here, and the extra argument has no bearing on browsing a
                // folder rather than running a search.
                _init = _listAreaType.GetMethod("Init", AnyInstance, null,
                    new[] { typeof(Rect), _hierarchyTypeType, _searchFilterType, typeof(bool) }, null);

                _onGui = _listAreaType.GetMethod("OnGUI", AnyInstance, null, new[] { typeof(Rect), typeof(int) }, null);
                _initSelection = _listAreaType.GetMethod("InitSelection", AnyInstance);
                _getSelection = _listAreaType.GetMethod("GetSelection", AnyInstance);
                _gridSize = _listAreaType.GetProperty("gridSize", AnyInstance);

                _setFolders = _searchFilterType.GetProperty("folders", AnyInstance)?.GetSetMethod(true);
                _setSearchArea = _searchFilterType.GetProperty("searchArea", AnyInstance)?.GetSetMethod(true);

                _available = _constructor != null && _init != null && _onGui != null &&
                             _setFolders != null && _setSearchArea != null;
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// Null when the object view cannot be hosted on this version, which is the caller's cue to
        /// fall back. The itemSelected callback is handed the view's own "was this a double click"
        /// flag, which is the only place that distinction is available.
        /// </summary>
        public static HelpfulEditorObjectListArea Create(EditorWindow owner, Action repaint, Action<bool> itemSelected)
        {
            if (!owner || !Available) return null;

            try
            {
                object state = Activator.CreateInstance(_listAreaStateType, true);

                // showNoneItem off: the "None" entry belongs to an object picker, not to a folder.
                object listArea = _constructor.Invoke(new[] { state, owner, false });

                SetFlag(listArea, "allowDragging", true);
                SetFlag(listArea, "allowRenaming", true);
                SetFlag(listArea, "allowMultiSelect", true);
                SetFlag(listArea, "allowDeselection", true);
                SetFlag(listArea, "foldersFirst", true);

                // What lets EditorApplication.projectWindowItemOnGUI reach these rows, and with it
                // every row overlay the suite's Project module draws.
                SetFlag(listArea, "allowUserRenderingHook", true);

                SetCallback(listArea, "repaintCallback", repaint);
                SetCallback(listArea, "itemSelectedCallback", itemSelected);

                return new HelpfulEditorObjectListArea(listArea);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        /// <summary>
        /// Forces the next SetFolder to rebuild rather than recognise the folder it is already on.
        /// The view reads the asset database once, when it is initialised, so anything that changes
        /// what is in the folder has to say so — this is what an asset being added, removed or
        /// renamed goes through.
        /// </summary>
        public void Invalidate()
        {
            _folder = null;
        }

        /// <summary>
        /// Points the view at a folder. Re-initialised on a resize as well as a folder change, because
        /// the grid lays its columns out against the rect it was given rather than the one it is drawn
        /// into.
        /// </summary>
        public void SetFolder(string folderPath, Rect rect)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            if (_folder == folderPath && Mathf.Approximately(_lastRect.width, rect.width) &&
                Mathf.Approximately(_lastRect.height, rect.height))
            {
                return;
            }

            _folder = folderPath;
            _lastRect = rect;

            try
            {
                object filter = Activator.CreateInstance(_searchFilterType, true);

                _setFolders.Invoke(filter, new object[] { new[] { folderPath } });
                _setSearchArea.Invoke(filter, new[] { Enum.Parse(_searchAreaType, "SelectedFolders") });

                // HierarchyType.Assets. Read by name rather than by its value, which is the one thing
                // here that would be silently wrong if it ever moved.
                object assets = Enum.Parse(_hierarchyTypeType, "Assets");

                _init.Invoke(_listArea, new[] { rect, assets, filter, false });

                // Init leaves the view with no notion of what is selected, so the highlight is put
                // back from the editor's selection — the same thing ProjectBrowser does on the line
                // after its own Init call.
                SetSelection(Selection.objects);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        public void OnGUI(Rect rect, int keyboardControlId)
        {
            try
            {
                _onGui.Invoke(_listArea, new object[] { rect, keyboardControlId });
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        public int GridSize
        {
            get
            {
                try
                {
                    return _gridSize?.GetValue(_listArea) is int size ? size : 64;
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                    return 64;
                }
            }
            set
            {
                try
                {
                    _gridSize?.SetValue(_listArea, value);
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                }
            }
        }

        /// <summary>The objects the view currently has selected, skipping ids that no longer resolve.</summary>
        public Object[] GetSelection()
        {
            try
            {
                if (_getSelection?.Invoke(_listArea, null) is not Array ids) return Array.Empty<Object>();

                Object[] result = new Object[ids.Length];
                int count = 0;

                for (int i = 0; i < ids.Length; i++)
                {
                    Object resolved = HelpfulEditorObjectId.Resolve(ids.GetValue(i));
                    if (resolved) result[count++] = resolved;
                }

                Array.Resize(ref result, count);
                return result;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return Array.Empty<Object>();
            }
        }

        /// <summary>Mirrors an outside selection into the view, so it highlights what the editor has selected.</summary>
        public void SetSelection(Object[] objects)
        {
            if (_initSelection == null || objects == null) return;

            try
            {
                Type idType = _initSelection.GetParameters()[0].ParameterType.GetElementType();
                if (idType == null) return;

                Array ids = Array.CreateInstance(idType, objects.Length);

                for (int i = 0; i < objects.Length; i++)
                {
                    object id = HelpfulEditorObjectId.ConvertTo(HelpfulEditorObjectId.Raw(objects[i]), idType);
                    if (id != null) ids.SetValue(id, i);
                }

                _initSelection.Invoke(_listArea, new object[] { ids });
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private static void SetFlag(object listArea, string propertyName, bool value)
        {
            _listAreaType.GetProperty(propertyName, AnyInstance)?.SetValue(listArea, value);
        }

        private static void SetCallback(object listArea, string propertyName, Delegate callback)
        {
            PropertyInfo property = _listAreaType.GetProperty(propertyName, AnyInstance);
            if (property == null || callback == null) return;

            // Only assigned when the delegate types line up — a signature change would otherwise
            // throw on every window that opened.
            if (property.PropertyType.IsInstanceOfType(callback)) property.SetValue(listArea, callback);
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] The folder view is unavailable on this Unity version, falling back to a Project window. ({e.Message})");
        }
    }
}
