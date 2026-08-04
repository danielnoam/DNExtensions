using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Records asset selection changes into quick navigation history.
    /// </summary>
    [InitializeOnLoad]
    internal static class QuickNavigationTracker
    {
        static QuickNavigationTracker()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void OnProjectChanged()
        {
            if (QuickNavigationData.Instance.HealFavorites())
                QuickNavigationData.Instance.Save();
            QuickNavigationWindow.RefreshWindow();
        }

        private static void OnSelectionChanged()
        {
            if (!Selection.activeObject) return;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;

            QuickNavigationData.Instance.AddToHistory(guid);
            QuickNavigationWindow.RefreshWindow();
        }

        private static void OnProjectItemGUI(string guid, Rect selectionRect)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.MouseDown || e.button != 0 || !selectionRect.Contains(e.mousePosition)) return;
            if (string.IsNullOrEmpty(guid)) return;

            QuickNavigationData.Instance.AddToHistory(guid);
            QuickNavigationWindow.RefreshWindow();
        }
    }
}
