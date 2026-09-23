using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Unity 6.6 made a UI Toolkit Hierarchy the default. It is not a SceneHierarchyWindow and never
    /// raises hierarchyWindowItemOnGUI, so every pass in this module has nothing to draw into and the
    /// whole module goes silent without an error. The legacy window is still shipped behind
    /// Project Settings > Editor > Use Legacy Hierarchy, so the module turns that on while it is
    /// enabled and turns it back off when disabled — but only if it was the one that turned it on.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyLegacyWindow
    {
        // Internal on 6.6 and absent before it, where there is only the legacy window anyway.
        private static readonly PropertyInfo UseLegacyHierarchy = typeof(EditorSettings).GetProperty(
            "useLegacyHierarchy", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        static HierarchyLegacyWindow()
        {
            // Setting it swaps any open Hierarchy for the other kind, which must not happen mid-load.
            EditorApplication.delayCall -= Sync;
            EditorApplication.delayCall += Sync;
        }

        /// <summary>Call after the module is toggled.</summary>
        public static void Sync()
        {
            if (UseLegacyHierarchy == null || !UseLegacyHierarchy.CanWrite) return;

            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
            bool isLegacy = (bool)UseLegacyHierarchy.GetValue(null);

            if (settings.moduleEnabled)
            {
                if (isLegacy) return;

                UseLegacyHierarchy.SetValue(null, true);
                settings.forcedLegacyHierarchy = true;
                HelpfulEditorSettings.SaveHierarchy();
                Debug.Log("HelpfulEditor: switched to the legacy Hierarchy window, which the Hierarchy module draws into. " +
                          "Disable the module in Project Settings > HelpfulEditor > Hierarchy to go back to Unity's new one.");
            }
            else if (settings.forcedLegacyHierarchy)
            {
                if (isLegacy) UseLegacyHierarchy.SetValue(null, false);
                settings.forcedLegacyHierarchy = false;
                HelpfulEditorSettings.SaveHierarchy();
            }
        }
    }
}
