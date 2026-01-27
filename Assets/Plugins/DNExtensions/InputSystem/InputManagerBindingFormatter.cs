using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;


namespace DNExtensions.Utilities.InputSystem
{
    
    public static class InputManagerBindingFormatter
    {
        // Map action tokens to Unity InputAction names
        private static readonly Dictionary<string, string> ActionMappings = new()
        {
            { "ACTION_MOVE", "Move" },
            { "ACTION_LOOK", "Look" },
            { "ACTION_ATTACK", "Attack" },
            { "ACTION_DODGELEFT", "DodgeLeft" },
            { "ACTION_DODGERIGHT", "DodgeRight" },
            { "ACTION_DODGEFREEFORM", "DodgeFreeform" },
            { "ACTION_PAUSE", "Pause" },
        };

        public static string ReplaceActionBindings(string text, bool useSprites, PlayerInput playerInput,
            TMP_SpriteAsset spriteAsset = null)
        {
            if (!playerInput) return text;


            foreach (var mapping in ActionMappings)
            {
                if (!text.Contains(mapping.Key)) continue;
                var action = playerInput.actions[mapping.Value];

                if (action is { bindings: { Count: > 0 } })
                {
                    string replacement = useSprites
                        ? GetSpriteTagsForAction(action, mapping.Key, playerInput, spriteAsset)
                        : GetTextTagsForAction(action, mapping.Key, playerInput);

                    text = text.Replace(mapping.Key, replacement);
                }
            }

            return text;
        }

        private static string GetTextTagsForAction(InputAction action, string actionKey, PlayerInput playerInput)
        {
            string currentScheme = playerInput.currentControlScheme;
            List<string> textTags = new List<string>();

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (binding.isComposite)
                {
                    // Process composite binding parts (e.g., WASD for movement)
                    List<string> compositeParts = new List<string>();

                    for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                    {
                        var partBinding = action.bindings[j];
                        bool partMatchesScheme = string.IsNullOrEmpty(partBinding.groups) ||
                                                 partBinding.groups.Contains(currentScheme);

                        if (partMatchesScheme)
                        {
                            string textTag = GetTextTag(partBinding);
                            compositeParts.Add(textTag);
                        }
                    }

                    if (compositeParts.Count > 0)
                    {
                        textTags.AddRange(compositeParts);
                    }

                    // Skip past all part bindings
                    while (i + 1 < action.bindings.Count && action.bindings[i + 1].isPartOfComposite)
                    {
                        i++;
                    }
                }
                else if (!binding.isPartOfComposite)
                {
                    // Process single binding
                    bool matchesScheme = string.IsNullOrEmpty(binding.groups) || binding.groups.Contains(currentScheme);

                    if (matchesScheme)
                    {
                        string textTag = GetTextTag(binding);
                        textTags.Add(textTag);
                    }
                }
            }

            return textTags.Count > 0 ? string.Join(", ", textTags) : actionKey;
        }

        private static string GetTextTag(InputBinding binding)
        {
            string buttonName = binding.effectivePath;
            buttonName = ConvertPathToReadableText(buttonName);
            return buttonName;
        }

        private static string ConvertPathToReadableText(string path)
        {
            // Remove device prefixes
            string result = path.Replace("<Keyboard>/", "")
                .Replace("<Mouse>/", "")
                .Replace("<Gamepad>/", "");

            // Convert Unity input paths to user-friendly names
            var textMappings = new Dictionary<string, string>
            {
                { "leftButton", "Left Click" },
                { "rightButton", "Right Click" },
                { "middleButton", "Middle Click" },
                { "leftStick", "Left Stick" },
                { "rightStick", "Right Stick" },
                { "leftTrigger", "Left Trigger" },
                { "rightTrigger", "Right Trigger" },
                { "leftShoulder", "Left Bumper" },
                { "rightShoulder", "Right Bumper" },
                { "buttonSouth", "A" },
                { "buttonEast", "B" },
                { "buttonWest", "X" },
                { "buttonNorth", "Y" },
                { "dpad/up", "D-Pad Up" },
                { "dpad/down", "D-Pad Down" },
                { "dpad/left", "D-Pad Left" },
                { "dpad/right", "D-Pad Right" },
            };

            if (textMappings.TryGetValue(result, out var text))
            {
                return text;
            }

            // Convert single letters to uppercase
            if (result.Length == 1 && char.IsLetter(result[0]))
            {
                return result.ToUpper();
            }

            return result;
        }

