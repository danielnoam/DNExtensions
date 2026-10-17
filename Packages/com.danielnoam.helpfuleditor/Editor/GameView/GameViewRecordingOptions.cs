using System;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Overrides for a single take, for callers that are not the toolbar button — the MCP tools
    /// above all. Anything left at its default falls back to the Game View settings, so an empty
    /// set of options records exactly what the button would.
    ///
    /// Serializable because a take that asks for play mode is started on the far side of the domain
    /// reload entering it causes, and the options have to make the trip through SessionState.
    /// </summary>
    [Serializable]
    internal class GameViewRecordingOptions
    {
        /// <summary>Forces this resolution when both are above zero. Zero keeps the settings' choice.</summary>
        public int width;
        public int height;

        /// <summary>Zero keeps the settings' frame rate.</summary>
        public int fps;

        /// <summary>A <see cref="RecordingQuality"/> value, or -1 to keep the settings' quality.</summary>
        public int quality = -1;

        /// <summary>A <see cref="RecordingMode"/> value, or -1 to keep the settings' mode.</summary>
        public int mode = -1;

        /// <summary>Where the finished file goes, absolute or project-relative. Empty keeps the settings' folder.</summary>
        public string folder = string.Empty;

        /// <summary>Stops the take by itself once it is this many seconds long. Zero records until stopped.</summary>
        public float maxDuration;

        /// <summary>Leaves play mode once the take stops by itself, for a take that entered it.</summary>
        public bool exitPlayModeOnStop;
    }
}
