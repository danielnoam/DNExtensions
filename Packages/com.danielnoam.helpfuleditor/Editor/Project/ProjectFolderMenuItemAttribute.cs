using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Adds an entry to the right-click menu of the Project window's new-folder button. The method is static, returns
    /// nothing, and takes either nothing or one <c>string</c>: the path of the folder that Project window is browsing,
    /// such as <c>Assets/Art</c>. Use "/" in the label for a submenu.
    /// </summary>
    /// <example>
    /// <code>
    /// [ProjectFolderMenuItem("Level/New Level", Priority = 10, Validate = nameof(CanCreateLevel))]
    /// private static void CreateLevel(string folderPath) { ... }
    ///
    /// private static bool CanCreateLevel(string folderPath) => folderPath.StartsWith("Assets/Levels");
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class ProjectFolderMenuItemAttribute : Attribute
    {
        public string Label { get; }

        /// <summary>Lower comes first. Entries with the same priority are ordered by label.</summary>
        public int Priority { get; set; }

        /// <summary>
        /// Name of a static method on the same type that returns <c>bool</c> and takes the same parameters as the menu
        /// method. When it returns false the entry is shown disabled.
        /// </summary>
        public string Validate { get; set; }

        public ProjectFolderMenuItemAttribute(string label)
        {
            Label = label;
        }
    }

    internal static class ProjectFolderMenuItems
    {
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly HashSet<MethodInfo> Reported = new HashSet<MethodInfo>();

        public readonly struct Entry
        {
            public readonly string label;
            public readonly MethodInfo method;
            public readonly MethodInfo validate;

            public Entry(string label, MethodInfo method, MethodInfo validate)
            {
                this.label = label;
                this.method = method;
                this.validate = validate;
            }

            public bool IsEnabled(string folderPath)
            {
                if (validate == null) return true;

                try
                {
                    return validate.Invoke(null, Arguments(validate, folderPath)) is true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e.InnerException ?? e);
                    return false;
                }
            }

            public void Run(string folderPath)
            {
                try
                {
                    method.Invoke(null, Arguments(method, folderPath));
                }
                catch (Exception e)
                {
                    Debug.LogException(e.InnerException ?? e);
                }
            }
        }

        /// <summary>
        /// Every valid [ProjectFolderMenuItem] in the project, in menu order. A method with the wrong shape is skipped
        /// and reported once per domain reload, so a typo shows up in the Console instead of as a missing entry.
        /// </summary>
        public static List<Entry> All()
        {
            List<(ProjectFolderMenuItemAttribute attribute, Entry entry)> found = new List<(ProjectFolderMenuItemAttribute, Entry)>();

            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<ProjectFolderMenuItemAttribute>())
            {
                ProjectFolderMenuItemAttribute attribute = method.GetCustomAttribute<ProjectFolderMenuItemAttribute>();
                if (attribute == null || string.IsNullOrEmpty(attribute.Label)) continue;

                if (!HasShape(method, typeof(void)))
                {
                    Report(method, $"[ProjectFolderMenuItem] {Name(method)} must be static, return void and take nothing or one string.");
                    continue;
                }

                MethodInfo validate = null;

                if (!string.IsNullOrEmpty(attribute.Validate))
                {
                    validate = method.DeclaringType?.GetMethods(AnyStatic)
                        .FirstOrDefault(m => m.Name == attribute.Validate && HasShape(m, typeof(bool)));

                    if (validate == null)
                    {
                        Report(method, $"[ProjectFolderMenuItem] {Name(method)}: no static bool {attribute.Validate}() or {attribute.Validate}(string) on {method.DeclaringType?.Name}.");
                        continue;
                    }
                }

                found.Add((attribute, new Entry(attribute.Label, method, validate)));
            }

            return found
                .OrderBy(item => item.attribute.Priority)
                .ThenBy(item => item.attribute.Label, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.entry)
                .ToList();
        }

        private static bool HasShape(MethodInfo method, Type returnType)
        {
            if (!method.IsStatic || method.ReturnType != returnType || method.ContainsGenericParameters) return false;

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 0 || (parameters.Length == 1 && parameters[0].ParameterType == typeof(string));
        }

        private static object[] Arguments(MethodInfo method, string folderPath)
        {
            return method.GetParameters().Length == 0 ? null : new object[] { folderPath };
        }

        private static void Report(MethodInfo method, string message)
        {
            if (Reported.Add(method)) Debug.LogError(message);
        }

        private static string Name(MethodInfo method)
        {
            return $"{method.DeclaringType?.Name}.{method.Name}";
        }
    }
}
