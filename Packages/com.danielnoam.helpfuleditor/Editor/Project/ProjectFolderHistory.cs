using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// Browser-style back/forward history of the folders visited in the Project window. Unity keeps
    /// no such history and exposes no API for changing the browsed folder, so the current folder is
    /// polled and navigation goes through ProjectBrowser internals — same risk tier as Tabs, guarded
    /// so a version bump degrades to "back/forward do nothing" rather than throwing.
    /// </summary>
    internal static class ProjectFolderHistory
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int MaxEntries = 64;

        private static readonly List<string> History = new List<string>();

        private const double PollInterval = 0.2;

        private static int _index = -1;
        private static double _lastPoll;
        private static bool _navigating;
        private static bool _unavailable;

        private static bool CanGoBack => _index > 0;

        private static bool CanGoForward => _index >= 0 && _index < History.Count - 1;

        /// <summary>
        /// Records the folder currently being browsed. Called from the Project module's update poll
        /// because Unity raises no event when the browsed folder changes.
        /// </summary>
        public static void RecordCurrentFolder()
        {
            if (_unavailable || _navigating) return;

            // Polled from the editor update loop, which ticks far faster than a folder can change.
            if (EditorApplication.timeSinceStartup - _lastPoll < PollInterval) return;
            _lastPoll = EditorApplication.timeSinceStartup;

            string current = GetActiveFolderPath();
            if (string.IsNullOrEmpty(current)) return;
            if (_index >= 0 && _index < History.Count && History[_index] == current) return;

            // Moving somewhere new abandons the forward entries, exactly like a browser.
            if (_index < History.Count - 1) History.RemoveRange(_index + 1, History.Count - _index - 1);

            History.Add(current);

            if (History.Count > MaxEntries) History.RemoveAt(0);

            _index = History.Count - 1;
        }

        public static bool Back()
        {
            if (!CanGoBack) return false;

            _index--;
            return NavigateToCurrent();
        }

        public static bool Forward()
        {
            if (!CanGoForward) return false;

            _index++;
            return NavigateToCurrent();
        }

        private static bool NavigateToCurrent()
        {
            string path = History[_index];

            // Folders can be deleted or moved while they sit in the history.
            if (!AssetDatabase.IsValidFolder(path))
            {
                History.RemoveAt(_index);
                _index = Mathf.Clamp(_index, 0, History.Count - 1);
                return false;
            }

            _navigating = true;

            try
            {
                return ShowFolder(path);
            }
            finally
            {
                _navigating = false;
            }
        }

        private static string GetActiveFolderPath()
        {
            try
            {
                MethodInfo method = typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                return method?.Invoke(null, null) as string;
            }
            catch (Exception e)
            {
                Disable(e.Message);
                return null;
            }
        }

        private static bool ShowFolder(string path)
        {
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (!folder) return false;

            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                Object[] windows = browserType != null ? Resources.FindObjectsOfTypeAll(browserType) : Array.Empty<Object>();

                if (windows.Length > 0 && windows[0] is EditorWindow browser && TryShowFolderContents(browserType, browser, folder))
                {
                    browser.Repaint();
                    return true;
                }
            }
            catch (Exception e)
            {
                Disable(e.Message);
            }

            // Fallback: at least reveal the folder, even if the browsed location cannot be set.
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
            return true;
        }

        private static bool TryShowFolderContents(Type browserType, EditorWindow browser, Object folder)
        {
            object rawId = HelpfulEditorObjectId.Raw(folder);

            foreach (MethodInfo method in browserType.GetMethods(AnyInstance))
            {
                if (method.Name != "ShowFolderContents") continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 || parameters[1].ParameterType != typeof(bool)) continue;

                object id = HelpfulEditorObjectId.ConvertTo(rawId, parameters[0].ParameterType);
                if (id == null) continue;

                method.Invoke(browser, new[] { id, (object)true });
                return true;
            }

            return false;
        }

        private static void Disable(string reason)
        {
            _unavailable = true;
            Debug.LogWarning($"[HelpfulEditor] Project folder history is unavailable on this Unity version — back/forward will do nothing. ({reason})");
        }
    }
}
