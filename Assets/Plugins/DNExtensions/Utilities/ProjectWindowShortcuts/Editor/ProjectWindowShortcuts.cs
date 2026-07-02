using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.ProjectWindowShortcuts
{
    /// <summary>
    /// Ctrl+Shift+Click an asset in the Project window to open its Properties preview window.
    /// Ctrl+R while the Project window is focused reveals the selected asset(s) in Explorer/Finder instead of refreshing.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectWindowShortcutsTool
    {
        private static readonly Type ProjectBrowserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");

        static ProjectWindowShortcutsTool()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            HookGlobalEventHandler();
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (!ProjectWindowShortcutsSettings.Instance.PreviewShortcutEnabled) return;

            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !e.control || !e.shift) return;
            if (!selectionRect.Contains(e.mousePosition)) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!asset) return;

            OpenPropertiesWindow(asset);
            e.Use();
        }

        private static void OpenPropertiesWindow(Object asset)
        {
            var previousSelection = Selection.objects;
            Selection.activeObject = asset;
            EditorApplication.ExecuteMenuItem("Assets/Properties...");
            Selection.objects = previousSelection;
        }

        private static void HookGlobalEventHandler()
        {
            var field = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null) return;

            var current = (EditorApplication.CallbackFunction)field.GetValue(null);
            field.SetValue(null, OnGlobalEvent + (current - OnGlobalEvent));
        }

        private static void OnGlobalEvent()
        {
            if (!ProjectWindowShortcutsSettings.Instance.RevealShortcutEnabled) return;

            var e = Event.current;
            if (e == null || e.type != EventType.KeyDown || !e.control || e.keyCode != KeyCode.R) return;
            if (ProjectBrowserType == null) return;
            if (EditorWindow.mouseOverWindow is not { } hoveredWindow) return;
            if (hoveredWindow.GetType() != ProjectBrowserType) return;

            RevealSelectionInExplorer();
            e.Use();
        }

        private static void RevealSelectionInExplorer()
        {
            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                EditorUtility.RevealInFinder(path);
            }
        }
    }
}
