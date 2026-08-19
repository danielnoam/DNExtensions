using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Project
{
    /// <summary>
    /// A "+" button on the right of the Project window's breadcrumb header that creates a folder in
    /// the folder currently being browsed. It is injected into the window's visual tree rather than
    /// drawn from the per-row callback, because that callback never fires for an empty folder —
    /// which is exactly when a new-folder button is most useful.
    ///
    /// Not undoable: AssetDatabase folder creation is a file operation outside Unity's undo stack.
    /// Deleting an unwanted empty folder afterwards is low-stakes, so nothing else guards it.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectCreateFolderButton
    {
        private const string ElementName = "helpfuleditor-new-folder";

        // Fast enough that a newly opened Project window comes up with its button already there.
        // A poll that finds nothing to do is a query per window and a lookup by name.
        private const double RefreshInterval = 0.1;

        private const string CreateMenuRoot = "Assets/Create";

        /// <summary>
        /// Right-click shortcuts, named by the leaf of the Unity create-menu item they should run.
        /// Only leaves are listed because the surrounding path moves between versions — scripting
        /// gained a Scripting submenu in Unity 6 — and the real path is looked up at click time.
        /// </summary>
        private static readonly (string Label, string[] Leaves)[] CreateEntries =
        {
            ("Folder", new[] { "Folder" }),
            ("C# Script", new[] { "MonoBehaviour Script", "C# Script" }),
            ("ScriptableObject Script", new[] { "ScriptableObject Script" }),
            ("Material", new[] { "Material" }),
            ("Scene", new[] { "Scene" }),
            ("Animator Controller", new[] { "Animator Controller" }),
            ("Animation", new[] { "Animation", "Animation Clip" }),
            ("Timeline", new[] { "Timeline" })
        };

        private static double _lastRefresh;
        private static bool _removed;

        static ProjectCreateFolderButton()
        {
            EditorApplication.update -= Refresh;
            EditorApplication.update += Refresh;
        }

        /// <summary>
        /// Skips the wait before the next poll, for the paths that have just created a window and
        /// know it needs a button. Also the way back in once the button has been switched off, since
        /// the poll stands itself down until told otherwise.
        /// </summary>
        public static void RequestRefresh()
        {
            _lastRefresh = 0.0;
            _removed = false;

            // A window created a moment ago is not in the cached scan yet, so polling immediately
            // against it would find nothing to put a button on.
            HelpfulEditorWindows.Invalidate();
        }

        private static void Refresh()
        {
            if (EditorApplication.timeSinceStartup - _lastRefresh < RefreshInterval) return;
            _lastRefresh = EditorApplication.timeSinceStartup;

            bool wanted = HelpfulEditorSettings.Project.moduleEnabled && HelpfulEditorSettings.Project.createFolderButtonEnabled;

            // A switched-off button needs one pass to clear what is already there and nothing after
            // that — the settings screen calls RequestRefresh when it comes back on. Without this
            // the poll goes on querying every Project window's visual tree ten times a second for a
            // feature nobody has enabled.
            if (!wanted && _removed) return;
            _removed = !wanted;

            foreach (EditorWindow window in HelpfulEditorWindows.AllProjectBrowsers())
            {
                if (!window || window.rootVisualElement == null) continue;

                VisualElement existing = window.rootVisualElement.Q(ElementName);

                // The breadcrumb only exists in the two-column layout; in one column the top strip is
                // the search bar and a button there would sit on top of Unity's own controls. Asked
                // of this window rather than of the Project window in general — two of them can be in
                // different layouts, and the old global check answered for whichever was found first.
                if (!wanted || !HelpfulEditorTreeReflection.IsTwoColumnLayout(window))
                {
                    existing?.RemoveFromHierarchy();
                    continue;
                }

                if (existing == null) window.rootVisualElement.Add(BuildButton());
            }
        }

        private static VisualElement BuildButton()
        {
            Button button = new Button(CreateFolder)
            {
                name = ElementName,
                tooltip = "New folder in the folder being browsed\nRight-click for other asset types"
            };

            button.AddManipulator(new ContextualMenuManipulator(BuildCreateMenu));

            button.style.position = Position.Absolute;
            button.style.top = 22f;
            button.style.right = 4f;
            button.style.width = 32f;
            button.style.height = 16f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.paddingLeft = 1f;
            button.style.paddingRight = 1f;
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;

            Texture folderIcon = HelpfulEditorGUI.LoadIcon("Folder Icon");
            if (folderIcon)
            {
                Image icon = new Image { image = folderIcon, scaleMode = ScaleMode.ScaleToFit };
                icon.style.width = 14f;
                icon.style.height = 14f;
                icon.style.flexShrink = 0f;
                button.Add(icon);
            }

            Label plus = new Label("+");
            plus.style.marginLeft = 1f;
            plus.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.Add(plus);

            return button;
        }

        private static void BuildCreateMenu(ContextualMenuPopulateEvent evt)
        {
            Dictionary<string, string> menu = EnumerateCreateMenu();

            foreach ((string label, string[] leaves) in CreateEntries)
            {
                string menuPath = ResolveMenuPath(menu, leaves);
                if (menuPath == null) continue;

                evt.menu.AppendAction(label, _ => EditorApplication.ExecuteMenuItem(menuPath));
            }
        }

        private static string ResolveMenuPath(Dictionary<string, string> menu, string[] leaves)
        {
            foreach (string leaf in leaves)
            {
                if (menu.TryGetValue(leaf, out string path)) return path;
            }

            return null;
        }

        /// <summary>
        /// Every real item under Assets/Create, keyed by its leaf name.
        ///
        /// Read from the live menu rather than assumed: most of these are not [MenuItem] attributes
        /// at all in Unity 6 — they are registered at runtime — so a hardcoded path is wrong as
        /// often as not, and Menu.GetEnabled happily reports true for paths that do not exist.
        /// Enumerating is the only way to know what this editor actually has.
        /// </summary>
        private static Dictionary<string, string> EnumerateCreateMenu()
        {
            Dictionary<string, string> byLeaf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                MethodInfo method = typeof(Menu).GetMethod("GetMenuItems",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method?.Invoke(null, new object[] { CreateMenuRoot, false, false }) is not Array items) return byLeaf;

                MemberInfo pathMember = null;

                foreach (object item in items)
                {
                    if (item == null) continue;

                    pathMember ??= FindPathMember(item.GetType());
                    if (ReadPath(pathMember, item) is not string path || path.Length <= CreateMenuRoot.Length) continue;

                    int slash = path.LastIndexOf('/');
                    string leaf = slash >= 0 ? path.Substring(slash + 1) : path;

                    // First wins, so a top-level item is preferred over a deeper one of the same name.
                    if (!byLeaf.ContainsKey(leaf)) byLeaf[leaf] = path;
                }
            }
            catch (Exception)
            {
                // Menu enumeration is internal; without it the context menu is simply empty.
            }

            return byLeaf;
        }

        private static MemberInfo FindPathMember(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo property = type.GetProperty("path", flags);
            if (property != null && property.PropertyType == typeof(string)) return property;

            return type.GetField("path", flags);
        }

        private static string ReadPath(MemberInfo member, object item)
        {
            return member switch
            {
                PropertyInfo property => property.GetValue(item) as string,
                FieldInfo field => field.GetValue(item) as string,
                _ => null
            };
        }

        /// <summary>
        /// Creates in whatever folder the Project window is showing, and enters rename mode. Runs
        /// the editor's own Folder item so it behaves exactly like creating one by hand; the path is
        /// looked up rather than assumed, since CreateFolderWithTemplates no longer exists in
        /// Unity 6 and the menu path is not a compile-time constant either.
        /// </summary>
        private static void CreateFolder()
        {
            string menuPath = ResolveMenuPath(EnumerateCreateMenu(), new[] { "Folder" });
            if (menuPath == null) return;

            EditorApplication.ExecuteMenuItem(menuPath);
        }

        /// <summary>
        /// Creates a folder inside a named folder, for callers that know their target rather than
        /// relying on which folder the editor considers active.
        ///
        /// The menu route above cannot be reused for those: ProjectWindowUtil.GetActiveFolderPath
        /// asks the Project browser and nothing else — it does not consult the selection — so an
        /// Assets/Create item fired from a folder tab would create in whatever a Project window
        /// happens to be showing. This creates the folder outright and selects it instead.
        /// </summary>
        public static void CreateFolderIn(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath)) return;

            string unique = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/New Folder");
            int slash = unique.LastIndexOf('/');
            if (slash < 0) return;

            string guid = AssetDatabase.CreateFolder(folderPath, unique.Substring(slash + 1));
            if (string.IsNullOrEmpty(guid)) return;

            Object created = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (!created) return;

            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }
    }
}
