using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Fully qualified at the use site rather than imported: 2022.3 still carries a legacy
// UnityEditor.PackageInfo, and importing the namespace makes the name ambiguous there.
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Names docked windows after what they are actually showing. Two Project tabs both read
    /// "Project" and two floating Properties windows both read "Inspector", which makes a row of them
    /// unreadable — the whole point of pinning a window to something is being able to pick it out
    /// again.
    ///
    /// Only windows pinned to something are renamed. An unlocked Project window follows the selection
    /// and has no identity of its own, so it keeps its default title.
    /// </summary>
    [InitializeOnLoad]
    internal static class HelpfulEditorWindowTitles
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string DefaultProjectTitle = "Project";

        // Fast, because a tab renaming itself half a second after you locked it reads as a glitch.
        // Affordable because a poll that finds nothing to do costs one reflected property read per
        // window and a string comparison — the expensive parts are all behind an early-out.
        private const double RefreshInterval = 0.1;

        // Rebuilt icons are cached by their source: without it every refresh would allocate another
        // texture for the same object.
        private static readonly Dictionary<int, Texture2D> ScaledIcons = new Dictionary<int, Texture2D>();

        private static double _lastRefresh;
        private static bool _warned;

        static HelpfulEditorWindowTitles()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            // The two points the dictionary is about to be lost. Everything it holds has to go with
            // it, because nothing else will ever collect it — see ReleaseScaledIcons.
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseScaledIcons;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseScaledIcons;

            EditorApplication.quitting -= ReleaseScaledIcons;
            EditorApplication.quitting += ReleaseScaledIcons;
        }

        /// <summary>
        /// Destroys the rebuilt icons.
        ///
        /// They carry DontSave so Unity will not collect them on a scene change, which is what keeps
        /// a tab icon alive between scenes — and is also what stops Unity collecting them on a domain
        /// reload. The dictionary comes back empty after one, so without this every texture it held
        /// stays in memory with nothing left pointing at it, and a session of recompiles accumulates
        /// one set per reload.
        /// </summary>
        private static void ReleaseScaledIcons()
        {
            foreach (Texture2D icon in ScaledIcons.Values)
            {
                if (icon) Object.DestroyImmediate(icon);
            }

            ScaledIcons.Clear();
        }

        /// <summary>
        /// Skips the wait before the next poll. Called by the paths that create or lock a window,
        /// which know the title is about to be wrong and need not wait to be told.
        /// </summary>
        public static void RequestRefresh() => _lastRefresh = 0.0;

        /// <summary>
        /// Polled rather than driven by an event: locking a window, and changing the folder a locked
        /// window shows, both happen without raising anything the suite can subscribe to.
        /// </summary>
        private static void OnUpdate()
        {
            if (!HelpfulEditorSettings.Project.windowTitlesEnabled) return;

            // vTabs renames the same windows from its own loop. Both writing titleContent would
            // flip-flop, so the one that can tell the other is there is the one that yields.
            if (HelpfulEditorPlugins.VTabsActive) return;

            if (EditorApplication.timeSinceStartup - _lastRefresh < RefreshInterval) return;
            _lastRefresh = EditorApplication.timeSinceStartup;

            foreach (EditorWindow browser in HelpfulEditorWindows.AllProjectBrowsers()) UpdateBrowser(browser);

            foreach (EditorWindow inspector in HelpfulEditorWindows.AllInspectors()) UpdateInspector(inspector);
        }

        private static void UpdateBrowser(EditorWindow browser)
        {
            if (!browser) return;

            try
            {
                if (!HelpfulEditorProjectWindow.IsLocked(browser))
                {
                    // Compared before the icon is fetched: this is the steady state for every
                    // unlocked window, and it runs several times a second.
                    if (browser.titleContent.text == DefaultProjectTitle) return;

                    Retitle(browser, DefaultProjectTitle, EditorGUIUtility.FindTexture("Project"));
                    return;
                }

                string path = HelpfulEditorProjectWindow.ActiveFolderPath(browser);
                if (string.IsNullOrEmpty(path)) return;

                string label = FolderLabel(path);
                if (browser.titleContent.text == label && browser.titleContent.image) return;

                Texture icon = AssetDatabase.GetCachedIcon(path);

                Retitle(browser, label, icon ? icon : EditorGUIUtility.FindTexture("Folder Icon"));
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private static void UpdateInspector(EditorWindow inspector)
        {
            if (!inspector) return;

            try
            {
                // Only a floating Properties window has an object of its own. The main Inspector
                // follows the selection, and renaming it every time that changed would be noise.
                if (GetMember(inspector, "m_InspectedObject") is not Object target || !target) return;

                // Fetching the thumbnail is the costly half, so the name is what decides whether
                // there is anything to do at all.
                string label = ObjectLabel(target);
                if (inspector.titleContent.text == label && inspector.titleContent.image) return;

                Retitle(inspector, label, ScaledIcon(AssetPreview.GetMiniThumbnail(target)));
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private static void Retitle(EditorWindow window, string title, Texture icon)
        {
            if (window.titleContent.text == title && window.titleContent.image == icon) return;

            window.titleContent = new GUIContent(title, icon);

            // Without this the tab keeps drawing the label it cached, which reads as the rename
            // silently not having worked.
            HelpfulEditorDockArea.ClearTitleCache();

            window.Repaint();
        }

        /// <summary>A package folder's reverse-DNS name says nothing, so its display name is used instead.</summary>
        private static string FolderLabel(string path)
        {
            int slash = path.LastIndexOf('/');
            string name = slash >= 0 ? path.Substring(slash + 1) : path;

            if (!name.StartsWith("com.", StringComparison.Ordinal)) return name;

            PackageInfo package = PackageInfo.FindForAssetPath(path);
            return package != null ? package.displayName : name;
        }

        private static string ObjectLabel(Object target)
        {
            // A component's own name is its GameObject's, which tells two component windows apart by
            // nothing at all.
            if (target is Component component) return component.GetType().Name;

            return target.name;
        }

        /// <summary>
        /// A tab icon is drawn at 16 points, and a thumbnail arrives without the scale factor that
        /// says how many pixels that is — so a 32px icon is drawn at double size unless it is copied
        /// onto a texture that carries the right one.
        /// </summary>
        private static Texture ScaledIcon(Texture2D source)
        {
            if (!source) return null;

            int key = source.GetHashCode();
            if (ScaledIcons.TryGetValue(key, out Texture2D cached) && cached) return cached;

            try
            {
                PropertyInfo pixelsPerPoint = typeof(Texture2D).GetProperty("pixelsPerPoint", AnyInstance);
                if (pixelsPerPoint == null || !pixelsPerPoint.CanWrite) return source;

                // Hidden as well as unsaved: these are an implementation detail of a tab label, and
                // have no business turning up in an object picker or a search. Destroyed explicitly
                // by ReleaseScaledIcons, since neither flag makes Unity collect them.
                Texture2D scaled = new Texture2D(source.width, source.height, source.format, source.mipmapCount, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                Graphics.CopyTexture(source, scaled);
                pixelsPerPoint.SetValue(scaled, Mathf.Max(1, Mathf.RoundToInt(source.width / 16f)));

                ScaledIcons[key] = scaled;
                return scaled;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return source;
            }
        }

        private static object GetMember(object instance, string memberName)
        {
            if (instance == null) return null;

            Type type = instance.GetType();

            PropertyInfo property = type.GetProperty(memberName, AnyInstance);
            if (property != null) return property.GetValue(instance);

            return type.GetField(memberName, AnyInstance)?.GetValue(instance);
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Window titles cannot be customised on this Unity version. ({e.Message})");
        }
    }
}
