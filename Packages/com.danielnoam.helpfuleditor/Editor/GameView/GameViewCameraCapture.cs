using System.Collections.Generic;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// Renders the scene's cameras into a target of our own, so the UI can be left out of the shot.
    ///
    /// The ordinary capture reads the image the Game View already produced, which is the right way to
    /// save exactly what is on screen but offers no say over what went into it. Rendering the cameras
    /// again is what allows something to be left out, at the cost of a second render — so what lands
    /// on disk is no longer guaranteed to match the window pixel for pixel.
    ///
    /// Screen space overlay canvases need no excluding. They are composited by the editor after the
    /// cameras have run and are simply not part of a manual render, which is why leaving the UI out
    /// costs nothing beyond culling the UI layer for canvases that do go through a camera.
    ///
    /// A transparent background was tried here and abandoned. URP discards the alpha on the way to a
    /// render texture, and it survived none of: a transparent solid clear, HDR off, post-processing
    /// off, PlayerSettings.preserveFramebufferAlpha on, or a full float target. Unity has an open
    /// issue for the same thing. Do not add it back without a way to test it.
    /// </summary>
    internal static class GameViewCameraCapture
    {
        private const string UiLayerName = "UI";

        private static readonly List<Camera> Cameras = new List<Camera>();

        /// <summary>
        /// The scene as its cameras see it, at this size. The caller owns the result and must release
        /// it. Null when there is no camera to render.
        /// </summary>
        public static RenderTexture Capture(int width, int height, bool excludeUi)
        {
            CollectCameras();
            if (Cameras.Count == 0) return null;

            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();

            int uiMask = UiLayerMask();

            foreach (Camera camera in Cameras)
            {
                CameraState state = CameraState.Capture(camera);

                try
                {
                    camera.targetTexture = target;

                    if (excludeUi && uiMask != 0) camera.cullingMask &= ~uiMask;

                    camera.Render();
                }
                finally
                {
                    state.Restore();
                }
            }

            return target;
        }

        /// <summary>
        /// Enabled cameras that draw to the screen, in the order they would draw. Cameras with a
        /// target texture of their own are left alone: they render somewhere else and are not part of
        /// what the Game View shows.
        /// </summary>
        private static void CollectCameras()
        {
            Cameras.Clear();

            foreach (Camera camera in Camera.allCameras)
            {
                if (!camera || !camera.isActiveAndEnabled || camera.targetTexture) continue;

                Cameras.Add(camera);
            }

            Cameras.Sort((left, right) => left.depth.CompareTo(right.depth));
        }

        /// <summary>Zero when the project has renamed or removed the built-in UI layer, which then simply is not excluded.</summary>
        private static int UiLayerMask()
        {
            int layer = LayerMask.NameToLayer(UiLayerName);

            return layer < 0 ? 0 : 1 << layer;
        }

        private readonly struct CameraState
        {
            private readonly Camera _camera;
            private readonly RenderTexture _targetTexture;
            private readonly int _cullingMask;

            private CameraState(Camera camera)
            {
                _camera = camera;
                _targetTexture = camera.targetTexture;
                _cullingMask = camera.cullingMask;
            }

            public static CameraState Capture(Camera camera) => new CameraState(camera);

            public void Restore()
            {
                if (!_camera) return;

                _camera.targetTexture = _targetTexture;
                _camera.cullingMask = _cullingMask;
            }
        }
    }
}
