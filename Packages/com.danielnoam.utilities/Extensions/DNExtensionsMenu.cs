namespace DNExtensions.Utilities
{
    /// <summary>
    /// Where DNExtensions puts its editor menu items: its own menu in the editor's top bar, next to File, Edit and Tools.
    /// Build a menu path from it, e.g. <c>[MenuItem(DNExtensionsMenu.Root + "Quick Navigation")]</c>.
    /// </summary>
    public static class DNExtensionsMenu
    {
        public const string Root = "DNExtensions/";
    }
}
