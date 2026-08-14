using System;
using System.Globalization;
using System.IO;

using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Records what the Game View is showing to an MP4, through Unity's own MediaEncoder.
    ///
    /// Frames are taken from the window's render target, the same source the screenshot uses and for
    /// the same reason: it is the game's own resolution rather than the window's, and it is readable
    /// while the editor is idle or paused instead of only at the end of a rendered frame.
    ///
    /// Written to a temporary file and moved into place when the recording ends, so nothing has to be
    /// decided up front and a cancelled take leaves nothing behind.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameViewRecording
    {
        private const string DefaultFolder = "Recordings";

        private const uint BitRateLow = 5000000u;
        private const uint BitRateMedium = 10000000u;
        private const uint BitRateHigh = 20000000u;
        private const uint BitRateUltra = 40000000u;

        /// <summary>Two seconds between keyframes, which is the usual trade for a screen capture.</summary>
        private const int GopSizeSeconds = 2;

        private const string CaptureSizeName = "Helpful Editor Recording";

        private const int MinResolution = 1;
        private const int MaxResolution = 8192;

        /// <summary>How long to wait for the resized view to draw before giving up on starting.</summary>
        private const int MaxWaitFrames = 120;

        /// <summary>Frames at the right size before the first one is encoded, so a cleared target is not frame one.</summary>
        private const int SettleFrames = 2;

        private static MediaEncoder _encoder;
        private static RenderTexture _readback;
        private static Texture2D _frame;

        private static EditorWindow _gameView;
        private static string _temporaryPath;

        private static int _width;
        private static int _height;
        private static int _fps;

        private static long _frameCount;
        private static double _accumulatedDuration;
        private static double _segmentStart;

        private static bool _warned;

        private static bool _starting;
        private static int _waitFrames;
        private static int _settledFrames;

        private static object _sizeGroup;
        private static int _originalSizeIndex = -1;
        private static int _customSizeIndex = -1;

        /// <summary>Set when a take is delegated, so Stop knows which recorder it is stopping.</summary>
        private static bool _delegated;

        /// <summary>Whether Recorder has been seen recording, which is what makes a later "not recording" mean "finished".</summary>
        private static bool _delegatedConfirmed;

        /// <summary>
        /// Survives the domain reload that entering play mode causes, which a static field would not —
        /// the request to record is made before the reload and acted on after it.
        /// </summary>
        private const string PendingStartKey = "DNExtensions.HelpfulEditor.Recording.PendingPlayStart";

        private static int _awaitingPlayFrames = -1;

        static GameViewRecording()
        {
            EditorApplication.update -= Pump;
            EditorApplication.update += Pump;

            // A reload while recording would drop the encoder mid-file and strand the temp file, with
            // nothing left holding its path.
            AssemblyReloadEvents.beforeAssemblyReload -= CancelForReload;
            AssemblyReloadEvents.beforeAssemblyReload += CancelForReload;

            EditorApplication.quitting -= CancelForReload;
            EditorApplication.quitting += CancelForReload;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// Picks up a take that asked for play mode before it had one. Entering play mode reloads the
        /// domain, so the request arrives through SessionState rather than in memory.
        /// </summary>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (!SessionState.GetBool(PendingStartKey, false)) return;

            SessionState.SetBool(PendingStartKey, false);

            // Not started here: play mode has begun but the first frame has not been drawn, so there
            // is no render target to size the recording from yet. The pump waits for one.
            _awaitingPlayFrames = MaxWaitFrames;
        }

        /// <summary>
        /// Tries to start the deferred take once the game has produced something to record. Gives up
        /// quietly after the same wait a forced resize gets — by then something else is wrong.
        /// </summary>
        private static void PumpAwaitingPlay()
        {
            if (!EditorApplication.isPlaying)
            {
                _awaitingPlayFrames = -1;
                return;
            }

            EditorWindow gameView = GameViewModule.FirstGameView();

            if (gameView && CanRecord(gameView))
            {
                _awaitingPlayFrames = -1;
                Start(gameView);

                return;
            }

            if (--_awaitingPlayFrames > 0) return;

            _awaitingPlayFrames = -1;
            Debug.LogWarning("[HelpfulEditor] Play mode started but the Game View never produced a frame, so no recording was started.");
        }

        public static bool IsRecording => _encoder != null || _delegated;
        public static bool IsPaused { get; private set; }

        /// <summary>
        /// Whether the take is Unity Recorder's. It has no pause, and its elapsed time is the clock
        /// rather than a frame count, so the toolbar asks before offering either.
        /// </summary>
        public static bool IsDelegated => _delegated;

        /// <summary>Resizing the view and waiting for it to settle, before the first frame is encoded.</summary>
        public static bool IsStarting => _starting;

        /// <summary>Either state counts as busy: both own the Game View and only one take runs at a time.</summary>
        public static bool IsActive => IsRecording || _starting;

        /// <summary>The window being recorded, so a second Game View shows its button idle rather than active.</summary>
        public static EditorWindow Target => _gameView;

        /// <summary>Whether starting would have anything to record, used to grey the button out.</summary>
        public static bool CanRecord(EditorWindow gameView)
        {
            if (IsActive || GameViewScreenshot.Busy) return false;

            RenderTexture target = GameViewScreenshot.GetRenderTexture(gameView);

            return target && target.IsCreated();
        }

        /// <summary>
        /// Elapsed video time. Ours is the frame count, which is exact; a delegated take is timed off
        /// the clock, since Recorder's frame count is not ours to read.
        /// </summary>
        public static TimeSpan Duration
        {
            get
            {
                if (_delegated) return TimeSpan.FromSeconds(ActiveDuration(EditorApplication.timeSinceStartup));

                return TimeSpan.FromSeconds(_fps > 0 ? _frameCount / (double)_fps : 0.0);
            }
        }

        /// <summary>
        /// Begins a take. With a forced resolution this only asks for the resize — the encoder is
        /// opened once the view has actually reached that size, since its frame size is fixed at
        /// construction and a frame of the wrong size cannot be fed to it afterwards.
        /// </summary>
        public static void Start(EditorWindow gameView)
        {
            if (!CanRecord(gameView)) return;

            GameViewSettings settings = HelpfulEditorSettings.GameView;

            if (settings.recordingMode == RecordingMode.Recorder)
            {
                StartDelegated(gameView, settings);
                return;
            }

            if (!settings.recordingForceResolution)
            {
                RenderTexture source = GameViewScreenshot.GetRenderTexture(gameView);

                BeginEncoding(gameView, source.width, source.height);
                return;
            }

            int width = Mathf.Clamp(settings.recordingResolution.x, MinResolution, MaxResolution);
            int height = Mathf.Clamp(settings.recordingResolution.y, MinResolution, MaxResolution);

            if (!GameViewSizeApi.Available)
            {
                Debug.LogWarning("[HelpfulEditor] The Game View size API could not be reached, so the recording is at the view's own resolution.");

                RenderTexture source = GameViewScreenshot.GetRenderTexture(gameView);
                BeginEncoding(gameView, source.width, source.height);

                return;
            }

            object group = GameViewSizeApi.CurrentGroup();
            object size = GameViewSizeApi.CreateFixedSize(width, height, CaptureSizeName);

            if (group == null || size == null)
            {
                RenderTexture source = GameViewScreenshot.GetRenderTexture(gameView);
                BeginEncoding(gameView, source.width, source.height);

                return;
            }

            _gameView = gameView;
            _width = width;
            _height = height;

            _sizeGroup = group;
            _originalSizeIndex = GameViewSizeApi.GetSelectedIndex(gameView);

            GameViewSizeApi.AddCustomSize(group, size);
            _customSizeIndex = GameViewSizeApi.GetTotalCount(group) - 1;

            GameViewSizeApi.Select(gameView, _customSizeIndex);

            _starting = true;
            _waitFrames = 0;
            _settledFrames = 0;

            gameView.Repaint();
        }

        /// <summary>
        /// Hands the take to Unity Recorder. It brings its own frame driver and audio, so none of the
        /// pumping below applies — this only picks the size and the destination and gets out of the way.
        /// </summary>
        private static void StartDelegated(EditorWindow gameView, GameViewSettings settings)
        {
            if (!GameViewRecorderBridge.Available)
            {
                Debug.LogWarning("[HelpfulEditor] Recorder mode needs the Unity Recorder package — install it, or switch the mode to Real Time.");
                return;
            }

            // Recorder records the running game, so pressing record out of play mode starts the game
            // and picks the take up on the other side rather than refusing.
            if (!EditorApplication.isPlaying)
            {
                SessionState.SetBool(PendingStartKey, true);
                EditorApplication.EnterPlaymode();

                return;
            }

            RenderTexture source = GameViewScreenshot.GetRenderTexture(gameView);

            int width = settings.recordingForceResolution
                ? Mathf.Clamp(settings.recordingResolution.x, MinResolution, MaxResolution)
                : source.width;

            int height = settings.recordingForceResolution
                ? Mathf.Clamp(settings.recordingResolution.y, MinResolution, MaxResolution)
                : source.height;

            string folder = ResolveFolder();
            Directory.CreateDirectory(folder);

            // Recorder appends the extension its codec needs, so the path it is given carries none.
            string path = Path.Combine(folder, BaseName(width, height));

            _fps = Mathf.Clamp(settings.recordingFps, 1, 240);

            if (!GameViewRecorderBridge.Start(width, height, _fps, settings.recordingQuality, path)) return;

            _gameView = gameView;
            _delegated = true;
            _segmentStart = EditorApplication.timeSinceStartup;
            _accumulatedDuration = 0.0;
        }

        /// <summary>Waits for the view to actually reach the forced size, then opens the encoder on it.</summary>
        private static void PumpStart()
        {
            if (!_gameView)
            {
                RestoreSize();
                _starting = false;

                return;
            }

            _waitFrames++;

            RenderTexture target = GameViewScreenshot.GetRenderTexture(_gameView);
            bool sized = target && target.IsCreated() && target.width == _width && target.height == _height;

            _settledFrames = sized ? _settledFrames + 1 : 0;

            if (_settledFrames >= SettleFrames)
            {
                EditorWindow gameView = _gameView;
                int width = _width;
                int height = _height;

                _starting = false;

                // The size stays forced for the whole take, so it is deliberately not restored here.
                BeginEncoding(gameView, width, height);

                return;
            }

            if (_waitFrames > MaxWaitFrames)
            {
                Debug.LogWarning($"[HelpfulEditor] The Game View did not reach {_width}×{_height} in time, so no recording was started.");

                _starting = false;
                RestoreSize();

                return;
            }

            _gameView.Repaint();
        }

        private static void BeginEncoding(EditorWindow gameView, int width, int height)
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            _gameView = gameView;
            _width = width;
            _height = height;
            _fps = Mathf.Clamp(settings.recordingFps, 1, 240);

            _temporaryPath = Path.Combine(Path.GetTempPath(), $"HelpfulEditor Recording {Guid.NewGuid():N}.mp4");

            try
            {
                VideoTrackEncoderAttributes video = new VideoTrackEncoderAttributes(new H264EncoderAttributes
                {
                    gopSize = (uint)(_fps * GopSizeSeconds),
                    numConsecutiveBFrames = 2,
                    profile = VideoEncodingProfile.H264High
                })
                {
                    frameRate = new MediaRational(_fps),
                    width = (uint)_width,
                    height = (uint)_height,
                    includeAlpha = false,
                    targetBitRate = BitRateFor(settings.recordingQuality)
                };

                _encoder = new MediaEncoder(_temporaryPath, video);

                _readback = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    name = "Game View Recording Readback",
                    hideFlags = HideFlags.HideAndDontSave
                };

                if (!_readback.Create()) throw new InvalidOperationException("The readback target could not be created.");

                _frame = new Texture2D(_width, _height, TextureFormat.RGBA32, false, false)
                {
                    name = "Game View Recording Frame",
                    hideFlags = HideFlags.HideAndDontSave
                };

                _frameCount = 0;
                _accumulatedDuration = 0.0;
                _segmentStart = EditorApplication.timeSinceStartup;
                IsPaused = false;

                CaptureFrame();
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not start recording: {e.Message}");
                Release(discard: true);
            }
        }

        /// <summary>Ours only — Recorder has no pause, and faking one by dropping frames would lie about the length.</summary>
        public static void TogglePause()
        {
            if (!IsRecording || _delegated) return;

            double now = EditorApplication.timeSinceStartup;

            if (IsPaused)
            {
                _segmentStart = now;
                IsPaused = false;

                return;
            }

            // Caught up before pausing, so the paused moment is the one the video ends on rather than
            // whatever was last drawn some fraction of a second earlier.
            _accumulatedDuration = ActiveDuration(now);
            IsPaused = true;

            TrySync(_accumulatedDuration);
        }

        public static void Stop()
        {
            if (!IsRecording) return;

            // Recorder writes straight to the destination and reports the file itself, so there is
            // nothing here to flush, move or announce.
            if (_delegated)
            {
                GameViewRecorderBridge.Stop();
                Release(discard: false);

                return;
            }

            if (!IsPaused)
            {
                _accumulatedDuration = ActiveDuration(EditorApplication.timeSinceStartup);
                IsPaused = true;
            }

            TrySync(_accumulatedDuration);

            string temporary = _temporaryPath;
            int width = _width;
            int height = _height;

            // Disposed before the move: the encoder writes the MP4's index on the way out, and the
            // file is not playable — or movable on Windows — until it has let go.
            Release(discard: false);

            Deliver(temporary, width, height);
        }

        public static void Cancel()
        {
            if (!IsActive) return;

            Release(discard: true);
        }

        /// <summary>
        /// Keeps the video's length honest. Rather than one frame per editor tick — which are as
        /// irregular as the editor's workload — the count that *should* exist by now is worked out
        /// from elapsed time, and the shortfall is made up by repeating the last frame. A stutter
        /// then shows as a held frame instead of the whole recording running fast.
        /// </summary>
        private static void Pump()
        {
            if (_awaitingPlayFrames > 0)
            {
                PumpAwaitingPlay();
                return;
            }

            if (_starting)
            {
                PumpStart();
                return;
            }

            if (!IsRecording || IsPaused) return;

            // Recorder captures its own frames and holds no encoder of ours, so none of the pumping
            // below applies to it — running it anyway is what tried to encode through a null.
            if (_delegated)
            {
                PumpDelegated();
                return;
            }

            // A recording outlives its window closing, but there is nothing left to read from.
            if (!_gameView)
            {
                Debug.LogWarning("[HelpfulEditor] The Game View being recorded was closed, so the recording was saved where it left off.");
                Stop();
                return;
            }

            TrySync(ActiveDuration(EditorApplication.timeSinceStartup));

            // Both halves of why this is here: an idle editor does not redraw the Game View, so its
            // render target would hold the same picture for the whole take — and the toolbar's
            // elapsed time is drawn by that same repaint.
            if (_gameView) _gameView.Repaint();
        }

        /// <summary>
        /// All that is left to do for a delegated take: notice when Recorder has ended it on its own —
        /// leaving play mode being the usual reason — and keep the elapsed time on the toolbar moving.
        /// </summary>
        private static void PumpDelegated()
        {
            if (GameViewRecorderBridge.IsRecording)
            {
                _delegatedConfirmed = true;

                if (_gameView) _gameView.Repaint();
                return;
            }

            // Not started yet is not the same as finished. A session only reports itself recording
            // once its first frame has gone through, a tick or two after StartRecording returned —
            // and letting go here meant the take was abandoned without ever being stopped, which is
            // exactly when Recorder writes the file.
            if (!_delegatedConfirmed) return;

            // Stopped explicitly rather than just dropped, so the file gets written.
            GameViewRecorderBridge.Stop();
            Release(discard: false);
        }

        private static void TrySync(double activeDuration)
        {
            try
            {
                Sync(activeDuration);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Recording stopped: {e.Message}");
                Release(discard: true);
            }
        }

        private static void Sync(double activeDuration)
        {
            long required = Math.Max(1L, (long)Math.Round(activeDuration * _fps, MidpointRounding.AwayFromZero));
            long missing = required - _frameCount;

            if (missing <= 0) return;

            // All but the newest are the frame already in hand, so only one readback happens per pump
            // however far behind the clock has got.
            for (long i = 1; i < missing; i++) AppendFrame();

            CaptureFrame();
        }

        private static void CaptureFrame()
        {
            RenderTexture source = GameViewScreenshot.GetRenderTexture(_gameView);

            if (!source || !source.IsCreated())
            {
                // Not an error: the view drops its target while resizing, so the frame in hand stands
                // in until it comes back.
                AppendFrame();
                return;
            }

            RenderTexture previous = RenderTexture.active;

            try
            {
                // Flipped in the blit rather than on the pixels afterwards — this runs per frame,
                // where the screenshot's row-swap runs once.
                Graphics.Blit(source, _readback, new Vector2(1f, -1f), new Vector2(0f, 1f));

                RenderTexture.active = _readback;

                _frame.ReadPixels(new Rect(0f, 0f, _width, _height), 0, 0, false);
                _frame.Apply(false, false);

                AppendFrame();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static void AppendFrame()
        {
            if (!_encoder.AddFrame(_frame)) throw new InvalidOperationException("The encoder rejected a frame.");

            _frameCount++;
        }

        private static double ActiveDuration(double now)
        {
            return IsPaused ? _accumulatedDuration : _accumulatedDuration + (now - _segmentStart);
        }

        /// <summary>Moves the finished file into the recordings folder under a name that says what it holds.</summary>
        private static void Deliver(string temporary, int width, int height)
        {
            if (string.IsNullOrEmpty(temporary) || !File.Exists(temporary)) return;

            try
            {
                string folder = ResolveFolder();
                Directory.CreateDirectory(folder);

                string path = NextFreePath(folder, width, height);

                File.Move(temporary, path);
                Reveal(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not save the recording: {e.Message}. It is still at {temporary}");
            }
        }

        private static void CancelForReload()
        {
            if (!IsActive) return;

            // Also the one thing that must not be skipped: a forced size left selected would outlive
            // the reload, with the temporary entry still in the view's size list and nothing left
            // holding its index.
            if (IsRecording) Debug.LogWarning("[HelpfulEditor] The recording was discarded because the editor reloaded its scripts.");

            Release(discard: true);
        }

        /// <summary>
        /// Puts the Game View back on the size it was showing, and takes the temporary entry back out
        /// of its size list. Safe to call when nothing was forced, which is the common case.
        /// </summary>
        private static void RestoreSize()
        {
            if (_customSizeIndex < 0)
            {
                _sizeGroup = null;
                _originalSizeIndex = -1;

                return;
            }

            if (_gameView && _originalSizeIndex >= 0) GameViewSizeApi.Select(_gameView, _originalSizeIndex);

            GameViewSizeApi.RemoveCustomSize(_sizeGroup, _customSizeIndex);

            if (_gameView) _gameView.Repaint();

            _sizeGroup = null;
            _originalSizeIndex = -1;
            _customSizeIndex = -1;
        }

        private static void Release(bool discard)
        {
            string temporary = _temporaryPath;

            // Before the window reference goes, since putting the size back needs it.
            RestoreSize();

            // A delegated take has its own teardown; this only lets go of the state that tracked it.
            if (_delegated && discard) GameViewRecorderBridge.Stop();

            _delegated = false;
            _delegatedConfirmed = false;

            IsPaused = false;
            _starting = false;
            _gameView = null;
            _temporaryPath = null;
            _frameCount = 0;

            try
            {
                _encoder?.Dispose();
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
            finally
            {
                _encoder = null;

                if (_readback) Object.DestroyImmediate(_readback);
                if (_frame) Object.DestroyImmediate(_frame);

                _readback = null;
                _frame = null;

                if (discard && !string.IsNullOrEmpty(temporary) && File.Exists(temporary))
                {
                    try
                    {
                        File.Delete(temporary);
                    }
                    catch (Exception e)
                    {
                        WarnOnce(e);
                    }
                }
            }
        }

        private static uint BitRateFor(RecordingQuality quality)
        {
            return quality switch
            {
                RecordingQuality.Low => BitRateLow,
                RecordingQuality.High => BitRateHigh,
                RecordingQuality.Ultra => BitRateUltra,
                _ => BitRateMedium
            };
        }


        public static string ResolveFolder()
        {
            string folder = HelpfulEditorSettings.GameView.recordingFolder;
            if (string.IsNullOrWhiteSpace(folder)) folder = DefaultFolder;

            return Path.IsPathRooted(folder) ? folder : Path.Combine(GameViewScreenshot.ProjectRoot, folder);
        }

        public static void OpenFolder()
        {
            string folder = ResolveFolder();

            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }

        /// <summary>
        /// Named the same way captures are, so the two folders read alike. Invariant so the stamp is
        /// the Gregorian date on every machine.
        /// </summary>
        /// <summary>Shared with the delegated path, which needs the name without an extension on it.</summary>
        private static string BaseName(int width, int height)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);

            return $"GameView {stamp} {width}x{height}";
        }

        private static string NextFreePath(string folder, int width, int height)
        {
            string name = BaseName(width, height);

            string path = Path.Combine(folder, $"{name}.mp4");

            for (int i = 2; File.Exists(path); i++)
            {
                path = Path.Combine(folder, $"{name} ({i}).mp4");
            }

            return path;
        }

        private static void Reveal(string path)
        {
            string relative = FileUtil.GetProjectRelativePath(path.Replace('\\', '/'));

            if (string.IsNullOrEmpty(relative))
            {
                Debug.Log($"[HelpfulEditor] Saved recording to {path}");
                return;
            }

            AssetDatabase.ImportAsset(relative);

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(relative);
            if (asset) EditorGUIUtility.PingObject(asset);

            Debug.Log($"[HelpfulEditor] Saved recording to {relative}", asset);
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Cleaning up after a recording did not go cleanly. ({e.Message})");
        }
    }
}
