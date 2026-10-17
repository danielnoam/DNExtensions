using System;
using System.Globalization;
using System.IO;
using DNExtensions.HelpfulEditor.GameView;
using Unity.Pipeline.Commands;
using UnityEditor;

namespace DNExtensions.HelpfulEditor.MCP
{
    /// <summary>
    /// Lets an agent record gameplay through the Helpful Editor Game View recorder — the same one the
    /// toolbar's record button drives, with the Game View settings as the defaults. Takes go to the
    /// project's Temp folder unless told otherwise, so they are never imported or committed.
    ///
    /// Recording runs on the editor's own update after a command returns, so a take is started by one
    /// command and finished by another, or by its own duration, with the status command in between.
    /// </summary>
    internal static class GameViewRecordingMcpCommands
    {
        private const string DefaultFolder = "Temp/DNExtensionsMCP";

        [CliCommand("dn_start_recording", "Start recording the Game View to an MP4. Options left at their defaults follow the Helpful Editor Game View settings. With play_mode the game is started first and recording begins on its first frame. With duration the take stops by itself; otherwise call dn_stop_recording. Check progress with dn_recording_status")]
        private static string StartRecording(
            [CliArg("width", "Record at this width; give height too (0 keeps the settings' choice)")] int width = 0,
            [CliArg("height", "Record at this height; give width too (0 keeps the settings' choice)")] int height = 0,
            [CliArg("fps", "Frames per second (0 keeps the settings' rate)")] int fps = 0,
            [CliArg("quality", "low, medium, high or ultra (empty keeps the settings' quality)")] string quality = "",
            [CliArg("mode", "realtime (no audio) or recorder (Unity Recorder, with audio, play mode only). Empty keeps the settings' mode")] string mode = "",
            [CliArg("duration", "Stop by itself after this many seconds (0 records until dn_stop_recording)")] float duration = 0f,
            [CliArg("play_mode", "Enter play mode first and start recording on its first frame")] bool playMode = false,
            [CliArg("exit_play_mode", "Leave play mode when a take with a duration stops by itself")] bool exitPlayMode = false,
            [CliArg("folder", "Where to save the MP4, absolute or project-relative. Defaults to Temp/DNExtensionsMCP")] string folder = "")
        {
            if (GameViewRecording.IsActive || GameViewRecording.IsAwaitingPlayMode)
            {
                throw new InvalidOperationException("A recording is already running or about to start. Stop it with dn_stop_recording first.");
            }

            if ((width > 0) != (height > 0))
            {
                throw new ArgumentException("Give both width and height to force a resolution, or neither.");
            }

            GameViewRecordingOptions options = new GameViewRecordingOptions
            {
                width = width,
                height = height,
                fps = fps,
                quality = ParseQuality(quality),
                mode = ParseMode(mode),
                folder = string.IsNullOrWhiteSpace(folder) ? DefaultFolder : folder,
                maxDuration = Math.Max(0f, duration),
                exitPlayModeOnStop = exitPlayMode
            };

            if (playMode && !EditorApplication.isPlaying)
            {
                GameViewRecording.StartInPlayMode(options);

                return $"Entering play mode; recording starts on the game's first frame{DurationNote(duration)}. " +
                       "The editor reloads its scripts on the way in, so wait a moment before calling dn_recording_status.";
            }

            EditorWindow gameView = GameViewModule.FirstGameView();

            if (!gameView)
            {
                throw new InvalidOperationException("No Game View is open, so there is nothing to record.");
            }

            if (!GameViewRecording.CanRecord(gameView))
            {
                throw new InvalidOperationException("The Game View has not drawn a frame yet, or a screenshot is being taken. Try again in a moment.");
            }

            GameViewRecording.Start(gameView, options);

            if (GameViewRecording.IsAwaitingPlayMode)
            {
                return $"Recorder mode records the running game, so play mode is being entered; recording starts on its first frame{DurationNote(duration)}.";
            }

            if (!GameViewRecording.IsActive)
            {
                throw new InvalidOperationException("The recording could not be started. The Unity console has the reason.");
            }

            return GameViewRecording.IsStarting
                ? $"Resizing the Game View to {width}x{height}; recording starts once it has redrawn at that size{DurationNote(duration)}."
                : $"Recording {Describe()}{DurationNote(duration)}.";
        }

