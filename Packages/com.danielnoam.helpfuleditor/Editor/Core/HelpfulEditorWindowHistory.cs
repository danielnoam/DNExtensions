using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Remembers windows as they are closed so the last one can be brought back where it was. Closing
    /// an editor window is otherwise irreversible, which is the only reason Close Focused Window ever
    /// needed a list of windows it refused to touch.
    ///
    /// Snapshots are held in memory and deliberately not serialised: they carry live object
    /// references and a dock area that a domain reload would invalidate anyway.
    /// </summary>
    internal static class HelpfulEditorWindowHistory
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int MaxRemembered = 16;

        private sealed class Snapshot
        {
            public Type windowType;
            public string title;
            public Object dockArea;
            public int tabIndex = -1;
            public bool wasFocused;

            public bool isBrowser;
            public bool isLocked;
            public string folderPath;
            public int viewMode = -1;
            public int gridSize = -1;

            /// <summary>Held directly rather than as a GlobalObjectId — the snapshot never outlives the session.</summary>
            public Object inspectedObject;
        }

        private static readonly List<Snapshot> Closed = new List<Snapshot>();

        private static bool _warned;

        public static bool CanReopen => Closed.Count > 0;

        /// <summary>Call immediately before closing, while the window still has its state to read.</summary>
        public static void Remember(EditorWindow window)
        {
            if (!window) return;

            try
            {
                Snapshot snapshot = new Snapshot
                {
                    windowType = window.GetType(),
                    title = window.titleContent.text,
                    wasFocused = window.hasFocus
                };

                snapshot.dockArea = HelpfulEditorDockArea.Of(window);
                snapshot.tabIndex = HelpfulEditorDockArea.IndexOfTab(snapshot.dockArea, window);

                CaptureBrowser(window, snapshot);
                CaptureInspected(window, snapshot);

                Closed.Add(snapshot);

                if (Closed.Count > MaxRemembered) Closed.RemoveAt(0);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        public static bool ReopenLast()
        {
            if (Closed.Count == 0) return false;

            Snapshot snapshot = Closed[Closed.Count - 1];
            Closed.RemoveAt(Closed.Count - 1);

            try
            {
                // A Properties window is reopened through the editor's own menu item, which knows how
                // to bind it to an object. Rebuilding that binding by hand means reaching into the
                // window's tracker, and the menu item is both simpler and version-proof.
                if (snapshot.inspectedObject) return ReopenPropertyWindow(snapshot);

                return ReopenWindow(snapshot);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        private static bool ReopenWindow(Snapshot snapshot)
        {
            if (snapshot.windowType == null) return false;

            EditorWindow previous = EditorWindow.focusedWindow;

            if (ScriptableObject.CreateInstance(snapshot.windowType) is not EditorWindow window) return false;

            // The dock area may have been destroyed along with its last tab, in which case the window
            // comes back floating rather than not at all.
            if (!HelpfulEditorDockArea.AddTab(snapshot.dockArea, window, snapshot.tabIndex)) window.Show();

            RestoreBrowser(window, snapshot);

            // Windows opened through GetWindow set their title there, so one built by CreateInstance
            // comes up titled with its own type name instead.
            if (!string.IsNullOrEmpty(snapshot.title)) window.titleContent.text = snapshot.title;

            HelpfulEditorDockArea.ClearTitleCache();

            // Both are driven by polling and both are about to be wrong for the window just restored.
            HelpfulEditorWindowTitles.RequestRefresh();
            Project.ProjectCreateFolderButton.RequestRefresh();

            if (snapshot.wasFocused || !previous) window.Focus();
            else previous.Focus();

            return true;
        }

        /// <summary>
        /// The Properties menu item acts on the selection, so the current selection is put back
        /// afterwards — reopening a window should not be a selection change.
        /// </summary>
        private static bool ReopenPropertyWindow(Snapshot snapshot)
        {
            Object[] previousSelection = Selection.objects;

            try
            {
                Selection.activeObject = snapshot.inspectedObject;
                return EditorApplication.ExecuteMenuItem("Assets/Properties...");
            }
            finally
            {
                Selection.objects = previousSelection;
            }
        }

        private static void CaptureBrowser(EditorWindow window, Snapshot snapshot)
        {
            if (!HelpfulEditorWindows.IsProjectBrowser(window)) return;

            snapshot.isBrowser = true;
            snapshot.isLocked = GetMember(window, "isLocked") is bool locked && locked;

            if (GetMember(window, "m_ViewMode") is int viewMode) snapshot.viewMode = viewMode;

            object listArea = GetMember(window, "m_ListArea");
            if (GetMember(listArea, "gridSize") is int gridSize) snapshot.gridSize = gridSize;

            snapshot.folderPath = ActiveFolderOf(window);
        }

        /// <summary>
        /// The object a Properties window is showing. Only floating property editors have one — the
        /// main Inspector follows the selection and has nothing of its own to restore.
        /// </summary>
        private static void CaptureInspected(EditorWindow window, Snapshot snapshot)
        {
            if (!HelpfulEditorWindows.IsInspector(window)) return;

            snapshot.inspectedObject = GetMember(window, "m_InspectedObject") as Object;
        }

        private static void RestoreBrowser(EditorWindow window, Snapshot snapshot)
        {
            if (!snapshot.isBrowser) return;

            try
            {
                window.GetType().GetMethod("Init", AnyInstance)?.Invoke(window, null);

                RestoreViewMode(window, snapshot);
                RestoreGridSize(window, snapshot);

                if (string.IsNullOrEmpty(snapshot.folderPath)) return;
                if (!AssetDatabase.IsValidFolder(snapshot.folderPath)) return;

                HelpfulEditorProjectWindow.ShowFolder(window, snapshot.folderPath);

                if (snapshot.isLocked) HelpfulEditorProjectWindow.SetLocked(window, true);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// Set through SetViewMode rather than by writing the field: the browser rebuilds its trees
        /// and rects when the mode changes, and a window whose field says two columns while its
        /// layout says one draws nothing.
        /// </summary>
        private static void RestoreViewMode(EditorWindow window, Snapshot snapshot)
        {
            if (snapshot.viewMode < 0) return;

            MethodInfo setViewMode = window.GetType().GetMethod("SetViewMode", AnyInstance);
            ParameterInfo[] parameters = setViewMode?.GetParameters();
            if (parameters == null || parameters.Length != 1 || !parameters[0].ParameterType.IsEnum) return;

            setViewMode.Invoke(window, new[] { Enum.ToObject(parameters[0].ParameterType, snapshot.viewMode) });
        }

        private static void RestoreGridSize(EditorWindow window, Snapshot snapshot)
        {
            if (snapshot.gridSize < 0) return;

            object listArea = GetMember(window, "m_ListArea");
            if (listArea == null) return;

            listArea.GetType().GetProperty("gridSize", AnyInstance)?.SetValue(listArea, snapshot.gridSize);
        }

        private static string ActiveFolderOf(EditorWindow window)
        {
            return window.GetType().GetMethod("GetActiveFolderPath", AnyInstance)?.Invoke(window, null) as string;
        }

        private static object GetMember(object instance, string memberName)
        {
            if (instance == null) return null;

            try
            {
                Type type = instance.GetType();

                PropertyInfo property = type.GetProperty(memberName, AnyInstance);
                if (property != null) return property.GetValue(instance);

                return type.GetField(memberName, AnyInstance)?.GetValue(instance);
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Reopening closed windows is unavailable on this Unity version. ({e.Message})");
        }
    }
}
