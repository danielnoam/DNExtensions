using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Record, and while recording, pause / stop / discard. One toolbar item rather than several,
    /// because the set of buttons changes with the state and the strip measures items rather than
    /// buttons — a group that grows and shrinks as one is what it can place.
    /// </summary>
    internal sealed class GameViewRecordButton : GameViewToolbarItem
    {
        public const string ElementName = "helpfuleditor-gameview-record-button";

        private const float IdleWidth = 30f;
        private const float PauseWidth = 58f;
        private const float StopWidth = 30f;
        private const float ActiveWidth = PauseWidth + StopWidth;

        private const float FallbackWidthGuess = 62f;

        private static readonly Color RecordColor = new Color(0.9f, 0.28f, 0.3f);

        /// <summary>Not tinted: a stop square that matched the record dot would read as still recording.</summary>
        private static readonly Color StopColor = new Color(0.85f, 0.85f, 0.85f);

        private readonly EditorWindow _gameView;
        private readonly IMGUIContainer _drawer;

        public GameViewRecordButton(EditorWindow gameView)
        {
            _gameView = gameView;

            name = ElementName;

            _drawer = new IMGUIContainer(OnDrawGUI) { pickingMode = PickingMode.Position };

            _drawer.style.flexGrow = 1f;
            Add(_drawer);

            RegisterCallback<PointerEnterEvent>(_ => _drawer.MarkDirtyRepaint());
            RegisterCallback<PointerLeaveEvent>(_ => _drawer.MarkDirtyRepaint());

            // Right-click is UI Toolkit's; IMGUI inside the container only ever sees the left button.
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        public override float FallbackWidth => FallbackWidthGuess;

        /// <summary>Whether this window is the one recording, rather than merely one of the open ones.</summary>
        private bool Active => GameViewRecording.IsRecording && GameViewRecording.Target == _gameView;

        private static bool Enabled
        {
            get
            {
                GameViewSettings settings = HelpfulEditorSettings.GameView;

                return settings.moduleEnabled && settings.recordingEnabled;
            }
        }

        public override float MeasureWidth()
        {
            if (!Enabled) return 0f;

            return Active ? ActiveWidth : IdleWidth;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 1 || !Enabled) return;

            GenericMenu menu = new GenericMenu();
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            // Everything here is read once when the encoder is created, so changing any of it mid-take
            // would describe the file wrongly rather than change it.
            foreach (RecordingMode mode in (RecordingMode[])Enum.GetValues(typeof(RecordingMode)))
            {
                RecordingMode captured = mode;
                string path = $"Mode/{GameViewRecording.Label(mode)}";

                // Offered but dead without the package, which says more than leaving it out would.
                if (mode == RecordingMode.Recorder && !GameViewRecorderBridge.Available)
                {
                    menu.AddDisabledItem(new GUIContent(path), settings.recordingMode == mode);
                    continue;
                }

                Entry(menu, path, settings.recordingMode == mode, () => SetMode(captured));
            }

            foreach (int fps in new[] { 24, 30, 60 })
            {
                int captured = fps;

                Entry(menu, $"FPS/{fps}", settings.recordingFps == fps, () => SetFps(captured));
            }

            foreach (RecordingQuality quality in (RecordingQuality[])Enum.GetValues(typeof(RecordingQuality)))
            {
                RecordingQuality captured = quality;

                Entry(menu, $"Quality/{GameViewRecording.Label(quality)}", settings.recordingQuality == quality, () => SetQuality(captured));
            }

            Entry(menu, "Force Resolution", settings.recordingForceResolution, ToggleForceResolution);

            menu.AddSeparator(string.Empty);

            // The only way to throw a take away now that the button is gone, which is the point —
            // discarding is not undoable and does not belong one stray click from Stop.
            if (Active) menu.AddItem(new GUIContent("Discard Recording"), false, GameViewRecording.Cancel);
            else menu.AddDisabledItem(new GUIContent("Discard Recording"));

            menu.AddItem(new GUIContent("Open Recordings Folder"), false, GameViewRecording.OpenFolder);
            menu.AddItem(new GUIContent("Settings…"), false, HelpfulEditorSettingsProvider.OpenGameViewSettings);

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        /// <summary>
        /// A menu entry that is greyed rather than absent while a take is running, so the settings can
        /// still be read off mid-recording — the tick still says what this one is being recorded at.
        /// </summary>
        private static void Entry(GenericMenu menu, string path, bool on, GenericMenu.MenuFunction chosen)
        {
            GUIContent label = new GUIContent(path);

            if (GameViewRecording.IsActive) menu.AddDisabledItem(label, on);
            else menu.AddItem(label, on, chosen);
        }

        private static void SetMode(RecordingMode mode)
        {
            HelpfulEditorSettings.GameView.recordingMode = mode;
            HelpfulEditorSettings.SaveGameView();
        }

        private static void SetFps(int fps)
        {
            HelpfulEditorSettings.GameView.recordingFps = fps;
            HelpfulEditorSettings.SaveGameView();
        }

        private static void SetQuality(RecordingQuality quality)
        {
            HelpfulEditorSettings.GameView.recordingQuality = quality;
            HelpfulEditorSettings.SaveGameView();
        }

        private static void ToggleForceResolution()
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            settings.recordingForceResolution = !settings.recordingForceResolution;
            HelpfulEditorSettings.SaveGameView();
        }

        private void OnDrawGUI()
        {
            if (!Enabled) return;

            ApplyMeasuredWidth(MeasureWidth());

            Rect rect = new Rect(0f, 0f, _drawer.contentRect.width, _drawer.contentRect.height);
            if (rect.width < 1f || rect.height < 1f) return;

            if (Active) DrawActive(rect);
            else DrawIdle(rect);
        }

        private void DrawIdle(Rect rect)
        {
            // Greyed rather than hidden while another Game View is recording, so it is clear the
            // button is there and why it will not go.
            using (new EditorGUI.DisabledScope(!GameViewRecording.CanRecord(_gameView)))
            {
                const string tooltip = "Record the Game View to MP4. Right-click for rate, quality and the folder.";

                // The editor's own record glyph, the one the Animation window uses. Carried as the
                // button's content rather than drawn over it, so it centres and greys with the button
                // exactly like the ruler and screenshot icons beside it.
                GUIContent icon = RecordIcon;

                GUIContent content = icon?.image
                    ? new GUIContent(icon.image, tooltip)
                    : new GUIContent(string.Empty, tooltip);

                bool pressed = GUI.Button(rect, content, EditorStyles.toolbarButton);

                // Only when the editor has no such icon, which is the one case the drawn dot is for.
                if (!content.image) DrawDot(rect);

                if (pressed) GameViewRecording.Start(_gameView);
            }
        }

        /// <summary>
        /// Pause with the running time, and stop. Split by hand rather than laid out, since this is a
        /// single IMGUI rect the toolbar handed over.
        ///
        /// Discarding is in the right-click menu rather than a third button: it is the rare choice of
        /// the two, and a one-click "throw the take away" sitting beside Stop is easy to hit by
        /// mistake and impossible to undo.
        /// </summary>
        private void DrawActive(Rect rect)
        {
            // A delegated take has no pause to offer, so the elapsed time takes the whole strip and
            // stop keeps its place at the end.
            if (GameViewRecording.IsDelegated)
            {
                DrawDelegatedActive(rect);
                return;
            }

            float scale = rect.width / ActiveWidth;

            Rect pause = new Rect(rect.x, rect.y, PauseWidth * scale, rect.height);
            Rect stop = new Rect(pause.xMax, rect.y, rect.xMax - pause.xMax, rect.height);

            bool paused = GameViewRecording.IsPaused;

            // The editor's own transport icons rather than glyphs — ❚❚ and ▶ are whatever the UI font
            // makes of them, which is never the shape Unity's toolbar uses right beside this.
            GUIContent pauseContent = new GUIContent(
                $"  {Format(GameViewRecording.Duration)}",
                paused ? "Resume recording." : "Pause recording.");

            if (GUI.Button(pause, pauseContent, EditorStyles.toolbarButton)) GameViewRecording.TogglePause();

            DrawIcon(pause, pauseContent, paused ? PlayIcon : PauseIcon);

            if (GUI.Button(stop, new GUIContent(string.Empty, "Stop recording and save it. Right-click to discard it instead."), EditorStyles.toolbarButton))
            {
                GameViewRecording.Stop();
            }

            // Drawn rather than iconed: the editor has no stop glyph, and a square is the one shape
            // that needs none — the same reason the record dot is drawn.
            DrawSquare(stop);
        }

        /// <summary>
        /// Cached because IconContent is a dictionary lookup and a GUIContent allocation, and these
        /// run on every repaint of the toolbar. Cleared on a skin change, which is the only thing that
        /// makes them wrong.
        /// </summary>
        private static GUIContent _recordIcon;
        private static GUIContent _pauseIcon;
        private static GUIContent _playIcon;
        private static bool _iconsAreProSkin;

        /// <summary>The same camera the Play From Camera toolbar button uses, so the two read as a set.</summary>
        private static GUIContent RecordIcon => Cached(ref _recordIcon, "SceneViewCamera@2x", "SceneViewCamera");
        /// <summary>
        /// Recorder is driving, so this is a readout and a stop — the dot stays lit beside the time to
        /// say the take is running, since there is no pause icon here to say it instead.
        /// </summary>
        private void DrawDelegatedActive(Rect rect)
        {
            float scale = rect.width / ActiveWidth;

            Rect elapsed = new Rect(rect.x, rect.y, PauseWidth * scale, rect.height);
            Rect stop = new Rect(elapsed.xMax, rect.y, rect.xMax - elapsed.xMax, rect.height);

            GUIContent content = new GUIContent($"  {Format(GameViewRecording.Duration)}", "Recording through Unity Recorder.");

            GUI.Label(elapsed, content, EditorStyles.toolbarButton);

            // A narrow rect at the left rather than the whole one, since DrawDot centres in whatever
            // it is given and the text is using the rest.
            DrawDot(new Rect(elapsed.x + 3f, elapsed.y, 14f, elapsed.height));

            if (GUI.Button(stop, new GUIContent(string.Empty, "Stop recording and save it."), EditorStyles.toolbarButton))
            {
                GameViewRecording.Stop();
            }

            DrawSquare(stop);
        }

        private static GUIContent PauseIcon => Cached(ref _pauseIcon, "PauseButton", "PauseButton On");
        private static GUIContent PlayIcon => Cached(ref _playIcon, "PlayButton", "PlayButton On");

        private static GUIContent Cached(ref GUIContent slot, params string[] names)
        {
            if (_iconsAreProSkin != EditorGUIUtility.isProSkin)
            {
                _iconsAreProSkin = EditorGUIUtility.isProSkin;

                _recordIcon = null;
                _pauseIcon = null;
                _playIcon = null;

                slot = null;
            }

            return slot ??= Icon(names) ?? GUIContent.none;
        }

        /// <summary>Tries the skin's own variant first, then the plain name, then gives up quietly.</summary>
        private static GUIContent Icon(params string[] names)
        {
            foreach (string name in names)
            {
                string skinned = EditorGUIUtility.isProSkin ? $"d_{name}" : name;

                foreach (string candidate in new[] { skinned, name })
                {
                    try
                    {
                        GUIContent icon = EditorGUIUtility.IconContent(candidate);
                        if (icon?.image) return icon;
                    }
                    catch (Exception)
                    {
                        // Falls through to the next name, and then to nothing.
                    }
                }
            }

            return null;
        }

        /// <summary>Placed left of the button's own text, which is what reserves the room for it.</summary>
        private static void DrawIcon(Rect rect, GUIContent content, GUIContent icon)
        {
            if (Event.current.type != EventType.Repaint || icon?.image == null) return;

            const float size = 12f;

            float textWidth = EditorStyles.toolbarButton.CalcSize(content).x;
            float textLeft = rect.x + (rect.width - textWidth) * 0.5f;

            Rect iconRect = new Rect(textLeft, rect.y + (rect.height - size) * 0.5f, size, size);

            GUI.DrawTexture(iconRect, icon.image, ScaleMode.ScaleToFit);
        }

        private static void DrawSquare(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            const float size = 8f;

            Rect square = new Rect(
                rect.x + (rect.width - size) * 0.5f,
                rect.y + (rect.height - size) * 0.5f,
                size, size);

            Color previous = GUI.color;
            GUI.color = Color.white;

            GUI.DrawTexture(square, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, StopColor, 0f, 1f);

            GUI.color = previous;
        }

        /// <summary>
        /// The red dot, drawn rather than iconed — the editor has no record glyph that reads at this
        /// size. Centred like the stop square, so the two read as a pair.
        /// </summary>
        private static void DrawDot(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            const float size = 9f;

            Rect dot = new Rect(
                rect.x + (rect.width - size) * 0.5f,
                rect.y + (rect.height - size) * 0.5f,
                size, size);

            Color previous = GUI.color;
            GUI.color = Color.white;

            GUI.DrawTexture(dot, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0f, RecordColor, 0f, size * 0.5f);

            GUI.color = previous;
        }

        private static string Format(TimeSpan elapsed)
        {
            return elapsed.TotalHours >= 1.0
                ? $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }
    }
}
