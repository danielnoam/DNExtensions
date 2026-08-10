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
        ///
        /// Linux is treated the same way, and for the same reason rather than a confirmed one: the
        /// editor falls back through fontconfig there, so whether a glyph appears depends on whether
        /// the machine happens to have an emoji font installed. Opting in only where the answer is
        /// known trades an icon for a word; guessing wrong the other way leaves a blank button.
        /// </summary>
#if UNITY_EDITOR_WIN
        public const bool SupportsEmoji = true;
#else
        public const bool SupportsEmoji = false;
#endif

        /// <summary>Picks whichever of the two this platform can actually display.</summary>
        public static string Glyph(string emoji, string fallback)
        {
            return SupportsEmoji ? emoji : fallback;
        }
    }
}
