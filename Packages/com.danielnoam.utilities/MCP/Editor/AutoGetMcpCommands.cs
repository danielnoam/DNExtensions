using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DNExtensions.Utilities.AutoGet;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities.MCP
{
    internal static class AutoGetMcpCommands
    {
        [CliCommand("dn_autoget_populate", "Populate [AutoGet] fields (GetComponent, GetComponentInChildren, Find... attributes) and report any that are still empty afterwards")]
        private static string Populate(
            [CliArg("target", "Scene hierarchy path; the GameObject and its children are processed. Empty processes every loaded scene")] string target = "",
            [CliArg("force", "Populate every AutoGet field even when its populate mode is Never or WhenEmpty and it already has a value")] bool force = false)
        {
            int processed = 0;
            var empty = new List<string>();

            foreach (MonoBehaviour behaviour in GetBehaviours(target))
            {
                if (!behaviour || !AutoGetSystem.HasAutoGetFields(behaviour)) continue;

                Undo.RecordObject(behaviour, "Populate AutoGet Fields");

                FieldInfo[] fields = AutoGetCache.GetAutoGetFields(behaviour.GetType());

                if (force)
                {
                    foreach (FieldInfo field in fields)
                    {
                        AutoGetSystem.PopulateField(behaviour, field.Name);
                    }
                }
                else
                {
                    AutoGetSystem.Process(behaviour);
                }

                EditorUtility.SetDirty(behaviour);
                processed++;

                foreach (FieldInfo field in fields)
                {
                    if (IsEmpty(field.GetValue(behaviour)))
                    {
                        empty.Add($"{McpUtility.GetHierarchyPath(behaviour.transform)} {behaviour.GetType().Name}.{field.Name}");
                    }
                }
            }

            var builder = new StringBuilder($"Processed {processed} component(s) with AutoGet fields.");

            if (empty.Count > 0)
            {
                builder.AppendLine().AppendLine($"Still empty ({empty.Count}):");
                foreach (string line in empty)
                {
                    builder.AppendLine(line);
                }
            }

            return builder.ToString();
        }

        private static IEnumerable<MonoBehaviour> GetBehaviours(string target)
        {
            if (!string.IsNullOrWhiteSpace(target))
            {
                return McpUtility.FindGameObject(target).GetComponentsInChildren<MonoBehaviour>(true);
            }

            var behaviours = new List<MonoBehaviour>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    behaviours.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));
                }
            }

            return behaviours;
        }

        private static bool IsEmpty(object value)
        {
            return value switch
            {
                null => true,
                Object unityObject => !unityObject,
                ICollection collection => collection.Count == 0,
                _ => false
            };
        }
    }
}
