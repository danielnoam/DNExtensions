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

        /// <summary>
        /// Whether the editor font renders emoji here. The macOS editor draws nothing at all for
        /// them, so anything that leans on one for meaning has to say it in words there instead.
        /// </summary>
#if UNITY_EDITOR_OSX
        public const bool SupportsEmoji = false;
#else
        public const bool SupportsEmoji = true;
#endif

        /// <summary>Picks whichever of the two this platform can actually display.</summary>
        public static string Glyph(string emoji, string fallback)
        {
            return SupportsEmoji ? emoji : fallback;
        }
    }
}
