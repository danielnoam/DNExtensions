using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Adds Scene View alignment buttons to Camera component headers: one moves the camera to the
    /// Scene View's viewpoint, the other moves the Scene View to the camera's.
    /// </summary>
    [InitializeOnLoad]
    internal static class CameraAlignButtons
    {
        private static readonly string AlignCameraIcon = HelpfulEditorPlatform.Glyph("🎥", "Cam");
        private static readonly string AlignViewIcon = HelpfulEditorPlatform.Glyph("👁", "View");

        static CameraAlignButtons()
        {
            // Two registrations rather than one: a provider contributes a single button per component.
            ComponentHeaderButtons.RegisterProvider(GetAlignCameraButton);
            ComponentHeaderButtons.RegisterProvider(GetAlignViewButton);
        }

        private static ComponentHeaderButtons.ButtonData GetAlignCameraButton(Component component)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = AlignCameraIcon,
                Tooltip = "Move this camera to the Scene View's viewpoint",
                Priority = -900,
                Callback = AlignCameraToView
            };
        }

        private static ComponentHeaderButtons.ButtonData GetAlignViewButton(Component component)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = AlignViewIcon,
                Tooltip = "Move the Scene View to this camera's viewpoint",
                Priority = -890,
                Callback = AlignViewToCamera
            };
        }

        private static bool ShouldShow(Component component)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.cameraAlignButtonsEnabled) return false;

            return component is Camera;
        }

        private static void AlignCameraToView(Component component)
        {
            SceneView view = HelpfulEditorWindows.ResolveSceneView(out bool _);
            if (!view || !view.camera) return;

            Transform target = component.transform;
            Transform source = view.camera.transform;

            Undo.RecordObject(target, "Align Camera To Scene View");
            target.SetPositionAndRotation(source.position, source.rotation);
        }

        private static void AlignViewToCamera(Component component)
        {
            SceneView view = HelpfulEditorWindows.ResolveSceneView(out bool needsFocus);
            if (!view) return;

            // Brought to front when nothing was looking at it, since a Scene View that moved behind
            // the Game tab is indistinguishable from a button that did nothing.
            if (needsFocus) view.Focus();

            Transform source = component.transform;

            // The Scene View camera sits cameraDistance behind the pivot, so the pivot is pushed that
            // far forward to leave the viewpoint itself exactly on the camera. Size is passed through
            // unchanged to keep the distance — and therefore the alignment — stable.
            view.LookAt(source.position + source.forward * view.cameraDistance, source.rotation, view.size);
            view.Repaint();
        }
    }
}
