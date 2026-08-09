using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Saves what the Game View is showing, straight to disk.
    ///
    /// The pixels are read from the window's own render target rather than through
    /// ScreenCapture, which only delivers at the end of a rendered frame — fine in play mode, but a
    /// button that quietly does nothing while the editor is paused or idle is not worth having. Reading
    /// the target gives the game's own resolution too, not the size the window happens to be.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameViewScreenshot
    {
        private const string DefaultFolder = "Screenshots";
        private const string CaptureSizeName = "Helpful Editor Capture";

        /// <summary>Unity's own ceiling for a Game View size, and a floor that still renders.</summary>
        private const int MinResolution = 1;
        private const int MaxResolution = 8192;

        /// <summary>
        /// How long to wait for the resized view to draw before giving up. Generous: the frames only
        /// tick while the editor is repainting, and a heavy scene takes a few.
        /// </summary>
        private const int MaxWaitFrames = 120;

        /// <summary>Frames at the right size before the pixels are taken, so a freshly cleared target is not what gets saved.</summary>
        private const int SettleFrames = 2;

        private static FieldInfo _renderTextureField;
        private static bool _resolved;

        private static Pending _pending;

        static GameViewScreenshot()
        {
            // A reload with a capture in flight would leave the view resized and a stray custom size
            // behind, with nothing left holding the originals.
            AssemblyReloadEvents.beforeAssemblyReload -= Restore;
            AssemblyReloadEvents.beforeAssemblyReload += Restore;
        }

        /// <summary>A forced capture owns the Game View's size until it is done, so only one runs at a time.</summary>
        public static bool Busy => _pending != null;

        /// <summary>Whether a capture would have anything to save, used to grey the button out.</summary>
        public static bool CanCapture(EditorWindow gameView)
        {
            RenderTexture target = GetRenderTexture(gameView);

            return !Busy && target && target.IsCreated();
        }

        public static void Capture(EditorWindow gameView)
        {
            if (Busy) return;

            GameViewSettings settings = HelpfulEditorSettings.GameView;

            if (settings.screenshotExcludeUi)
            {
                CaptureExcluding(gameView, settings);
                return;
            }

            if (!settings.screenshotForceResolution)
            {
                CaptureNow(gameView);
                return;
            }

            int width = Mathf.Clamp(settings.screenshotResolution.x, MinResolution, MaxResolution);
            int height = Mathf.Clamp(settings.screenshotResolution.y, MinResolution, MaxResolution);

            RenderTexture current = GetRenderTexture(gameView);

            // Already the right size — most of the time, once someone has set the view to match.
            if (current && current.IsCreated() && current.width == width && current.height == height)
            {
                CaptureNow(gameView);
                return;
            }

            BeginForced(gameView, width, height);
        }

        /// <summary>
        /// The excluding path renders the cameras itself, so it can be handed any size directly and
        /// needs none of the resize-and-wait the ordinary capture goes through to reach one.
        /// </summary>
        private static void CaptureExcluding(EditorWindow gameView, GameViewSettings settings)
        {
            RenderTexture current = GetRenderTexture(gameView);

            int width = settings.screenshotForceResolution ? settings.screenshotResolution.x : current ? current.width : 0;
            int height = settings.screenshotForceResolution ? settings.screenshotResolution.y : current ? current.height : 0;

            width = Mathf.Clamp(width, MinResolution, MaxResolution);
            height = Mathf.Clamp(height, MinResolution, MaxResolution);

            RenderTexture target = GameViewCameraCapture.Capture(width, height, settings.screenshotExcludeUi);

            if (!target)
            {
                Debug.LogWarning("[HelpfulEditor] No active camera renders to the Game View, so there is nothing to capture.");
                return;
            }

            try
            {
                Save(target, flipVertically: false);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void CaptureNow(EditorWindow gameView)
        {
            RenderTexture target = GetRenderTexture(gameView);

            if (!target || !target.IsCreated())
            {
                Debug.LogWarning("[HelpfulEditor] The Game View has not rendered yet, so there is nothing to save.");
                return;
            }

            Save(target, flipVertically: true);
        }

        /// <param name="flipVertically">
        /// True for the Game View's own target, which is held bottom up. False for one we rendered
        /// ourselves, which already comes back the right way round — flipping it as well is what put
        /// the excluded captures upside down.
        /// </param>
        private static void Save(RenderTexture target, bool flipVertically)
        {
            string folder = ResolveFolder();

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not create the screenshot folder at {folder}: {e.Message}");
                return;
            }

            string path = NextFreePath(folder, target.width, target.height);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;

            try
            {
                RenderTexture.active = target;

                image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false, false)
                {
                    name = "Game View Screenshot",
                    hideFlags = HideFlags.HideAndDontSave
                };

                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0, false);

                if (flipVertically) FlipVertically(image);
                image.Apply(false, false);

                byte[] encoded = Encode(image);
                if (encoded == null) return;

                File.WriteAllBytes(path, encoded);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not save the screenshot to {path}: {e.Message}");
                return;
            }
            finally
            {
                RenderTexture.active = previous;

                if (image) UnityEngine.Object.DestroyImmediate(image);
            }

            Reveal(path);
        }

        /// <summary>
        /// Sizes the Game View to the wanted resolution, waits for it to draw, then captures and puts
        /// the size back. Done as a temporary custom size rather than by writing the resolution
        /// anywhere: the size list is a project setting, and a capture has no business editing it
        /// permanently. The wait is unavoidable — the game has to be given a frame to render at the new
        /// size before there is anything worth reading.
        /// </summary>
        private static void BeginForced(EditorWindow gameView, int width, int height)
        {
            if (!GameViewSizeApi.Available)
            {
                Debug.LogWarning("[HelpfulEditor] The Game View size API could not be reached, so the screenshot is at the view's own resolution.");
                CaptureNow(gameView);

                return;
            }

            object group = GameViewSizeApi.CurrentGroup();
            object size = GameViewSizeApi.CreateFixedSize(width, height, CaptureSizeName);

            if (group == null || size == null)
            {
                CaptureNow(gameView);
                return;
            }

            int originalIndex = GameViewSizeApi.GetSelectedIndex(gameView);

            GameViewSizeApi.AddCustomSize(group, size);
            int captureIndex = GameViewSizeApi.GetTotalCount(group) - 1;

            _pending = new Pending
            {
                GameView = gameView,
                Group = group,
                Width = width,
                Height = height,
                OriginalIndex = originalIndex,
                CaptureIndex = captureIndex
            };

            GameViewSizeApi.Select(gameView, captureIndex);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            gameView.Repaint();
        }

        private static void Tick()
        {
            if (_pending == null)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!_pending.GameView)
            {
                Restore();
                return;
            }

            _pending.Frames++;

            RenderTexture target = GetRenderTexture(_pending.GameView);
            bool sized = target && target.IsCreated() && target.width == _pending.Width && target.height == _pending.Height;

            if (sized) _pending.SettledFrames++;
            else _pending.SettledFrames = 0;

            if (_pending.SettledFrames >= SettleFrames)
            {
                RenderTexture captured = target;

                // Restored first: saving reads pixels back and writes a file, and the view has no reason
                // to sit at the wrong size while that happens.
                Restore();
                Save(captured, flipVertically: true);

                return;
            }

            if (_pending.Frames > MaxWaitFrames)
            {
                Debug.LogWarning($"[HelpfulEditor] The Game View did not reach {_pending.Width}×{_pending.Height} in time, so no screenshot was saved.");
                Restore();

                return;
            }

            _pending.GameView.Repaint();
        }

        /// <summary>Puts the size selection back and takes the temporary size out again, in that order.</summary>
        private static void Restore()
        {
            EditorApplication.update -= Tick;

            Pending pending = _pending;
            _pending = null;

            if (pending == null) return;

            if (pending.GameView) GameViewSizeApi.Select(pending.GameView, pending.OriginalIndex);

            GameViewSizeApi.RemoveCustomSize(pending.Group, pending.CaptureIndex);

            if (pending.GameView) pending.GameView.Repaint();
        }

        private sealed class Pending
        {
            public EditorWindow GameView;
            public object Group;
            public int Width;
            public int Height;
            public int OriginalIndex;
            public int CaptureIndex;
            public int Frames;
            public int SettledFrames;
        }

        /// <summary>
        /// Shows the folder in the file manager, by way of the newest screenshot in it when there is
        /// one — which also puts the last capture under the cursor. RevealInFinder is only handed a
        /// file: given an absolute folder path Windows tries to open it as a document and asks which
        /// app should handle it, so an empty folder goes through the file URL instead.
        /// </summary>
        public static void OpenFolder()
        {
            string folder = ResolveFolder();

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Could not open the screenshot folder at {folder}: {e.Message}");
                return;
            }

            string newest = NewestScreenshot(folder);

            if (!string.IsNullOrEmpty(newest))
            {
                EditorUtility.RevealInFinder(newest.Replace('\\', '/'));
                return;
            }

            Application.OpenURL("file:///" + folder.Replace('\\', '/'));
        }

        private static string NewestScreenshot(string folder)
        {
            try
            {
                string newest = null;
                DateTime newestTime = DateTime.MinValue;

                // Both extensions, not just the one currently selected — the newest shot in the folder
                // is the one worth landing on whichever format it was taken in.
                foreach (string file in Directory.GetFiles(folder))
                {
                    if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                        !file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) continue;

                    DateTime written = File.GetLastWriteTimeUtc(file);
                    if (written <= newestTime) continue;

                    newest = file;
                    newestTime = written;
                }

                return newest;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Where the setting points, as an absolute path. A relative one is taken from the project root.</summary>
        public static string ResolveFolder()
        {
            string folder = HelpfulEditorSettings.GameView.screenshotFolder;
            if (string.IsNullOrWhiteSpace(folder)) folder = DefaultFolder;

            return Path.IsPathRooted(folder) ? folder : Path.Combine(ProjectRoot, folder);
        }

        /// <summary>The folder holding Assets, which is what a relative setting is written against.</summary>
        public static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        /// <summary>Written back as project-relative when the folder is inside the project, so the setting travels.</summary>
        public static string ToSettingPath(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return DefaultFolder;

            string root = ProjectRoot.Replace('\\', '/').TrimEnd('/');
            string normalised = absolute.Replace('\\', '/').TrimEnd('/');

            if (!normalised.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) return absolute;

            return normalised.Substring(root.Length + 1);
        }

        /// <summary>
        /// Timestamped to the second and tagged with the size actually saved, with a counter behind it.
        /// Two captures inside the same second is unlikely but entirely possible from a button, and
        /// silently overwriting the first is not on. The size comes from the target rather than from the
        /// setting, so it says what the file holds even when the forced resolution was not reached.
        /// </summary>
        private static string NextFreePath(string folder, int width, int height)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            string name = $"GameView {stamp} {width}x{height}";
            string extension = Extension();

            string path = Path.Combine(folder, $"{name}{extension}");

            for (int i = 2; File.Exists(path); i++)
            {
                path = Path.Combine(folder, $"{name} ({i}){extension}");
            }

            return path;
        }

        private static byte[] Encode(Texture2D image)
        {
            GameViewSettings settings = HelpfulEditorSettings.GameView;

            if (settings.screenshotFormat != ScreenshotFormat.Jpg) return image.EncodeToPNG();

            return image.EncodeToJPG(Mathf.Clamp(settings.screenshotJpgQuality, 1, 100));
        }

        private static string Extension()
        {
            return HelpfulEditorSettings.GameView.screenshotFormat == ScreenshotFormat.Jpg ? ".jpg" : ".png";
        }

        /// <summary>Imported and pinged when it lands inside the project, so it shows up without a manual refresh.</summary>
        private static void Reveal(string path)
        {
            string relative = FileUtil.GetProjectRelativePath(path.Replace('\\', '/'));

            if (string.IsNullOrEmpty(relative))
            {
                Debug.Log($"[HelpfulEditor] Saved screenshot to {path}");
                return;
            }

            AssetDatabase.ImportAsset(relative);

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relative);
            if (asset) EditorGUIUtility.PingObject(asset);

            Debug.Log($"[HelpfulEditor] Saved screenshot to {relative}", asset);
        }

        private static void FlipVertically(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();

            int width = texture.width;
            int height = texture.height;

            for (int y = 0; y < height / 2; y++)
            {
                int row = y * width;
                int oppositeRow = (height - 1 - y) * width;

                for (int x = 0; x < width; x++)
                {
                    (pixels[oppositeRow + x], pixels[row + x]) = (pixels[row + x], pixels[oppositeRow + x]);
                }
            }

            texture.SetPixels32(pixels);
        }

        private static RenderTexture GetRenderTexture(EditorWindow gameView)
        {
            ResolveReflection();

            if (_renderTextureField == null || !gameView) return null;

            try
            {
                return _renderTextureField.GetValue(gameView) as RenderTexture;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ResolveReflection()
        {
            if (_resolved) return;
            _resolved = true;

            Type gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");

            _renderTextureField = gameViewType?.GetField("m_RenderTexture", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
