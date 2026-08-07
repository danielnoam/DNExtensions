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

            GameViewToolbar.RegisterProvider(_ => new GameViewRulerToggle());
            GameViewToolbar.RegisterProvider(gameView => new GameViewScreenshotButton(gameView), priority: 10);
        }

        /// <summary>
        /// The guide menu, reached by right-clicking either a ruler or the toolbar's Rulers button. It
        /// lives here rather than on the overlay because the button is not the overlay's to own — the
        /// toolbar strip holds it, and it outlives any one Game View's guidelines drawer.
        /// </summary>
        public static void ShowGuideMenu()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;
            GenericMenu menu = new GenericMenu();

            // Adding while the rulers are off would drop a guide nobody can see, so the menu says no
            // rather than looking broken. Clearing stays available either way.
            if (settings.showRulers)
            {
                menu.AddItem(new GUIContent("Add Vertical Guide"), false, () => AddCentred(false));
                menu.AddItem(new GUIContent("Add Horizontal Guide"), false, () => AddCentred(true));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Add Vertical Guide"));
                menu.AddDisabledItem(new GUIContent("Add Horizontal Guide"));
            }

            if (settings.guides.Count > 0) menu.AddItem(new GUIContent("Clear All Guides"), false, ClearGuides);
            else menu.AddDisabledItem(new GUIContent("Clear All Guides"));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Settings…"), false, HelpfulEditorSettingsProvider.OpenGameViewSettings);

            menu.ShowAsContext();
        }

        private static void AddCentred(bool horizontal)
        {
            HelpfulEditorSettings.GameView.guides.Add(new GameViewGuide { isHorizontal = horizontal, normalizedPosition = 0.5f });
            HelpfulEditorSettings.SaveGameView();

            Sync();
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

            // The toolbar strip is not part of the overlay and stays put when the feature is off — its
            // items report no width and it reserves nothing, which is what makes the button come back.
            GameViewToolbar.Refresh();
        }

        public static void ClearGuides()
        {
            HelpfulEditorSettings.GameView.guides.Clear();
            HelpfulEditorSettings.SaveGameView();
            Sync();
        }
    }
}
