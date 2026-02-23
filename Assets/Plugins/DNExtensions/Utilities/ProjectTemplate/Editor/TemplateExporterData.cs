namespace DNExtensions.Utilities
{
    internal static class TemplateExporterData
    {
        internal static readonly string[] AlwaysExcluded =
        {
            "ProjectVersion.txt",
            "EditorBuildSettings.asset",
            "NavMeshAreas.asset",
            "VFXManager.asset",
            "XRSettings.asset",
            "MemorySettings.asset",
            "CloudProjectSettings.asset"
        };

        internal static readonly (string file, string label)[] ToggleableSettings =
        {
            ("EditorSettings.asset",         "Editor Settings (version control, serialization)"),
            ("InputManager.asset",           "Input Manager (legacy axes)"),
            ("PackageManagerSettings.asset", "Package Manager (scoped registries)"),
            ("Physics2DSettings.asset",      "Physics 2D"),
            ("DynamicsManager.asset",        "Physics 3D"),
            ("TagManager.asset",             "Tags & Layers"),
            ("GraphicsSettings.asset",       "Graphics Settings"),
            ("QualitySettings.asset",        "Quality Settings"),
            ("AudioManager.asset",           "Audio Settings"),
            ("TimeManager.asset",            "Time Settings"),
        };
    }
}