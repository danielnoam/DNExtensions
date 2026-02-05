namespace DNExtensions.Utilities.AutoGet
{
    /// <summary>
    /// Defines when fields should be automatically populated.
    /// </summary>
    public enum AutoPopulateMode
    {
        /// <summary>
        /// Use the global setting from AutoGetSettings.
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Never auto-populate. Manual only via button or context menu.
        /// </summary>
        Never = 1,
        
        /// <summary>
        /// Only populate when field is null or array/list is empty.
        /// </summary>
        WhenEmpty = 2,
        
        /// <summary>
        /// Always populate on validation, replacing existing values.
        /// </summary>
        Always = 3
    }
}
