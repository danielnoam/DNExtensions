using UnityEditor;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Clears component isolation's HideInInspector flags before anything is written to disk.
    /// EditorSceneManager.sceneSaving only covers scenes; this catches prefabs — and anything else
    /// that gets saved — so a hidden component can never be baked into an asset.
    /// </summary>
    internal class ComponentIsolationSaveGuard : UnityEditor.AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            ComponentIsolation.RestoreForExternalWrite();
            return paths;
        }
    }
}
