using UnityEditor;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// Owns every Scene View overlay pass. All passes run from a single callback so their draw order
    /// is explicit rather than dependent on subscription order — the same arrangement the Hierarchy
    /// and Project modules use for their row overlays.
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneViewModule
    {
        static SceneViewModule()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        /// <summary>
        /// Runs once per Scene View per event, for every open one rather than only the focused view.
        /// The module gate is checked here so no pass has to repeat it.
        /// </summary>
        private static void OnSceneGui(SceneView sceneView)
        {
            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            if (!settings.moduleEnabled) return;

            SceneViewPicker.Process(sceneView, settings);
        }
    }
}
