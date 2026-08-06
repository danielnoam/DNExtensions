using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Keeps a guidelines overlay attached to every open Game View. Unity's GameView type is
    /// internal, so the windows are found by reflected type rather than named.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameViewModule
    {
        private const double ScanInterval = 0.5;

        private static Type _gameViewType;
        private static double _nextScan;

        static GameViewModule()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            EditorApplication.delayCall += Sync;
        }

        private static IEnumerable<EditorWindow> EnumerateGameViews()
        {
            _gameViewType ??= typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (_gameViewType == null) yield break;

            foreach (UnityEngine.Object window in Resources.FindObjectsOfTypeAll(_gameViewType))
            {
                if (window is EditorWindow gameView) yield return gameView;
            }
        }

        private static void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextScan) return;
            _nextScan = EditorApplication.timeSinceStartup + ScanInterval;

            // Newly opened Game Views only. Rebuilding overlays that already exist would tear down
            // the element holding a pointer capture and abandon whatever drag is in progress.
            Sync(false);
        }

        /// <summary>Rebuilds every overlay. Call after a settings change, not from the scan.</summary>
        public static void Sync() => Sync(true);

        private static void Sync(bool refreshExisting)
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            // Rulers and the toolbar button come with the guides — turning the feature off takes the
            // whole overlay away rather than leaving furniture behind with nothing to place.
            bool attach = settings.moduleEnabled && settings.guidesEnabled;

            foreach (EditorWindow gameView in EnumerateGameViews())
            {
                VisualElement root = gameView.rootVisualElement;
                if (root == null) continue;

                GameViewGuidelinesDrawer existing = root.Q<GameViewGuidelinesDrawer>(GameViewGuidelinesDrawer.OverlayName);

                if (!attach)
                {
                    existing?.RemoveFromHierarchy();
                    continue;
                }

                if (existing == null) root.Add(new GameViewGuidelinesDrawer(gameView));
                else if (refreshExisting) existing.RefreshLayout();
            }
        }

        public static void ClearGuides()
        {
            HelpfulEditorSettings.GameView.guides.Clear();
            HelpfulEditorSettings.SaveGameView();
            Sync();
        }
    }
}
