using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Expand/collapse control for the Hierarchy and Project trees. Unity exposes no public API for
    /// this, so every call reflects into the internal TreeViewController. Same risk tier as the Tabs
    /// module: every entry point is guarded and degrades to a no-op with a single warning rather
    /// than throwing.
    /// </summary>
    internal static class HelpfulEditorTreeReflection
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool _warned;
        private static bool _foldAnimationWarned;
        private static bool _foldAnimationResolved;
        private static bool _foldAnimationAvailable;

        /// <summary>
        /// Works on any hierarchy row id, not just GameObjects — scene headers are rows in the same
        /// tree and expand exactly the same way.
        /// </summary>
        private static bool IsHierarchyExpanded(object rawId)
        {
            object data = DataOf(GetHierarchyTreeView());
            if (data == null || rawId == null) return false;

            return InvokeWithId(data, "IsExpanded", rawId) is bool expanded && expanded;
        }

        private static void SetHierarchyExpanded(object rawId, bool expanded, bool includeChildren)
        {
            if (rawId == null) return;

            if (SetExpanded(GetHierarchyTreeView(), rawId, expanded, includeChildren)) EditorApplication.RepaintHierarchyWindow();
        }

        public static void ToggleHierarchyExpanded(object rawId, bool includeChildren)
        {
            SetHierarchyExpanded(rawId, !IsHierarchyExpanded(rawId), includeChildren);
        }

        public static object[] GetHierarchyExpandedIds()
        {
            return GetExpandedIds(DataOf(GetHierarchyTreeView()));
        }

        public static void SetHierarchyExpandedIds(IEnumerable<object> ids)
        {
            SetExpandedIds(DataOf(GetHierarchyTreeView()), ids);
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// <paramref name="listArea"/> scopes the change to the pane the row was in. The two panes
        /// hold independent expansion state, so acting on both would expand a folder in the left
        /// tree just because it was toggled in the right one.
        /// </summary>
        private static void SetProjectExpanded(string assetPath, bool expanded, bool includeChildren, bool listArea)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (!asset) return;

            object rawId = HelpfulEditorObjectId.Raw(asset);
            bool changed;

            if (listArea)
            {
                // The two-column right pane is an ObjectListArea, not a TreeView, which is why
                // sub-assets such as an FBX's contents need this separate path.
                changed = TrySetListAreaExpanded(rawId, expanded);
            }
            else
            {
                changed = false;
                foreach (object treeView in GetProjectTreeViews())
                {
                    changed |= SetExpanded(treeView, rawId, expanded, includeChildren);
                }
            }

            if (changed) EditorApplication.RepaintProjectWindow();
        }

        private static bool IsProjectExpanded(string assetPath, bool listArea)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (!asset) return false;

            object rawId = HelpfulEditorObjectId.Raw(asset);

            if (listArea)
            {
                foreach (object host in GetListAreaHosts())
                {
                    if (InvokeWithId(host, "IsExpanded", rawId) is bool listExpanded && listExpanded) return true;
                }

                return false;
            }

            foreach (object treeView in GetProjectTreeViews())
            {
                if (InvokeWithId(DataOf(treeView), "IsExpanded", rawId) is bool expanded && expanded) return true;
            }

            return false;
        }

        /// <summary>
        /// Rect of the two-column right pane, used to tell which pane a row belongs to. Reports
        /// nothing in the one-column layout: there is no second pane there, and the stale rect would
        /// make every row look like a list-area row and send expansion to the wrong owner.
        /// </summary>
        public static bool TryGetProjectListAreaRect(out Rect rect)
        {
            rect = default;

            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                EditorWindow window = FindWindow(browserType);
                if (!window || !IsTwoColumnLayout(browserType, window)) return false;

                object value = browserType.GetField("m_ListAreaRect", AnyInstance)?.GetValue(window);
                if (value is Rect listAreaRect)
                {
                    rect = listAreaRect;
                    return true;
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return false;
        }

        /// <summary>
        /// Whether this particular window is in the two-column layout. The window has to be named:
        /// two Project windows can be in different layouts, and answering for whichever one happened
        /// to be found first gets the other one wrong.
        /// </summary>
        public static bool IsTwoColumnLayout(EditorWindow window)
        {
            if (!window) return false;

            try
            {
                return IsTwoColumnLayout(window.GetType(), window);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }


        /// <summary>
        /// Decided by which tree the window built rather than by reading m_ViewMode: the enum member
        /// names are not in the shipped metadata, so their numeric values cannot be relied on. The
        /// folder tree only exists in the two-column layout, and the asset tree only in one column,
        /// which is a structural fact rather than an assumed ordering.
        /// </summary>
        private static bool IsTwoColumnLayout(Type browserType, EditorWindow window)
        {
            return browserType.GetField("m_FolderTree", AnyInstance)?.GetValue(window) != null;
        }

        /// <summary>
        /// The ObjectListArea and its local group, which own expansion of sub-assets in the
        /// two-column right pane. Member names here are less settled than the TreeView ones, so the
        /// expansion call is matched loosely and every failure degrades to a no-op.
        /// </summary>
        private static IEnumerable<object> GetListAreaHosts()
        {
            List<object> hosts = new List<object>();

            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                EditorWindow window = FindWindow(browserType);
                if (!window) return hosts;

                object listArea = browserType.GetField("m_ListArea", AnyInstance)?.GetValue(window);
                if (listArea == null) return hosts;

                hosts.Add(listArea);

                object localAssets = GetMemberValue(listArea, "m_LocalAssets");
                if (localAssets != null) hosts.Add(localAssets);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return hosts;
        }

        private static bool TrySetListAreaExpanded(object rawId, bool expanded)
        {
            foreach (object host in GetListAreaHosts())
            {
                try
                {
                    foreach (MethodInfo method in host.GetType().GetMethods(AnyInstance))
                    {
                        if (method.Name.IndexOf("Expand", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        if (method.Name.StartsWith("Is", StringComparison.Ordinal)) continue;

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length != 2 || parameters[1].ParameterType != typeof(bool)) continue;

                        object id = ConvertId(rawId, parameters[0].ParameterType);
                        if (id == null) continue;

                        method.Invoke(host, new[] { id, (object)expanded });
                        return true;
                    }
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                }
            }

            return false;
        }

        public static void ToggleProjectExpanded(string assetPath, bool includeChildren, bool listArea)
        {
            SetProjectExpanded(assetPath, !IsProjectExpanded(assetPath, listArea), includeChildren, listArea);
        }

        /// <summary>
        /// Collapses the asset folders but leaves rows that are not assets alone — the Project tree
        /// has structural rows of its own, and clearing the expanded set wholesale would fold those
        /// away too, the same way it used to swallow the Hierarchy's scene headers.
        /// </summary>
        public static void CollapseAllProjectFolders()
        {
            foreach (object treeView in GetProjectTreeViews())
            {
                object data = DataOf(treeView);
                if (data == null) continue;

                List<object> keepExpanded = new List<object>();
                foreach (object id in GetExpandedIds(data))
                {
                    if (ShouldStayExpanded(id)) keepExpanded.Add(id);
                }

                SetExpandedIds(data, keepExpanded);
            }

            EditorApplication.RepaintProjectWindow();
        }

        /// <summary>
        /// Structural rows and the tree roots — Assets, Packages — survive a collapse-all. Folding
        /// the roots away would empty the window rather than tidy it.
        /// </summary>
        private static bool ShouldStayExpanded(object id)
        {
            Object resolved = HelpfulEditorObjectId.Resolve(id);
            if (!resolved) return true;

            string path = AssetDatabase.GetAssetPath(resolved);
            return string.IsNullOrEmpty(path) || path.IndexOf('/') < 0;
        }

        /// <summary>
        /// Toggles the tree row at a given y. Rows like the Packages root have no asset behind them,
        /// so they cannot be addressed by path — but they are ordinary tree rows, and the GUI can
        /// say where each one sits.
        /// </summary>
        public static bool ToggleProjectExpandedAtRow(float rowY)
        {
            try
            {
                foreach (object treeView in GetProjectTreeViews())
                {
                    object data = DataOf(treeView);
                    object gui = GetMemberValue(treeView, "gui");
                    if (data == null || gui == null) continue;

                    MethodInfo getRows = data.GetType().GetMethod("GetRows", AnyInstance, null, Type.EmptyTypes, null);
                    if (getRows?.Invoke(data, null) is not IList rows) continue;

                    MethodInfo getRowRect = gui.GetType().GetMethod("GetRowRect", AnyInstance, null,
                        new[] { typeof(int), typeof(float) }, null);
                    if (getRowRect == null) continue;

                    for (int row = 0; row < rows.Count; row++)
                    {
                        if (getRowRect.Invoke(gui, new object[] { row, 1f }) is not Rect rect) continue;
                        if (Mathf.Abs(rect.y - rowY) > 0.5f) continue;

                        object id = GetMemberValue(rows[row], "id");
                        if (id == null) return false;

                        bool expanded = InvokeWithId(data, "IsExpanded", id) is bool current && current;
                        InvokeWithId(data, "SetExpanded", id, !expanded);

                        EditorApplication.RepaintProjectWindow();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return false;
        }

        /// <summary>Collapses every GameObject but leaves the scene headers open.</summary>
        public static void CollapseAllHierarchy()
        {
            object data = DataOf(GetHierarchyTreeView());
            if (data == null) return;

            List<object> keepExpanded = new List<object>();
            foreach (object id in GetExpandedIds(data))
            {
                // Scene headers are rows without an object behind them, so they are what survives.
                if (!HelpfulEditorObjectId.Resolve(id)) keepExpanded.Add(id);
            }

            SetExpandedIds(data, keepExpanded);
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// Prefers the same call the foldout arrow makes, so the row slides open exactly as it does
        /// on a click. Whole-subtree changes, and any version where that call is missing, fall back
        /// to setting the data source directly, which applies instantly.
        /// </summary>
        /// <summary>Returns false when nothing actually changed, so callers can skip a pointless repaint.</summary>
        private static bool SetExpanded(object treeView, object rawId, bool expanded, bool includeChildren)
        {
            object data = DataOf(treeView);
            if (data == null) return false;

            object item = InvokeWithId(data, "FindItem", rawId);
            if (item == null) return false;

            // A row with nothing under it has no expanded state to change. Poking the data source
            // anyway makes the tree reload, which reads as the view flickering or "refreshing".
            if (!HasChildren(item)) return false;

            if (!includeChildren && IsExpandedOn(data, rawId) == expanded) return false;

            if (!includeChildren && TryAnimatedExpand(treeView, data, rawId, expanded)) return true;

            InvokeWithId(data, includeChildren ? "SetExpandedWithChildren" : "SetExpanded", rawId, expanded);
            return true;
        }

        private static bool HasChildren(object item)
        {
            try
            {
                PropertyInfo property = item.GetType().GetProperty("hasChildren", AnyInstance);
                object value = property?.GetValue(item);
                return !(value is bool hasChildren) || hasChildren;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return true;
            }
        }

        private static bool IsExpandedOn(object data, object rawId)
        {
            return InvokeWithId(data, "IsExpanded", rawId) is bool expanded && expanded;
        }

        /// <summary>
        /// Plays a single fold with the tree's own slide animation.
        ///
        /// ChangeFoldingForSingleItem is preferred over UserInputChangedExpandedState, which is the
        /// call a click on the arrow makes. That one reads Event.current to see whether Alt is held,
        /// so it throws outright when a queued fold is released from the update loop where there is
        /// no event — and on the occasions it did have one, holding Alt for something unrelated
        /// would silently turn a queued fold into an expand-the-whole-subtree.
        /// </summary>
        private static bool TryAnimatedExpand(object treeView, object data, object rawId, bool expanded)
        {
            if (treeView == null) return false;

            // Once the animated path has failed there is no point paying for the exception again on
            // every remaining fold; the instant path still applies them.
            if (_foldAnimationResolved && !_foldAnimationAvailable) return false;

            try
            {
                if (TryChangeFolding(treeView, rawId, expanded)) return true;

                // Only reached on a version without that call, and only usable mid-event.
                if (Event.current == null) return false;

                object item = InvokeWithId(data, "FindItem", rawId);
                if (item == null) return false;

                if (!(InvokeWithId(data, "GetRow", rawId) is int row) || row < 0) return false;

                MethodInfo method = FindUserInputChangedExpandedState(treeView.GetType(), item);
                if (method == null) return false;

                method.Invoke(treeView, new[] { item, (object)row, expanded });
                return true;
            }
            catch (Exception e)
            {
                _foldAnimationResolved = true;
                _foldAnimationAvailable = false;

                WarnFoldAnimation(e);
            }

            return false;
        }

        private static bool TryChangeFolding(object treeView, object rawId, bool expanded)
        {
            MethodInfo method = FindChangeFolding(treeView.GetType());
            if (method == null) return false;

            object id = ConvertId(rawId, method.GetParameters()[0].ParameterType);
            if (id == null) return false;

            method.Invoke(treeView, new[] { id, (object)expanded });
            return true;
        }

        private static MethodInfo FindChangeFolding(Type treeViewType)
        {
            foreach (MethodInfo method in treeViewType.GetMethods(AnyInstance))
            {
                if (method.Name != "ChangeFoldingForSingleItem") continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 || parameters[1].ParameterType != typeof(bool)) continue;

                return method;
            }

            return null;
        }

        /// <param name="item">
        /// The row the call is for, so the overload is matched against the tree's own item type.
        /// Null probes for the method's existence without one to hand.
        /// </param>
        private static MethodInfo FindUserInputChangedExpandedState(Type treeViewType, object item)
        {
            foreach (MethodInfo method in treeViewType.GetMethods(AnyInstance))
            {
                if (method.Name != "UserInputChangedExpandedState") continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 3) continue;
                if (item != null && !parameters[0].ParameterType.IsInstanceOfType(item)) continue;
                if (parameters[1].ParameterType != typeof(int) || parameters[2].ParameterType != typeof(bool)) continue;

                return method;
            }

            return null;
        }

        /// <summary>
        /// Whether folds can be played rather than applied instantly. Resolved from a live tree, so
        /// the answer is deferred rather than cached negative while no Project window is open.
        /// </summary>
        /// <param name="kind">
        /// Only used to find a live tree to probe. The answer is shared between the two: both are the
        /// same TreeViewController type, so what one supports the other does, and a failure in one
        /// predicts the other.
        /// </param>
        public static bool CanAnimateFolds(TreeKind kind)
        {
            if (_foldAnimationResolved) return _foldAnimationAvailable;

            object treeView = TreeViewOf(kind);
            if (treeView == null) return false;

            _foldAnimationResolved = true;
            _foldAnimationAvailable = GetMemberValue(treeView, "m_ExpansionAnimator") != null
                                      && (FindChangeFolding(treeView.GetType()) != null
                                          || FindUserInputChangedExpandedState(treeView.GetType(), null) != null);

            return _foldAnimationAvailable;
        }

        /// <summary>Whether the tree is running its own scroll-to-row animation, which ours must not fight.</summary>
        public static bool IsProjectTreeFraming()
        {
            object framing = GetMemberValue(GetActiveProjectTreeView(), "m_FramingAnimFloat");
            return GetMemberValue(framing, "isAnimating") is bool animating && animating;
        }

        private static object GetHierarchyTreeView()
        {
            try
            {
                Type windowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                EditorWindow window = FindWindow(windowType);
                if (!window) return null;

                object sceneHierarchy = windowType.GetProperty("sceneHierarchy", AnyInstance)?.GetValue(window)
                                        ?? windowType.GetField("m_SceneHierarchy", AnyInstance)?.GetValue(window);
                if (sceneHierarchy == null) return null;

                return GetMemberValue(sceneHierarchy, "treeView") ?? GetMemberValue(sceneHierarchy, "m_TreeView");
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        /// <summary>
        /// The tree the Project window is actually showing rows in: the folder tree in the
        /// two-column layout, the asset tree in one column. Both fields can outlive a layout switch,
        /// and acting on the one that is no longer on screen silently does nothing.
        /// </summary>
        private static object GetActiveProjectTreeView()
        {
            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                EditorWindow window = FindWindow(browserType);
                if (!window) return null;

                string fieldName = IsTwoColumnLayout(browserType, window) ? "m_FolderTree" : "m_AssetTree";
                return browserType.GetField(fieldName, AnyInstance)?.GetValue(window);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        /// <summary>Every row the active tree is currently showing, top to bottom. Null when unavailable.</summary>
        public static IList GetProjectRows()
        {
            object data = DataOf(GetActiveProjectTreeView());
            if (data == null) return null;

            try
            {
                MethodInfo getRows = data.GetType().GetMethod("GetRows", AnyInstance, null, Type.EmptyTypes, null);
                return getRows?.Invoke(data, null) as IList;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        public static object GetItemId(object item) => GetMemberValue(item, "id");

        public static object GetItemParent(object item) => GetMemberValue(item, "parent");

        public static IList GetItemChildren(object item) => GetMemberValue(item, "children") as IList;

        private static object TreeViewOf(TreeKind kind)
        {
            return kind == TreeKind.Project ? GetActiveProjectTreeView() : GetHierarchyTreeView();
        }

        /// <summary>
        /// Whether the tree is mid-fold. Only one row can animate at a time, so a queued fold has to
        /// wait for this to go quiet or it replaces the one already running and the rest snap.
        /// </summary>
        public static bool IsTreeAnimating(TreeKind kind)
        {
            object animator = GetMemberValue(TreeViewOf(kind), "m_ExpansionAnimator");
            return GetMemberValue(animator, "isAnimating") is bool animating && animating;
        }

        public static object[] GetExpandedIds(TreeKind kind)
        {
            return GetExpandedIds(DataOf(TreeViewOf(kind)));
        }

        public static int GetRowIndex(TreeKind kind, object rawId)
        {
            return InvokeWithId(DataOf(TreeViewOf(kind)), "GetRow", rawId) is int row ? row : -1;
        }

        /// <summary>
        /// Whether the tree's row lookup currently disagrees with its own row list — GetRow reports
        /// an index whose row is a different item. It happens while collapsing long trees, and acting
        /// on the answer folds the wrong row. The cure is to let the tree rebuild and ask again.
        /// </summary>
        public static bool IsRowStale(TreeKind kind, object rawId)
        {
            try
            {
                object treeView = TreeViewOf(kind);
                object data = DataOf(treeView);
                if (data == null || rawId == null) return false;

                MethodInfo getRows = data.GetType().GetMethod("GetRows", AnyInstance, null, Type.EmptyTypes, null);
                if (getRows?.Invoke(data, null) is not IList rows) return false;

                if (InvokeWithId(data, "GetRow", rawId) is not int row) return false;
                if (row < 0 || row >= rows.Count) return false;

                object rowId = GetMemberValue(rows[row], "id");
                if (rowId == null) return false;

                object wanted = ConvertId(rawId, rowId.GetType());

                return wanted != null && !wanted.Equals(rowId);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        /// <summary>
        /// Pokes the window with an event that does nothing, which is enough to make it rebuild its
        /// rows. Used to shake a tree out of the stale-row state above.
        /// </summary>
        public static void NudgeTree(TreeKind kind)
        {
            try
            {
                string typeName = kind == TreeKind.Project ? "UnityEditor.ProjectBrowser" : "UnityEditor.SceneHierarchyWindow";

                FindWindow(typeof(EditorWindow).Assembly.GetType(typeName))
                    ?.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.None });
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        public static void RepaintTree(TreeKind kind)
        {
            if (kind == TreeKind.Project) EditorApplication.RepaintProjectWindow();
            else EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>Folds a single row with the tree's own slide animation, falling back to an instant change.</summary>
        public static bool SetExpandedAnimated(TreeKind kind, object rawId, bool expanded)
        {
            object treeView = TreeViewOf(kind);
            object data = DataOf(treeView);
            if (data == null || rawId == null) return false;

            object item = InvokeWithId(data, "FindItem", rawId);
            if (item == null || !HasChildren(item)) return false;
            if (IsExpandedOn(data, rawId) == expanded) return false;

            if (!TryAnimatedExpand(treeView, data, rawId, expanded)) InvokeWithId(data, "SetExpanded", rawId, expanded);

            return true;
        }

        public static void SetExpandedImmediate(TreeKind kind, object rawId, bool expanded)
        {
            object data = DataOf(TreeViewOf(kind));
            if (data == null || rawId == null) return;

            InvokeWithId(data, "SetExpanded", rawId, expanded);
        }

        public static bool TryGetProjectScroll(out float scroll)
        {
            scroll = 0f;

            object state = GetMemberValue(GetActiveProjectTreeView(), "state");
            if (GetMemberValue(state, "scrollPos") is not Vector2 position) return false;

            scroll = position.y;
            return true;
        }

        public static bool SetProjectScroll(float scroll)
        {
            object state = GetMemberValue(GetActiveProjectTreeView(), "state");
            if (state == null) return false;

            try
            {
                FieldInfo field = state.GetType().GetField("scrollPos", AnyInstance);
                if (field == null || field.FieldType != typeof(Vector2)) return false;

                field.SetValue(state, new Vector2(0f, scroll));
                return true;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        private static IEnumerable<object> GetProjectTreeViews()
        {
            List<object> trees = new List<object>();

            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                EditorWindow window = FindWindow(browserType);
                if (!window) return trees;

                foreach (string fieldName in new[] { "m_AssetTree", "m_FolderTree" })
                {
                    object treeView = browserType.GetField(fieldName, AnyInstance)?.GetValue(window);
                    if (treeView != null) trees.Add(treeView);
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return trees;
        }

        private static object DataOf(object treeView)
        {
            return treeView == null ? null : GetMemberValue(treeView, "data");
        }

        // Shared scan rather than one of its own: TryGetProjectListAreaRect runs this every editor
        // tick while the cursor is over the Project window, and a raw FindObjectsOfTypeAll there
        // costs the whole loaded object set every time.
        private static EditorWindow FindWindow(Type windowType) => HelpfulEditorWindows.First(windowType);

        /// <summary>
        /// Whether the Project window is renaming this particular asset. Asked of the rename overlay
        /// itself because <c>EditorGUIUtility.editingTextField</c> cannot answer it: that flag is
        /// global — a focused field anywhere in the editor sets it — so treating it as "this row is
        /// being renamed" blanks a row's overlay whenever something else happens to hold the caret.
        ///
        /// Both halves are asked. A rename runs in the list area in the two-column layout and in the
        /// tree in one column, and which of them is on screen is the layout's business, not ours.
        /// </summary>
        public static bool IsProjectRenaming(object rawId)
        {
            if (rawId == null) return false;

            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                if (browserType == null) return false;

                FieldInfo listAreaField = browserType.GetField("m_ListArea", AnyInstance);

                foreach (EditorWindow window in HelpfulEditorWindows.AllProjectBrowsers())
                {
                    if (IsRenamingIn(FindRenameOverlay(listAreaField?.GetValue(window)), rawId)) return true;
                }

                object state = GetMemberValue(GetActiveProjectTreeView(), "state");

                return IsRenamingIn(FindRenameOverlay(state), rawId);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        /// <summary>
        /// The overlay is reached by a method on the list area and by a property on the tree's state,
        /// and its own type went generic in Unity 6 — so it is found by name and used by name rather
        /// than named anywhere here.
        /// </summary>
        private static object FindRenameOverlay(object owner)
        {
            if (owner == null) return null;

            MethodInfo getter = owner.GetType().GetMethod("GetRenameOverlay", AnyInstance, null, Type.EmptyTypes, null);
            if (getter != null) return getter.Invoke(owner, null);

            return GetMemberValue(owner, "renameOverlay") ?? GetMemberValue(owner, "m_RenameOverlay");
        }

        private static bool IsRenamingIn(object overlay, object rawId)
        {
            if (overlay == null) return false;

            MethodInfo isRenaming = overlay.GetType().GetMethod("IsRenaming", AnyInstance, null, Type.EmptyTypes, null);
            if (!(isRenaming?.Invoke(overlay, null) is bool renaming) || !renaming) return false;

            // Which object it is renaming, in whichever id type this version of the overlay holds.
            object userData = GetMemberValue(overlay, "userData");
            if (userData == null) return false;

            object converted = HelpfulEditorObjectId.ConvertTo(rawId, userData.GetType());

            return converted != null && converted.Equals(userData);
        }

        private static object GetMemberValue(object instance, string memberName)
        {
            if (instance == null) return null;

            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(memberName, AnyInstance);
            if (property != null) return property.GetValue(instance);

            FieldInfo field = type.GetField(memberName, AnyInstance);
            return field?.GetValue(instance);
        }

        private static object InvokeWithId(object instance, string methodName, object rawId, params object[] extraArgs)
        {
            if (instance == null || rawId == null) return null;

            try
            {
                foreach (MethodInfo method in instance.GetType().GetMethods(AnyInstance))
                {
                    if (method.Name != methodName) continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1 + extraArgs.Length) continue;

                    object id = ConvertId(rawId, parameters[0].ParameterType);
                    if (id == null) continue;

                    object[] args = new object[parameters.Length];
                    args[0] = id;
                    for (int i = 0; i < extraArgs.Length; i++) args[i + 1] = extraArgs[i];

                    return method.Invoke(instance, args);
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return null;
        }

        private static object ConvertId(object rawId, Type targetType)
        {
            return HelpfulEditorObjectId.ConvertTo(rawId, targetType);
        }

        private static object[] GetExpandedIds(object dataSource)
        {
            if (dataSource == null) return Array.Empty<object>();

            try
            {
                MethodInfo method = dataSource.GetType().GetMethod("GetExpandedIDs", AnyInstance, null, Type.EmptyTypes, null);
                if (!(method?.Invoke(dataSource, null) is IEnumerable raw)) return Array.Empty<object>();

                List<object> ids = new List<object>();
                foreach (object id in raw)
                {
                    if (id != null) ids.Add(id);
                }

                return ids.ToArray();
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return Array.Empty<object>();
            }
        }

        private static void SetExpandedIds(object dataSource, IEnumerable<object> ids)
        {
            if (dataSource == null) return;

            try
            {
                foreach (MethodInfo method in dataSource.GetType().GetMethods(AnyInstance))
                {
                    if (method.Name != "SetExpandedIDs") continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1 || !parameters[0].ParameterType.IsArray) continue;

                    Type elementType = parameters[0].ParameterType.GetElementType();
                    if (elementType == null) continue;

                    List<object> converted = new List<object>();
                    foreach (object id in ids)
                    {
                        object value = ConvertId(id, elementType);
                        if (value != null) converted.Add(value);
                    }

                    Array array = Array.CreateInstance(elementType, converted.Count);
                    for (int i = 0; i < converted.Count; i++) array.SetValue(converted[i], i);

                    method.Invoke(dataSource, new object[] { array });
                    return;
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Tree expand/collapse is unavailable on this Unity version — those actions will do nothing. ({Describe(e)})");
        }

        /// <summary>
        /// Losing the animation is not losing the action, so it says so rather than reusing the
        /// warning for a tree that cannot be folded at all.
        /// </summary>
        private static void WarnFoldAnimation(Exception e)
        {
            if (_foldAnimationWarned) return;

            _foldAnimationWarned = true;
            Debug.LogWarning($"[HelpfulEditor] Project folds will apply instantly rather than animating on this Unity version. ({Describe(e)})");
        }

        /// <summary>
        /// A reflected call reports every failure as the same invocation wrapper, whose message says
        /// nothing about what actually went wrong.
        /// </summary>
        private static string Describe(Exception e)
        {
            Exception cause = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;

            // The top frame names the internal method that actually failed, which is the only part
            // that says anything useful about an editor-version mismatch.
            string frame = cause.StackTrace?.Split('\n')[0].Trim();

            return string.IsNullOrEmpty(frame)
                ? $"{cause.GetType().Name}: {cause.Message}"
                : $"{cause.GetType().Name}: {cause.Message} — at {frame}";
        }
    }
}
