using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Opens a folder in a second Project window, the way a middle-click opens a link in a new tab.
    /// Unity has the machinery for this — every Project window can be told which folder to show — but
    /// no menu item or shortcut reaches it, and ProjectBrowser is internal.
    /// </summary>
    internal static class ProjectFolderTab
    {
        private static readonly Type ProjectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");

        private static MethodInfo _showFolderContents;
        private static bool _resolved;

        public static void Open(string folderPath)
        {
            if (ProjectBrowserType == null || string.IsNullOrEmpty(folderPath)) return;

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (!folder) return;

            // Captured now rather than in the deferred call: by then the mouse has moved on and the
            // window that was clicked is no longer the one under the cursor.
            EditorWindow source = EditorWindow.mouseOverWindow ? EditorWindow.mouseOverWindow : EditorWindow.focusedWindow;

            // Deferred: creating and focusing a window from inside the click that asked for it
            // reshuffles focus while the event is still being dispatched.
            EditorApplication.delayCall += () => Create(folder, source);
        }

        private static void Create(Object folder, EditorWindow source)
        {
            if (!(ScriptableObject.CreateInstance(ProjectBrowserType) is EditorWindow window)) return;

            if (!HelpfulEditorSettings.Project.autoDock || !TryDockBeside(source, window)) window.Show();

            window.Focus();

            // A second hop: the browser builds its trees on its first OnGUI, and asking it to show a
            // folder before that leaves it on whatever the last window was looking at.
            EditorApplication.delayCall += () => ShowFolder(window, folder);
        }

        private static void ShowFolder(EditorWindow window, Object folder)
        {
            if (!window || !folder) return;

            MethodInfo method = ResolveShowFolderContents();

            if (method == null)
            {
                // Without the internal call the folder can at least be selected, which lands the new
                // window on it in one-column mode.
                Selection.activeObject = folder;
                EditorGUIUtility.PingObject(folder);
                return;
            }

            try
            {
                ParameterInfo[] parameters = method.GetParameters();

                // The id parameter is an int on older versions and an EntityId from 6.4 on.
                object id = HelpfulEditorObjectId.ConvertTo(HelpfulEditorObjectId.Raw(folder), parameters[0].ParameterType);
                if (id == null) return;

                method.Invoke(window, parameters.Length > 1 ? new[] { id, (object)true } : new[] { id });
                window.Repaint();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HelpfulEditor] Could not open the folder in the new Project window: {e.Message}");
            }
        }

        /// <summary>
        /// Adds the new window as a tab in the same dock as the one that was clicked, so it appears
        /// beside its source rather than floating over the middle of the screen.
        ///
        /// A window's host is a DockArea only while it is docked — a floating Project window has a
        /// plain HostView with no AddTab, and there the new window simply floats too.
        /// </summary>
        private static bool TryDockBeside(EditorWindow source, EditorWindow window)
        {
            if (!source || ProjectBrowserType == null || !ProjectBrowserType.IsInstanceOfType(source)) return false;

            try
            {
                FieldInfo parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);

                object host = parentField?.GetValue(source);
                if (host == null) return false;

                MethodInfo addTab = FindAddTab(host.GetType());
                if (addTab == null) return false;

                addTab.Invoke(host, BuildArguments(addTab, window));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HelpfulEditor] Could not dock the new Project window, showing it floating instead: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Matched by shape rather than by an exact signature. AddTab carries trailing optional
        /// parameters that differ between versions, and asking for the one-argument form finds
        /// nothing at all — which looked exactly like the window not being docked anywhere.
        /// The overload taking an index first is skipped by requiring the window to come first.
        /// </summary>
        private static MethodInfo FindAddTab(Type hostType)
        {
            foreach (MethodInfo candidate in hostType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (candidate.Name != "AddTab") continue;

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length == 0 || !parameters[0].ParameterType.IsAssignableFrom(typeof(EditorWindow))) continue;

                return candidate;
            }

            return null;
        }

        /// <summary>Fills the trailing optional parameters with whatever the method itself defaults them to.</summary>
        private static object[] BuildArguments(MethodInfo method, EditorWindow window)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];

            arguments[0] = window;

            for (int i = 1; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                arguments[i] = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }

            return arguments;
        }

        private static MethodInfo ResolveShowFolderContents()
        {
            if (_resolved) return _showFolderContents;
            _resolved = true;

            _showFolderContents = ProjectBrowserType?.GetMethod("ShowFolderContents",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return _showFolderContents;
        }
    }
}