        [CliCommand("dn_stop_recording", "Stop the Game View recording and return the absolute path of the saved video")]
        private static string StopRecording(
            [CliArg("exit_play_mode", "Leave play mode after stopping")] bool exitPlayMode = false)
        {
            if (!GameViewRecording.IsActive)
            {
                if (GameViewRecording.IsAwaitingPlayMode)
                {
                    throw new InvalidOperationException("The take is still waiting for play mode to start. Try again in a moment.");
                }

                string last = GameViewRecording.LastSavedPath;

                return string.IsNullOrEmpty(last)
                    ? "Nothing is recording, and no take has been saved this session."
                    : $"Nothing is recording. The last take was saved to {last}";
            }

            bool delegated = GameViewRecording.IsDelegated;
            string length = FormatDuration(GameViewRecording.Duration);
            string previous = GameViewRecording.LastSavedPath;

            GameViewRecording.Stop();

            if (exitPlayMode && EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            string path = GameViewRecording.LastSavedPath;

            // Still the previous take's path means this one has not reached the disk yet.
            if (!string.IsNullOrEmpty(path) && path != previous)
            {
                return $"{path} ({length})";
            }

            return delegated
                ? $"Stopped ({length}). Unity Recorder is still writing the file; dn_recording_status reports its path once it is on disk."
                : "Stopped, but the video could not be saved. The Unity console has the reason.";
        }

        [CliCommand("dn_recording_status", "Report whether the Game View is recording, for how long, at what size, and where the last take was saved")]
        private static string RecordingStatus()
        {
            string playState = EditorApplication.isPlaying ? "play mode" : "edit mode";
            string last = GameViewRecording.LastSavedPath;
            string lastNote = string.IsNullOrEmpty(last) ? "no take saved yet" : $"last take: {last}";

            if (GameViewRecording.IsAwaitingPlayMode)
            {
                return $"Waiting for play mode to start the take ({playState}); {lastNote}";
            }

            if (GameViewRecording.IsStarting)
            {
                return $"Resizing the Game View before recording ({playState}); {lastNote}";
            }

            if (!GameViewRecording.IsRecording)
            {
                return $"Idle ({playState}); {lastNote}";
            }

            string state = GameViewRecording.IsPaused ? "Paused" : "Recording";
            float limit = GameViewRecording.MaxDuration;
            string limitNote = limit > 0f ? $" of {limit.ToString("0.#", CultureInfo.InvariantCulture)}s" : string.Empty;

            return $"{state} {Describe()}, {FormatDuration(GameViewRecording.Duration)}{limitNote} so far ({playState}); {lastNote}";
        }

        private static string Describe()
        {
            string mode = GameViewRecording.IsDelegated ? "Unity Recorder" : "real time";

            return $"{GameViewRecording.Width}x{GameViewRecording.Height} at {GameViewRecording.Fps} fps ({mode}) into {GameViewRecording.Folder}";
        }

        private static string DurationNote(float duration)
        {
            return duration > 0f ? $", stopping after {duration.ToString("0.#", CultureInfo.InvariantCulture)}s" : string.Empty;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }

        private static int ParseQuality(string quality)
        {
            if (string.IsNullOrWhiteSpace(quality)) return -1;

            if (Enum.TryParse(quality.Trim(), true, out RecordingQuality parsed)) return (int)parsed;

            throw new ArgumentException($"Unknown quality '{quality}'. Use low, medium, high or ultra.");
        }

        private static int ParseMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode)) return -1;

            string normalized = mode.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);

            if (Enum.TryParse(normalized, true, out RecordingMode parsed)) return (int)parsed;

            throw new ArgumentException($"Unknown mode '{mode}'. Use realtime or recorder.");
        }
    }
}
