namespace DNExtensions.HelpfulEditor
{
    internal enum LineStyle
    {
        Solid,
        Dotted
    }

    internal enum BadgePosition
    {
        LeftOfName,
        RightAligned,
        OnIcon
    }

    internal enum ConflictDefaultChoice
    {
        AlwaysAsk,
        Replace,
        KeepBoth
    }

    /// <summary>Which of the editor's two IMGUI trees an operation targets.</summary>
    internal enum TreeKind
    {
        Project,
        Hierarchy
    }

}
