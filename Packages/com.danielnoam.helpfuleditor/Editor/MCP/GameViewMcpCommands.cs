using System;
using System.IO;
using System.Linq;
using DNExtensions.HelpfulEditor.GameView;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.MCP
{
    /// <summary>
    /// Lets an agent see the game. Saves to the project's Temp folder by default so captures never
    /// end up imported or committed.
    /// </summary>
    internal static class GameViewMcpCommands
    {
        private const string DefaultFolder = "Temp/DNExtensionsMCP";
        private const int FallbackWidth = 1920;
        private const int FallbackHeight = 1080;
        private const int MaxResolution = 8192;

        [CliCommand("dn_capture_game_view", "Save a PNG of the Game View and return its absolute path. Without a size or exclude_ui it is exactly what the Game View shows, overlay UI included; otherwise the cameras are rendered again at that size")]
        private static string CaptureGameView(
            [CliArg("width", "Render the cameras at this width instead of copying the Game View (0 keeps the view's own)")] int width = 0,
            [CliArg("height", "Render the cameras at this height instead of copying the Game View (0 keeps the view's own)")] int height = 0,
            [CliArg("exclude_ui", "Leave the UI out. Screen space overlay canvases are never part of a camera render")] bool excludeUi = false,
            [CliArg("path", "Where to save the PNG, absolute or project-relative. Defaults to Temp/DNExtensionsMCP")] string path = "")
        {
            EditorWindow gameView = FindGameView();
            RenderTexture viewTarget = GameViewScreenshot.GetRenderTexture(gameView);
            bool hasViewTarget = viewTarget && viewTarget.IsCreated();

            if (!excludeUi && width <= 0 && height <= 0 && hasViewTarget)
            {
                return Save(viewTarget, true, path, "game view");
            }

            int captureWidth = Mathf.Clamp(width > 0 ? width : hasViewTarget ? viewTarget.width : FallbackWidth, 1, MaxResolution);
            int captureHeight = Mathf.Clamp(height > 0 ? height : hasViewTarget ? viewTarget.height : FallbackHeight, 1, MaxResolution);

            RenderTexture rendered = GameViewCameraCapture.Capture(captureWidth, captureHeight, excludeUi);

            if (!rendered)
            {
                throw new InvalidOperationException("No active camera renders to the Game View, so there is nothing to capture.");
            }

            try
            {
                return Save(rendered, false, path, "camera render, no overlay UI");
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rendered);
            }
        }

        private static EditorWindow FindGameView()
        {
            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return null;

            return Resources.FindObjectsOfTypeAll(gameViewType).OfType<EditorWindow>().FirstOrDefault();
        }

        private static string Save(RenderTexture target, bool flipVertically, string path, string source)
        {
            string fullPath = ResolvePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? GameViewScreenshot.ProjectRoot);

            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;

            try
            {
                RenderTexture.active = target;

                image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0, false);

                if (flipVertically)
                {
                    GameViewScreenshot.FlipVertically(image);
                }

                image.Apply(false, false);
                File.WriteAllBytes(fullPath, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;

                if (image)
                {
                    Object.DestroyImmediate(image);
                }
            }

            return $"{fullPath} ({target.width}x{target.height}, {source}, {(Application.isPlaying ? "play mode" : "edit mode")})";
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(DefaultFolder, $"GameView_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(GameViewScreenshot.ProjectRoot, path);
            }

            return Path.GetFullPath(Path.ChangeExtension(path, ".png"));
        }
    }
}
