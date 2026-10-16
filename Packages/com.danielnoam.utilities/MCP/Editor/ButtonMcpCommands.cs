using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using DNExtensions.Utilities.Button;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.MCP
{
    /// <summary>
    /// Exposes methods marked with <see cref="ButtonAttribute"/> so an agent can press the same
    /// inspector buttons a person would.
    /// </summary>
    internal static class ButtonMcpCommands
    {
        private const BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [CliCommand("dn_list_buttons", "List the [Button] methods on a scene GameObject's components or on an asset, with their parameters and when they can be pressed")]
        private static string ListButtons(
            [CliArg("target", "Scene hierarchy path (e.g. 'Player/Camera') or asset path (e.g. 'Assets/Data/Wave.asset')", Required = true)] string target)
        {
            var builder = new StringBuilder();

            foreach (Object owner in GetOwners(McpUtility.FindTarget(target)))
            {
                foreach ((MethodInfo method, ButtonAttribute attribute) in GetButtons(owner))
                {
                    string parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}{(p.HasDefaultValue ? $" = {p.DefaultValue ?? "null"}" : "")}"));
                    builder.AppendLine($"{owner.GetType().Name}.{method.Name}({parameters}) label: \"{attribute.Name ?? ObjectNames.NicifyVariableName(method.Name)}\" when: {ResolvePlayMode(attribute)}");
                }
            }

            return builder.Length > 0 ? builder.ToString() : $"No [Button] methods on '{target}'.";
        }

        [CliCommand("dn_invoke_button", "Invoke a [Button] method on a scene GameObject's component or on an asset, exactly as pressing it in the inspector would")]
        private static string InvokeButton(
            [CliArg("target", "Scene hierarchy path or asset path", Required = true)] string target,
            [CliArg("method", "Method name, as listed by dn_list_buttons", Required = true)] string method,
            [CliArg("component", "Component type name, when more than one component has a button with this method name")] string component = "",
            [CliArg("args", "Parameter values as 'name=value' pairs separated by ';'. Supports string, int, float, bool and enums; omitted parameters use their defaults")] string args = "")
        {
            Object owner = null;
            MethodInfo methodInfo = null;
            ButtonAttribute attribute = null;

            foreach (Object candidate in GetOwners(McpUtility.FindTarget(target)))
            {
                if (!string.IsNullOrEmpty(component) && candidate.GetType().Name != component) continue;

                foreach ((MethodInfo m, ButtonAttribute a) in GetButtons(candidate))
                {
                    if (m.Name != method) continue;

                    owner = candidate;
                    methodInfo = m;
                    attribute = a;
                    break;
                }

                if (owner) break;
            }

            if (!owner)
            {
                throw new ArgumentException($"No [Button] method '{method}' on '{target}'. Use dn_list_buttons to see what is available.");
            }

            ButtonPlayMode playMode = ResolvePlayMode(attribute);

            if (playMode == ButtonPlayMode.OnlyWhenPlaying && !Application.isPlaying)
            {
                throw new InvalidOperationException($"'{method}' can only be pressed in play mode.");
            }

            if (playMode == ButtonPlayMode.OnlyWhenNotPlaying && Application.isPlaying)
            {
                throw new InvalidOperationException($"'{method}' can only be pressed outside play mode.");
            }

            object[] parameters = ResolveParameters(methodInfo, owner, ParseArgs(args));

            if (!methodInfo.IsStatic)
            {
                Undo.RecordObject(owner, $"Button: {methodInfo.Name}");
            }

            object result;

            try
            {
                result = methodInfo.Invoke(methodInfo.IsStatic ? null : owner, parameters);
            }
            catch (TargetInvocationException e)
            {
                Exception inner = e.InnerException ?? e;
                throw new InvalidOperationException($"'{method}' threw {inner.GetType().Name}: {inner.Message}", inner);
            }

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(owner);
            }

            return result != null ? $"Invoked {owner.GetType().Name}.{method}, returned: {result}" : $"Invoked {owner.GetType().Name}.{method}.";
        }

        private static IEnumerable<Object> GetOwners(Object target)
        {
            if (target is GameObject gameObject)
            {
                return gameObject.GetComponents<MonoBehaviour>().Where(b => b);
            }

            return new[] { target };
        }

        private static IEnumerable<(MethodInfo, ButtonAttribute)> GetButtons(Object owner)
        {
            for (Type type = owner.GetType(); type != null && type != typeof(MonoBehaviour) && type != typeof(ScriptableObject); type = type.BaseType)
            {
                foreach (MethodInfo method in type.GetMethods(MethodFlags))
                {
                    var attribute = method.GetCustomAttribute<ButtonAttribute>();
                    if (attribute != null) yield return (method, attribute);
                }
            }
        }

        private static ButtonPlayMode ResolvePlayMode(ButtonAttribute attribute)
        {
            return attribute.PlayMode != ButtonPlayMode.UseDefault ? attribute.PlayMode : ButtonSettings.Instance.ButtonPlayMode;
        }

        private static Dictionary<string, string> ParseArgs(string args)
        {
            var values = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(args)) return values;

            foreach (string pair in args.Split(';'))
            {
                int separator = pair.IndexOf('=');

                if (separator <= 0)
                {
                    throw new ArgumentException($"Could not read '{pair}'. Use 'name=value' pairs separated by ';'.");
                }

                values[pair.Substring(0, separator).Trim()] = pair.Substring(separator + 1).Trim();
            }

            return values;
        }

        private static object[] ResolveParameters(MethodInfo method, Object owner, Dictionary<string, string> values)
        {
            ParameterInfo[] parameters = method.GetParameters();
            var resolved = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type type = parameter.ParameterType;

                if (values.TryGetValue(parameter.Name, out string value))
                {
                    resolved[i] = ParseValue(value, type, parameter.Name);
                    values.Remove(parameter.Name);
                }
                else if (type.IsInstanceOfType(owner))
                {
                    resolved[i] = owner;
                }
                else if (parameter.HasDefaultValue)
                {
                    resolved[i] = parameter.DefaultValue;
                }
                else
                {
                    resolved[i] = type.IsValueType ? Activator.CreateInstance(type) : type == typeof(string) ? "" : null;
                }
            }

            if (values.Count > 0)
            {
                throw new ArgumentException($"'{method.Name}' has no parameter named {string.Join(", ", values.Keys.Select(k => $"'{k}'"))}.");
            }

            return resolved;
        }

        private static object ParseValue(string value, Type type, string parameterName)
        {
            try
            {
                if (type == typeof(string)) return value;
                if (type.IsEnum) return Enum.Parse(type, value, true);

                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new ArgumentException($"Could not convert '{value}' to {type.Name} for parameter '{parameterName}'.");
            }
        }
    }
}
