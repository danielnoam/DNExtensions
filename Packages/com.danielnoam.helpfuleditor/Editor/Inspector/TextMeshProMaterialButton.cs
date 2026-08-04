using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Adds a button to TextMeshPro headers that copies the shared font material into a new asset and
    /// assigns it, so tweaking this object's material stops changing every other object using the
    /// same font.
    /// </summary>
    [InitializeOnLoad]
    internal static class TextMeshProMaterialButton
    {
        private static readonly string DuplicateIcon = HelpfulEditorPlatform.Glyph("📋", "Mat");
        private const string FallbackFolder = "Assets";
        private const string TextType = "TMPro.TMP_Text";
        private const string SharedMaterialProperty = "fontSharedMaterial";

        static TextMeshProMaterialButton()
        {
            ComponentHeaderButtons.RegisterProvider(GetButton);
        }

        private static ComponentHeaderButtons.ButtonData GetButton(Component component)
        {
            if (!ShouldShow(component)) return null;

            return new ComponentHeaderButtons.ButtonData
            {
                Icon = DuplicateIcon,
                Tooltip = "Duplicate the font material and assign the copy",
                Priority = -870,
                Callback = DuplicateMaterial
            };
        }

        private static bool ShouldShow(Component component)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.textMeshProDuplicateMaterialEnabled) return false;

            return HelpfulEditorReflection.DerivesFrom(component.GetType(), TextType) && GetSharedMaterial(component) != null;
        }

        private static PropertyInfo GetSharedMaterial(Component component)
        {
            return component.GetType().GetProperty(SharedMaterialProperty, BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Assigned through the property rather than the serialized field: TMP's setter is what
        /// rebuilds the text's material references, and a raw field write leaves them stale.
        /// </summary>
        private static void DuplicateMaterial(Component component)
        {
            PropertyInfo sharedMaterial = GetSharedMaterial(component);
            if (sharedMaterial == null) return;

            if (!(sharedMaterial.GetValue(component) is Material source))
            {
                Debug.LogWarning("[HelpfulEditor] No font material to duplicate.", component);
                return;
            }

            string path = PromptForCopyPath(source);
            if (string.IsNullOrEmpty(path)) return;

            // Writing the copy over its own source would destroy the material being duplicated and
            // leave the component pointing at the replacement — never what the button is for.
            if (string.Equals(path, AssetDatabase.GetAssetPath(source), StringComparison.Ordinal))
            {
                Debug.LogWarning("[HelpfulEditor] Cannot duplicate a font material over itself.", component);
                return;
            }

            Material copy = new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(copy, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(component, "Duplicate Font Material");
            sharedMaterial.SetValue(component, copy);
            EditorUtility.SetDirty(component);

            EditorGUIUtility.PingObject(copy);
        }

        /// <summary>
        /// Asks where to save, starting in the folder of the material being copied. Materials living
        /// in a package start at the Assets root instead: the save panel cannot leave the project,
        /// and those folders are read-only anyway. Returns an empty string when cancelled.
        /// </summary>
        private static string PromptForCopyPath(Material source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);

            string folder = FallbackFolder;
            if (sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                folder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? FallbackFolder;
            }

            return EditorUtility.SaveFilePanelInProject(
                "Duplicate Font Material",
                $"{source.name} Preset",
                "mat",
                "Where should the duplicated material be saved?",
                folder);
        }
    }
}
