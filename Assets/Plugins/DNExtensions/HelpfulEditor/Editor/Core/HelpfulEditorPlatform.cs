namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Platform wording for user-facing strings. The actions themselves are platform-neutral —
    /// EditorUtility.RevealInFinder already dispatches to Explorer, Finder or the Linux file
    /// manager — but calling all three "Explorer" reads as a Windows-only tool.
    /// </summary>
    internal static class HelpfulEditorPlatform
    {
#if UNITY_EDITOR_OSX
        public const string FileManagerName = "Finder";
#elif UNITY_EDITOR_LINUX
        public const string FileManagerName = "File Manager";
#else
        public const string FileManagerName = "Explorer";
#endif

        /// <summary>Mac reports Command through Event.command, so "Ctrl" in a keybind means Cmd there.</summary>
#if UNITY_EDITOR_OSX
        public const string CommandModifierName = "Cmd";
#else
        public const string CommandModifierName = "Ctrl";
#endif
    }
}
