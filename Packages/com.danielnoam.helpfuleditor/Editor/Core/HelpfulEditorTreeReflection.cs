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

        public static bool IsProjectTwoColumnLayout()
        {
            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                EditorWindow window = FindWindow(browserType);

                return window && IsTwoColumnLayout(browserType, window);
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

        private static bool TryAnimatedExpand(object treeView, object data, object rawId, bool expanded)
        {
            if (treeView == null) return false;

            try
            {
                object item = InvokeWithId(data, "FindItem", rawId);
                if (item == null) return false;

                if (!(InvokeWithId(data, "GetRow", rawId) is int row) || row < 0) return false;

                foreach (MethodInfo method in treeView.GetType().GetMethods(AnyInstance))
                {
                    if (method.Name != "UserInputChangedExpandedState") continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 3) continue;
                    if (!parameters[0].ParameterType.IsInstanceOfType(item)) continue;
                    if (parameters[1].ParameterType != typeof(int) || parameters[2].ParameterType != typeof(bool)) continue;

                    method.Invoke(treeView, new[] { item, (object)row, expanded });
                    return true;
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            return false;
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

        private static EditorWindow FindWindow(Type windowType)
        {
            if (windowType == null) return null;

            Object[] windows = Resources.FindObjectsOfTypeAll(windowType);
            return windows.Length > 0 ? windows[0] as EditorWindow : null;
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
            Debug.LogWarning($"[HelpfulEditor] Tree expand/collapse is unavailable on this Unity version — those actions will do nothing. ({e.Message})");
        }
    }
}
