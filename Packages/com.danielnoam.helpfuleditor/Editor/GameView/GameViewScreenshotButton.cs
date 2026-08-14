using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// One press, one PNG, no preview and no dialog — the folder is settled in the settings so the
    /// button never has to ask. Drawn in Unity's toolbar button style and placed by the strip.
    /// </summary>
    internal sealed class GameViewScreenshotButton : GameViewToolbarItem
    {
        public const string ElementName = "helpfuleditor-gameview-screenshot-button";

        private const float FallbackWidthGuess = 30f;

        private static GUIContent _content;

        private readonly EditorWindow _gameView;
        private readonly IMGUIContainer _drawer;

        public GameViewScreenshotButton(EditorWindow gameView)
        {
            _gameView = gameView;

            name = ElementName;

            _drawer = new IMGUIContainer(OnDrawGUI) { pickingMode = PickingMode.Position };

            _drawer.style.flexGrow = 1f;
            Add(_drawer);

            RegisterCallback<PointerEnterEvent>(_ => _drawer.MarkDirtyRepaint());
            RegisterCallback<PointerLeaveEvent>(_ => _drawer.MarkDirtyRepaint());

            // Right-click is UI Toolkit's; IMGUI inside the container only ever sees the left button. It
            // has to be the trickling phase — the press lands on the container inside this element,
            // which swallows it, so a callback waiting for the way back up never runs.
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        public override float FallbackWidth => FallbackWidthGuess;

        public override float MeasureWidth()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;
            if (!settings.moduleEnabled || !settings.screenshotEnabled) return 0f;

            EnsureContent();

            return Mathf.Ceil(EditorStyles.toolbarButton.CalcSize(_content).x);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 1) return;

            GenericMenu menu = new GenericMenu();
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            // Locked while a capture is in flight: a forced-resolution shot owns the Game View's size
            // until it is done, and changing what it is capturing halfway would describe it wrongly.
            bool locked = GameViewScreenshot.Busy;

            foreach (ScreenshotFormat format in (ScreenshotFormat[])Enum.GetValues(typeof(ScreenshotFormat)))
            {
                ScreenshotFormat captured = format;

                GameViewToolbarMenu.Entry(menu, $"Format/{GameViewToolbarMenu.Label(format)}",
                    settings.screenshotFormat == format, locked, () => SetFormat(captured));
            }

            GameViewToolbarMenu.Entry(menu, "Exclude UI", settings.screenshotExcludeUi, locked, ToggleExcludeUi);
            GameViewToolbarMenu.Entry(menu, "Force Resolution", settings.screenshotForceResolution, locked, ToggleForceResolution);

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Open Screenshot Folder"), false, GameViewScreenshot.OpenFolder);
            menu.AddItem(new GUIContent("Settings…"), false, HelpfulEditorSettingsProvider.OpenGameViewSettings);

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private static void SetFormat(ScreenshotFormat format)
        {
            HelpfulEditorSettings.GameView.screenshotFormat = format;
            HelpfulEditorSettings.SaveGameView();
        }

        private static void ToggleExcludeUi()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            settings.screenshotExcludeUi = !settings.screenshotExcludeUi;
            HelpfulEditorSettings.SaveGameView();
        }

        private static void ToggleForceResolution()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            settings.screenshotForceResolution = !settings.screenshotForceResolution;
            HelpfulEditorSettings.SaveGameView();
        }

        private void OnDrawGUI()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;
            if (!settings.moduleEnabled || !settings.screenshotEnabled) return;

            EnsureContent();
            ApplyMeasuredWidth(MeasureWidth());

            Rect rect = new Rect(0f, 0f, _drawer.contentRect.width, _drawer.contentRect.height);
            if (rect.width < 1f || rect.height < 1f) return;

            // Greyed out until the view has rendered something, rather than failing on the press.
            using (new EditorGUI.DisabledScope(!GameViewScreenshot.CanCapture(_gameView)))
            {
                if (GUI.Button(rect, _content, EditorStyles.toolbarButton)) GameViewScreenshot.Capture(_gameView);
            }
        }

        private static void EnsureContent()
        {
            if (_content != null) return;

            const string tooltip = "Save a screenshot of the Game View. Right-click to open the folder.";

            _content = Icon(tooltip, "FrameCapture", "Camera Icon");
        }

        /// <summary>Tries each name in turn and falls back to the word, so a missing icon still reads.</summary>
        private static GUIContent Icon(string tooltip, params string[] iconNames)
        {
            foreach (string iconName in iconNames)
            {
                try
                {
                    GUIContent icon = EditorGUIUtility.IconContent(iconName);
                    if (icon?.image) return new GUIContent(icon.image, tooltip);
                }
                catch (Exception)
                {
                    // Falls through to the next name, and then to the text below.
                }
            }

            return new GUIContent("Snap", tooltip);
        }
    }
}
