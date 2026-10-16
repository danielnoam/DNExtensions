using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Earlier versions of the module switched Unity 6.6 back to the legacy Hierarchy, because the
    /// new UI Toolkit window never raises hierarchyWindowItemOnGUI and the module had nothing to draw
    /// into. It now decorates the new window directly, so a switch it made is undone once. Which
    /// window to use is left to Project Settings > Editor > Use Legacy Hierarchy from then on: the
    /// module works with either.
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
            EditorApplication.delayCall -= UndoForcedLegacy;
            EditorApplication.delayCall += UndoForcedLegacy;
        }

        private static void UndoForcedLegacy()
        {
            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
            if (!settings.forcedLegacyHierarchy) return;

            settings.forcedLegacyHierarchy = false;
            HelpfulEditorSettings.SaveHierarchy();

            if (UseLegacyHierarchy == null || !UseLegacyHierarchy.CanWrite) return;
            if (!(bool)UseLegacyHierarchy.GetValue(null)) return;

            UseLegacyHierarchy.SetValue(null, false);
            Debug.Log("HelpfulEditor: switched back to Unity's new Hierarchy window, which the Hierarchy module now supports. " +
                      "Use Project Settings > Editor > Use Legacy Hierarchy to choose the old one.");
        }
    }
}
