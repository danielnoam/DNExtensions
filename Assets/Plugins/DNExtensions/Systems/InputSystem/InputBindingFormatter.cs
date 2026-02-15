using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;


namespace DNExtensions.InputSystem
{
    public static class InputBindingFormatter
    {
        private static readonly Dictionary<string, string> TextOverrides = new()
        {
            { "Left Button", "Left Click" },
            { "Right Button", "Right Click" },
            { "Middle Button", "Middle Click" },
            { "D-Pad/Up", "Up"},
            { "D-Pad/Down", "Down"},
            { "D-Pad/Left", "Left"},
            { "D-Pad/Right", "Right"},
            
        };

        #region Public API

        /// <summary>
        /// Get binding display for a specific InputAction
        /// </summary>
        public static string GetActionBinding(InputAction action, bool useSprites, PlayerInput playerInput,
            TMP_SpriteAsset spriteAsset = null, string compositePartFilter = "")
        {
            if (!playerInput || action == null)
            {
                return action?.name ?? "Unknown";
            }

            // Get relevant bindings for the current control scheme
            List<InputBinding> bindings = GetDisplayBindingsForAction(action, playerInput.currentControlScheme, compositePartFilter);

            if (bindings.Count == 0)
            {
                return null;
            }

            // Format bindings as text or sprites
            List<string> formattedBindings = new List<string>();

            foreach (var binding in bindings)
            {
                string formatted = useSprites
                    ? FormatBindingAsSprite(binding, spriteAsset)
                    : FormatBindingAsText(binding);

                formattedBindings.Add(formatted);
            }

            // Join with appropriate separator
            string separator = useSprites ? " " : ", ";
            return string.Join(separator, formattedBindings);
        }

        #endregion

        #region Binding Extraction

        /// <summary>
        /// Extract the bindings to display for an action based on the current control scheme
        /// </summary>
        private static List<InputBinding> GetDisplayBindingsForAction(InputAction action, string currentScheme, string compositePartFilter = "")
        {
            List<InputBinding> displayBindings = new List<InputBinding>();

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (binding.isComposite)
                {
                    // Collect all parts of the composite that match the current scheme
                    for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                    {
                        var partBinding = action.bindings[j];

                        if (BindingMatchesScheme(partBinding, currentScheme))
                        {
                            // Check composite part filter
                            if (string.IsNullOrEmpty(compositePartFilter) || partBinding.name == compositePartFilter)
                            {
                                displayBindings.Add(partBinding);
                            }
                        }
                    }

                    // Skip past composite parts
                    while (i + 1 < action.bindings.Count && action.bindings[i + 1].isPartOfComposite)
                    {
                        i++;
                    }
                }
                else if (!binding.isPartOfComposite)
                {
                    // Single binding (not part of composite) - filter doesn't apply
                    if (BindingMatchesScheme(binding, currentScheme))
                    {
                        displayBindings.Add(binding);
                    }
                }
            }

            return displayBindings;
        }

        private static bool BindingMatchesScheme(InputBinding binding, string scheme)
        {
            return string.IsNullOrEmpty(binding.groups) || binding.groups.Contains(scheme);
        }

        #endregion

        #region Text Formatting

        /// <summary>
        /// Format a binding as human-readable text using Unity's ToDisplayString with custom overrides
        /// </summary>
        private static string FormatBindingAsText(InputBinding binding)
        {
            // Use Unity's built-in display string (handles most cases)
            string displayName = binding.ToDisplayString();

            // Apply custom overrides for specific cases
            if (TextOverrides.TryGetValue(displayName, out string overrideName))
            {
                return overrideName;
            }

            return displayName;
        }

        #endregion

        #region Sprite Formatting

        /// <summary>
        /// Format a binding as a sprite tag with fallback to placeholder if sprite is missing
        /// </summary>
        private static string FormatBindingAsSprite(InputBinding binding, TMP_SpriteAsset spriteAsset)
        {
            if (spriteAsset == null)
            {
                // No sprite asset provided, return placeholder
                return "[?]";
            }

            // Convert binding path to sprite asset naming convention
            string spriteName = ConvertPathToSpriteName(binding.effectivePath);

            // Check if sprite exists
            if (!SpriteExists(spriteAsset, spriteName))
            {
                // Sprite missing, return placeholder
                return "[?]";
            }

            return $"<sprite=\"{spriteAsset.name}\" name=\"{spriteName}\">";
        }

        private static string ConvertPathToSpriteName(string path)
        {
            // Convert Unity input paths to sprite asset naming convention
            string spriteName = path
                .Replace("<Keyboard>/", "Keyboard_")
                .Replace("<Mouse>/", "Mouse_")
                .Replace("<Gamepad>/", "Gamepad_");

            return spriteName;
        }

        private static bool SpriteExists(TMP_SpriteAsset spriteAsset, string spriteName)
        {
            if (spriteAsset == null || spriteAsset.spriteInfoList == null)
            {
                return false;
            }

            foreach (var spriteInfo in spriteAsset.spriteInfoList)
            {
                if (spriteInfo.name == spriteName)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}