        private static string GetSpriteTagsForAction(InputAction action, string actionKey, PlayerInput playerInput,
            TMP_SpriteAsset spriteAsset)
        {
            string currentScheme = playerInput.currentControlScheme;
            List<string> spriteTags = new List<string>();

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (binding.isComposite)
                {
                    // Process composite binding parts (e.g., WASD for movement)
                    List<string> compositeParts = new List<string>();

                    for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                    {
                        var partBinding = action.bindings[j];
                        bool partMatchesScheme = string.IsNullOrEmpty(partBinding.groups) ||
                                                 partBinding.groups.Contains(currentScheme);

                        if (partMatchesScheme)
                        {
                            string spriteTag = GetSpriteTag(partBinding, spriteAsset);
                            compositeParts.Add(spriteTag);
                        }
                    }

                    if (compositeParts.Count > 0)
                    {
                        spriteTags.AddRange(compositeParts);
                    }

                    // Skip past all part bindings
                    while (i + 1 < action.bindings.Count && action.bindings[i + 1].isPartOfComposite)
                    {
                        i++;
                    }
                }
                else if (!binding.isPartOfComposite)
                {
                    // Process single binding
                    bool matchesScheme = string.IsNullOrEmpty(binding.groups) || binding.groups.Contains(currentScheme);

                    if (matchesScheme)
                    {
                        string spriteTag = GetSpriteTag(binding, spriteAsset);
                        spriteTags.Add(spriteTag);
                    }
                }
            }

            string result = spriteTags.Count > 0 ? string.Join(" ", spriteTags) : actionKey;
            return result;
        }

        private static string GetSpriteTag(InputBinding binding, TMP_SpriteAsset spriteAsset)
        {
            string stringButtonName = binding.effectivePath;
            string originalButtonName = stringButtonName;
            stringButtonName = RenameInput(stringButtonName);

            // Check if the sprite exists in the sprite asset - only warn if there's a problem
            if (spriteAsset && !SpriteExists(spriteAsset, stringButtonName))
            {
                // Debug.LogWarning($"[InputManagerBindingFormatter] Missing sprite '{stringButtonName}' in sprite asset '{spriteAsset.name}' for input '{originalButtonName}'. " +
                // $"Available sprites: {string.Join(", ", GetAllSpriteNames(spriteAsset))}");
            }

            return $"<sprite=\"{spriteAsset?.name}\" name=\"{stringButtonName}\">";
        }

        // Check if a sprite with the given name exists in the sprite asset
        private static bool SpriteExists(TMP_SpriteAsset spriteAsset, string spriteName)
        {
            if (!spriteAsset || spriteAsset.spriteInfoList == null)
                return false;

            foreach (var spriteInfo in spriteAsset.spriteInfoList)
            {
                if (spriteInfo.name == spriteName)
                    return true;
            }

            return false;
        }

        // Helper method to get all sprite names for debugging (only used when there's an error)
        private static string[] GetAllSpriteNames(TMP_SpriteAsset spriteAsset)
        {
            if (!spriteAsset || spriteAsset.spriteInfoList == null)
                return Array.Empty<string>();

            string[] names = new string[spriteAsset.spriteInfoList.Count];
            for (int i = 0; i < spriteAsset.spriteInfoList.Count; i++)
            {
                names[i] = spriteAsset.spriteInfoList[i].name;
            }

            return names;
        }

        // Convert Unity input paths to sprite asset naming convention
        private static string RenameInput(string buttonName)
        {
            buttonName = buttonName.Replace("<Keyboard>/", "Keyboard_");
            buttonName = buttonName.Replace("<Mouse>/", "Mouse_");
            buttonName = buttonName.Replace("<Gamepad>/", "Gamepad_");

            return buttonName;
        }
        
        
        /// <summary>
        /// Get binding display for a specific InputAction
        /// </summary>
        public static string GetActionBinding(InputAction action, bool useSprites, PlayerInput playerInput, TMP_SpriteAsset spriteAsset = null)
        {
            if (!playerInput || action == null) return action?.name ?? "Unknown";
    
            if (useSprites)
            {
                return GetSpriteTagsForAction(action, action.name, playerInput, spriteAsset);
            }
            else
            {
                return GetTextTagsForAction(action, action.name, playerInput);
            }
        }
    }
}