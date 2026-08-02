using System;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Type checks for packages the suite deliberately does not reference. The asmdef carries no
    /// references so the suite keeps compiling in projects where uGUI or TextMeshPro are absent,
    /// which leaves reflection as the only way to recognise their types.
    /// </summary>
    internal static class HelpfulEditorReflection
    {
        /// <summary>
        /// Whether the type or one of its bases is named this. Deliberately walks the type in hand
        /// rather than resolving the base type by name: an assembly is only in the domain once
        /// something has touched it, so a lookup made during InitializeOnLoad can miss a package that
        /// is perfectly well installed — and then never see it again, because it ran once.
        /// </summary>
        public static bool DerivesFrom(Type type, string baseFullName)
        {
            for (; type != null; type = type.BaseType)
            {
                if (string.Equals(type.FullName, baseFullName, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}
