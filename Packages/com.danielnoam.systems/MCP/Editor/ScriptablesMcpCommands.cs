using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DNExtensions.Systems.Scriptables;
using DNExtensions.Utilities.MCP;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Systems.MCP
{
    /// <summary>
    /// Reads and tunes <see cref="SOValue{T}"/> assets and raises <see cref="SOEvent"/> assets.
    /// Values are written through a SerializedObject, so they behave like an inspector edit:
    /// undoable, and in play mode they fire OnValueChanged through OnValidate.
    /// </summary>
    internal static class ScriptablesMcpCommands
    {
        private const string ValueProperty = "value";

        [CliCommand("dn_list_scriptables", "List SOValue assets (SOFloat, SOInt, SOBool, SOColor, SOVector3...) with their current values, and SOEvent assets")]
        private static string ListScriptables(
            [CliArg("filter", "Only assets whose path contains this text (case-insensitive)")] string filter = "")
        {
            var builder = new StringBuilder();

            foreach (SOBase asset in McpUtility.FindAssets<SOBase>(filter).OrderBy(a => AssetDatabase.GetAssetPath(a)))
            {
                SerializedProperty property = new SerializedObject(asset).FindProperty(ValueProperty);
                if (property == null) continue;

                builder.AppendLine($"{AssetDatabase.GetAssetPath(asset)} ({asset.GetType().Name}) = {FormatValue(property)}");
            }

            foreach (SOEvent soEvent in McpUtility.FindAssets<SOEvent>(filter).OrderBy(a => AssetDatabase.GetAssetPath(a)))
            {
                builder.AppendLine($"{AssetDatabase.GetAssetPath(soEvent)} (SOEvent)");
            }

            return builder.Length > 0 ? builder.ToString() : "No SOValue or SOEvent assets found.";
        }

        [CliCommand("dn_set_so_value", "Set the value of an SOValue asset. Vectors and quaternions take 'x,y,z', colors take '#RRGGBBAA' or 'r,g,b,a', sprites take an asset path")]
        private static string SetValue(
            [CliArg("asset", "Asset path, e.g. 'Assets/Data/PlayerSpeed.asset'", Required = true)] string asset,
            [CliArg("value", "The new value", Required = true)] string value)
        {
            var target = McpUtility.LoadAsset<SOBase>(asset);
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(ValueProperty);

            if (property == null)
            {
                throw new ArgumentException($"'{asset}' is not an SOValue asset.");
            }

            string previous = FormatValue(property);

            ApplyValue(property, value);
            serializedObject.ApplyModifiedProperties();

            return $"{asset}: {previous} -> {FormatValue(property)}";
        }

        [CliCommand("dn_invoke_so_event", "Raise an SOEvent asset in play mode, notifying every subscriber")]
        private static string InvokeEvent(
            [CliArg("asset", "Asset path of the SOEvent", Required = true)] string asset)
        {
            McpUtility.RequirePlayMode("Raising an SOEvent");

            var soEvent = McpUtility.LoadAsset<SOEvent>(asset);
            int subscribers = new SerializedObject(soEvent).FindProperty("subscriberCount")?.intValue ?? 0;

            soEvent.Invoke();

            return $"Raised {soEvent.name} ({subscribers} subscriber(s)).";
        }

        private static string FormatValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Float => property.floatValue.ToString(CultureInfo.InvariantCulture),
                SerializedPropertyType.Integer => property.intValue.ToString(),
                SerializedPropertyType.Boolean => property.boolValue.ToString(),
                SerializedPropertyType.String => $"\"{property.stringValue}\"",
                SerializedPropertyType.Color => $"#{ColorUtility.ToHtmlStringRGBA(property.colorValue)}",
                SerializedPropertyType.Vector2 => property.vector2Value.ToString(),
                SerializedPropertyType.Vector3 => property.vector3Value.ToString(),
                SerializedPropertyType.Vector4 => property.vector4Value.ToString(),
                SerializedPropertyType.Vector2Int => property.vector2IntValue.ToString(),
                SerializedPropertyType.Vector3Int => property.vector3IntValue.ToString(),
                SerializedPropertyType.Quaternion => property.quaternionValue.eulerAngles + " (euler)",
                SerializedPropertyType.LayerMask => string.Join("|", Enumerable.Range(0, 32).Where(i => (property.intValue & (1 << i)) != 0).Select(LayerMask.LayerToName).Where(n => !string.IsNullOrEmpty(n))),
                SerializedPropertyType.ObjectReference => property.objectReferenceValue ? AssetDatabase.GetAssetPath(property.objectReferenceValue) : "None",
                SerializedPropertyType.AnimationCurve => $"curve with {property.animationCurveValue?.length ?? 0} key(s)",
                _ => property.propertyType.ToString()
            };
        }

        private static void ApplyValue(SerializedProperty property, string value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    property.floatValue = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = bool.Parse(value);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = ParseColor(value);
                    break;
                case SerializedPropertyType.Vector2:
                    float[] v2 = ParseFloats(value, 2);
                    property.vector2Value = new Vector2(v2[0], v2[1]);
                    break;
                case SerializedPropertyType.Vector3:
                    float[] v3 = ParseFloats(value, 3);
                    property.vector3Value = new Vector3(v3[0], v3[1], v3[2]);
                    break;
                case SerializedPropertyType.Vector4:
                    float[] v4 = ParseFloats(value, 4);
                    property.vector4Value = new Vector4(v4[0], v4[1], v4[2], v4[3]);
                    break;
                case SerializedPropertyType.Vector2Int:
                    float[] v2i = ParseFloats(value, 2);
                    property.vector2IntValue = new Vector2Int((int)v2i[0], (int)v2i[1]);
                    break;
                case SerializedPropertyType.Vector3Int:
                    float[] v3i = ParseFloats(value, 3);
                    property.vector3IntValue = new Vector3Int((int)v3i[0], (int)v3i[1], (int)v3i[2]);
                    break;
                case SerializedPropertyType.Quaternion:
                    float[] euler = ParseFloats(value, 3);
                    property.quaternionValue = Quaternion.Euler(euler[0], euler[1], euler[2]);
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = LayerMask.GetMask(value.Split('|', ',').Select(n => n.Trim()).ToArray());
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = string.IsNullOrEmpty(value) || value == "None" ? null : McpUtility.LoadAsset<Object>(value);
                    break;
                default:
                    throw new ArgumentException($"Setting a {property.propertyType} value is not supported. Edit it in the inspector.");
            }
        }

        private static float[] ParseFloats(string value, int count)
        {
            float[] parts = value.Trim('(', ')', ' ').Split(',').Select(p => float.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();

            if (parts.Length != count)
            {
                throw new ArgumentException($"Expected {count} comma-separated numbers, got '{value}'.");
            }

            return parts;
        }

        private static Color ParseColor(string value)
        {
            if (ColorUtility.TryParseHtmlString(value, out Color color)) return color;

            string[] parts = value.Trim('(', ')', ' ').Split(',');

            if (parts.Length != 3 && parts.Length != 4)
            {
                throw new ArgumentException($"Could not read '{value}' as a color. Use '#RRGGBB', '#RRGGBBAA' or 'r,g,b[,a]' in 0-1.");
            }

            float[] channels = ParseFloats(value, parts.Length);

            return new Color(channels[0], channels[1], channels[2], channels.Length == 4 ? channels[3] : 1f);
        }
    }
}
