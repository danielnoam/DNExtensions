using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Editor window for managing the local Git exclude file.
    /// </summary>
    internal class GitExcludeManager : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _fileContents = "";

        [MenuItem("Tools/DNExtensions/Git Local Exclude Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<GitExcludeManager>("Git Exclude");
            window.minSize = new Vector2(300, 380);
        }

        private void OnEnable() => RefreshFileContents();
        private void OnFocus() => RefreshFileContents();
        private void OnSelectionChange() => Repaint();

        private void OnGUI()
        {
            GUILayout.Label("Local Exclude Management", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Use this tool to add files or folders to your local Git exclude file. " +
                                    "These items will be completely ignored by Git without modifying your project's shared .gitignore file.",
                                    MessageType.Info);
            EditorGUILayout.Space();

            string excludeFilePath = GetExcludeFilePath();

            if (GUILayout.Button("Open 'exclude' File", GUILayout.Height(30)))
            {
                OpenExcludeFile(excludeFilePath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            GUILayout.Label("Current File Contents (Drag & Drop items here)", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            if (!string.IsNullOrEmpty(_fileContents))
            {
                EditorGUILayout.LabelField(_fileContents, EditorStyles.wordWrappedLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("File is empty or does not exist yet.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            HandleDragAndDrop(excludeFilePath);
        }

        private void HandleDragAndDrop(string excludeFilePath)
        {
            Rect dropArea = GUILayoutUtility.GetLastRect();
            Event currentEvent = Event.current;

            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        AddPathToExclude(draggedPath, excludeFilePath);
                    }

                    currentEvent.Use();
                }
            }
        }

        private void RefreshFileContents()
        {
            string path = GetExcludeFilePath();
            _fileContents = File.Exists(path) ? File.ReadAllText(path) : "";
            Repaint();
        }

        public static string GetExcludeFilePath()
        {
            string projectDir = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectDir, ".git", "info", "exclude");
        }

        private void OpenExcludeFile(string path)
        {
            if (!File.Exists(path))
            {
                string infoPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(infoPath)) Directory.CreateDirectory(infoPath);
                File.WriteAllText(path, "");
                RefreshFileContents();
            }

            #if UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", $"-t \"{path}\"");
            #elif UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("notepad.exe", $"\"{path}\"");
            #else
            EditorUtility.OpenWithDefaultApp(path);
            #endif
        }

        public static void AddPathToExclude(string assetPath, string excludeFilePath)
        {
            string gitPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".git");
            string infoPath = Path.Combine(gitPath, "info");

            if (!Directory.Exists(gitPath))
            {
                Debug.LogError("No .git folder found. Ensure this project is a Git repository.");
                return;
            }

            if (!Directory.Exists(infoPath)) Directory.CreateDirectory(infoPath);

            List<string> lines = new List<string>();
            if (File.Exists(excludeFilePath))
            {
                lines.AddRange(File.ReadAllLines(excludeFilePath));
            }

            bool isFolder = AssetDatabase.IsValidFolder(assetPath);
            string entryPath = (isFolder ? assetPath + "/" : assetPath).Replace("\\", "/");
            string metaPath = (assetPath + ".meta").Replace("\\", "/");

            bool wasModified = false;

            if (!lines.Contains(entryPath))
            {
                lines.Add(entryPath);
                wasModified = true;
            }

            if (!lines.Contains(metaPath))
            {
                lines.Add(metaPath);
                wasModified = true;
            }

            if (wasModified)
            {
                File.WriteAllLines(excludeFilePath, lines);
                NotifyWindowToRefresh();
            }
        }

        public static void RemovePathFromExclude(string assetPath, string excludeFilePath)
        {
            if (!File.Exists(excludeFilePath)) return;

            List<string> lines = new List<string>(File.ReadAllLines(excludeFilePath));

            bool isFolder = AssetDatabase.IsValidFolder(assetPath);
            string entryPath = (isFolder ? assetPath + "/" : assetPath).Replace("\\", "/");
            string metaPath = (assetPath + ".meta").Replace("\\", "/");

            bool wasModified = false;

            if (lines.Contains(entryPath))
            {
                lines.Remove(entryPath);
                wasModified = true;
            }

            if (lines.Contains(metaPath))
            {
                lines.Remove(metaPath);
                wasModified = true;
            }

            if (wasModified)
            {
                File.WriteAllLines(excludeFilePath, lines);
                NotifyWindowToRefresh();
            }
        }

        private static void NotifyWindowToRefresh()
        {
            if (HasOpenInstances<GitExcludeManager>())
            {
                GetWindow<GitExcludeManager>().RefreshFileContents();
            }
        }

        [MenuItem("Assets/Git/Add to Local Exclude", false, 20)]
        public static void ContextMenuAddExclude()
        {
            string excludeFilePath = GetExcludeFilePath();
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    AddPathToExclude(path, excludeFilePath);
                }
            }
            Debug.Log("<b>[Git Exclude]</b> Selected items added to local exclude.");
        }

        [MenuItem("Assets/Git/Add to Local Exclude", true)]
        public static bool ContextMenuAddExcludeValidate()
        {
            return Selection.objects.Length > 0 && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.objects[0]));
        }

        [MenuItem("Assets/Git/Remove from Local Exclude", false, 21)]
        public static void ContextMenuRemoveExclude()
        {
            string excludeFilePath = GetExcludeFilePath();
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    RemovePathFromExclude(path, excludeFilePath);
                }
            }
            Debug.Log("<b>[Git Exclude]</b> Selected items removed from local exclude.");
        }

        [MenuItem("Assets/Git/Remove from Local Exclude", true)]
        public static bool ContextMenuRemoveExcludeValidate()
        {
            return Selection.objects.Length > 0 && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.objects[0]));
        }
    }
}
