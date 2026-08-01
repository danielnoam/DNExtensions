using System;
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
        private const double RefreshInterval = 1.0;

        private static double _lastRefresh;

        static ProjectCreateFolderButton()
        {
            EditorApplication.update -= Refresh;
            EditorApplication.update += Refresh;
        }

        private static void Refresh()
        {
            if (EditorApplication.timeSinceStartup - _lastRefresh < RefreshInterval) return;
            _lastRefresh = EditorApplication.timeSinceStartup;

            bool wanted = HelpfulEditorSettings.Project.moduleEnabled && HelpfulEditorSettings.Project.createFolderButtonEnabled;

            Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
            if (browserType == null) return;

            foreach (Object candidate in Resources.FindObjectsOfTypeAll(browserType))
            {
                if (candidate is not EditorWindow window || window.rootVisualElement == null) continue;

                VisualElement existing = window.rootVisualElement.Q(ElementName);

                if (!wanted)
                {
                    existing?.RemoveFromHierarchy();
                    continue;
                }

                // The breadcrumb only exists in the two-column layout; in one column the top strip
                // is the search bar and a button there would sit on top of Unity's own controls.
                if (!HelpfulEditorTreeReflection.IsProjectTwoColumnLayout())
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
                tooltip = "New folder in the folder being browsed"
            };

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

            Texture folderIcon = EditorGUIUtility.IconContent("Folder Icon")?.image;
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

        /// <summary>Creates in whatever folder the Project window is showing, and enters rename mode.</summary>
        private static void CreateFolder()
        {
            if (TryCreateWithTemplates()) return;

            EditorApplication.ExecuteMenuItem("Assets/Create/Folder");
        }

        private static bool TryCreateWithTemplates()
        {
            try
            {
                MethodInfo method = typeof(ProjectWindowUtil).GetMethod("CreateFolderWithTemplates",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null) return false;

                method.Invoke(null, new object[] { "New Folder", Array.Empty<string>() });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
