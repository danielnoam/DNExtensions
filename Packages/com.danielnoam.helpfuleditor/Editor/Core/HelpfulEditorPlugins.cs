using System;
using UnityEditor;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Other editor extensions that cover the same ground, so the suite can stand down rather than
    /// fight them. Detected by type presence plus the plugin's own disable flag, since these can be
    /// switched off without being uninstalled.
    /// </summary>
    internal static class HelpfulEditorPlugins
    {
        private static readonly Type VTabsType =
            Type.GetType("VTabs.VTabs, VTabs") ?? Type.GetType("VTabs.VTabs");

        /// <summary>
        /// vTabs renames locked Project windows and floating Properties windows on its own update
        /// loop. Two things writing titleContent would flip-flop every frame, so whichever feature
        /// overlaps has to yield rather than both being right half the time.
        /// </summary>
        public static bool VTabsActive => VTabsType != null && !EditorPrefs.GetBool("vTabs-pluginDisabled", false);
    }
}
