using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.BuildTool
{
    /// <summary>
    /// Marks a project's own check, run before every build when Run Project Checks is on. The method is static, takes
    /// nothing and returns <c>IEnumerable&lt;string&gt;</c>: one message per problem, none when all is well. Any
    /// message stops the build.
    /// </summary>
    /// <example>
    /// <code>
    /// [BuildCheck("Levels")]
    /// private static IEnumerable&lt;string&gt; LevelsAreValid() { ... yield return "Level 3 has no goal."; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class BuildCheckAttribute : Attribute
    {
        public string Name { get; }

        public BuildCheckAttribute(string name)
        {
            Name = name;
        }
    }

    internal static class BuildChecks
    {
        /// <summary>Every [BuildCheck] method in the project, by name.</summary>
        public static IEnumerable<(string name, MethodInfo method)> All()
        {
            foreach (var method in TypeCache.GetMethodsWithAttribute<BuildCheckAttribute>())
            {
                yield return (method.GetCustomAttribute<BuildCheckAttribute>().Name, method);
            }
        }

        /// <summary>Runs every check and returns their problems, each prefixed with the check's name.</summary>
        public static List<string> Run()
        {
            var problems = new List<string>();

            foreach (var (name, method) in All())
            {
                if (!method.IsStatic || method.GetParameters().Length > 0 || !typeof(IEnumerable<string>).IsAssignableFrom(method.ReturnType))
                {
                    problems.Add($"[BuildCheck] {method.DeclaringType?.Name}.{method.Name} must be static, take nothing and return IEnumerable<string>.");
                    continue;
                }

                try
                {
                    if (method.Invoke(null, null) is not IEnumerable<string> messages) continue;
                    foreach (string message in messages) problems.Add($"{name}: {message}");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    problems.Add($"{name}: the check threw {(e.InnerException ?? e).GetType().Name}. See the Console.");
                }
            }

            return problems;
        }
    }
}
