using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    internal enum LineStyle
    {
        Solid,
        Dotted
    }

    internal enum BadgePosition
    {
        LeftOfName,
        RightAligned,
        OnIcon
    }

    internal enum ConflictDefaultChoice
    {
        AlwaysAsk,
        Replace,
        KeepBoth
    }

    /// <summary>Which of the editor's two IMGUI trees an operation targets.</summary>
    internal enum TreeKind
    {
        Project,
        Hierarchy
    }

    /// <summary>PNG keeps alpha, which is the only reason a transparent capture is worth taking.</summary>
    internal enum ScreenshotFormat
    {
        [InspectorName("PNG — lossless, keeps alpha")] Png,
        [InspectorName("JPG — smaller, no alpha")] Jpg
    }

    /// <summary>
    /// Who does the capturing. Real Time is the suite's own: it follows the clock, holds a frame when
    /// the editor falls behind, works outside play mode and writes no audio. Recorder hands the take
    /// to the Unity Recorder package, which brings the frame driver and the audio that capturing in
    /// step with the game actually needs.
    /// </summary>
    internal enum RecordingMode
    {
        [InspectorName("Real Time — no audio")] RealTime,
        [InspectorName("Unity Recorder — with audio")] Recorder
    }

    /// <summary>
    /// Recording bit rate. The rate is in the name rather than in a label beside it, so the popup
    /// says what each step actually costs without having to be read together with something else.
    /// </summary>
    internal enum RecordingQuality
    {
        [InspectorName("Low — 5 Mbps")] Low,
        [InspectorName("Medium — 10 Mbps")] Medium,
        [InspectorName("High — 20 Mbps")] High,
        [InspectorName("Ultra — 40 Mbps")] Ultra
    }
}
