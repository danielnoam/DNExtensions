using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// The handful of ProjectBrowser operations the suite needs on a specific window rather than on
    /// "the" Project window: which folder it is showing, whether it is locked, and telling it to show
    /// a folder. Unity exposes none of these, and ProjectBrowser is internal.
    /// </summary>
    internal static class HelpfulEditorProjectWindow
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static MethodInfo _showFolderContents;
        private static MethodInfo _activeFolderPath;
        private static PropertyInfo _isLocked;
        private static Type _resolvedFor;
        private static bool _warned;

        /// <summary>
        /// Member lookups are resolved once per type rather than per call. These sit on polling paths
        /// that run several times a second, and a GetProperty by name on every one of them is the
        /// expensive part of asking a cheap question.
        /// </summary>
        private static void Resolve(Type browserType)
        {
            if (_resolvedFor == browserType) return;

            _resolvedFor = browserType;

            try
            {
                _showFolderContents = browserType.GetMethod("ShowFolderContents", AnyInstance);
                _activeFolderPath = browserType.GetMethod("GetActiveFolderPath", AnyInstance);
                _isLocked = browserType.GetProperty("isLocked", AnyInstance);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// Points the window at a folder. Returns false when the internal call is missing, which
        /// leaves the caller to fall back to selecting the folder instead.
        /// </summary>
        public static bool ShowFolder(EditorWindow browser, string folderPath)
        {
            if (!browser || string.IsNullOrEmpty(folderPath)) return false;

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (!folder) return false;

            Resolve(browser.GetType());

            MethodInfo method = _showFolderContents;
            if (method == null) return false;

            try
            {
                ParameterInfo[] parameters = method.GetParameters();

                // The id parameter is an int on older versions and an EntityId from 6.4 on.
                object id = HelpfulEditorObjectId.ConvertTo(HelpfulEditorObjectId.Raw(folder), parameters[0].ParameterType);
                if (id == null) return false;

                method.Invoke(browser, parameters.Length > 1 ? new[] { id, (object)true } : new[] { id });
                browser.Repaint();
                return true;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        public static bool IsLocked(EditorWindow browser)
        {
            if (!browser) return false;

            Resolve(browser.GetType());

            try
            {
                return _isLocked?.GetValue(browser) is bool locked && locked;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        public static void SetLocked(EditorWindow browser, bool locked)
        {
            if (!browser) return;

            Resolve(browser.GetType());

            try
            {
                _isLocked?.SetValue(browser, locked);
                browser.Repaint();
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// The folder this window is browsing. Only meaningful in the two-column layout — one column
        /// has no concept of a browsed folder, so it reports nothing there rather than guessing.
        /// </summary>
        public static string ActiveFolderPath(EditorWindow browser)
        {
            if (!browser) return null;

            Resolve(browser.GetType());

            try
            {
                return _activeFolderPath?.Invoke(browser, null) as string;
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
            Debug.LogWarning($"[HelpfulEditor] Project window control is unavailable on this Unity version. ({e.Message})");
        }
    }
}
