using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.Utilities
{
    internal sealed class FullscreenWindowSettings : ScriptableObject
    {
        private const string SettingsPath = "ProjectSettings/DNExtensions_FullscreenWindowSettings.asset";

        [SerializeField] private bool enabled = true;
        [SerializeField] private ShortcutKey key = ShortcutKey.F12;
        [SerializeField] private ModifierKey modifierKey = ModifierKey.None;

        public bool Enabled => enabled;
        public ShortcutKey Key => key;
        public ModifierKey Modifier => modifierKey;

        private static FullscreenWindowSettings _instance;

        public static FullscreenWindowSettings Instance
        {
            get
            {
                if (_instance)
                {
                    return _instance;
                }

                var loaded = InternalEditorUtility.LoadSerializedFileAndForget(SettingsPath);

                if (loaded is { Length: > 0 })
                {
                    _instance = loaded[0] as FullscreenWindowSettings;
                }

                if (!_instance)
                {
                    _instance = CreateInstance<FullscreenWindowSettings>();
                }

                return _instance;
            }
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(SettingsPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { this }, SettingsPath, true);
        }

        public void ResetToDefaults()
        {
            enabled = true;
            key = ShortcutKey.F12;
            modifierKey = ModifierKey.None;
            Save();
        }

        /// <summary>
        /// Checks whether the given event matches the configured shortcut.
        /// </summary>
        public bool MatchesEvent(Event e)
        {
            if (!enabled || e.type != EventType.KeyDown)
            {
                return false;
            }

            if (e.keyCode != (KeyCode)key)
            {
                return false;
            }

            if (e.control != (modifierKey == ModifierKey.Ctrl))
            {
                return false;
            }

            if (e.alt != (modifierKey == ModifierKey.Alt))
            {
                return false;
            }

            if (e.shift != (modifierKey == ModifierKey.Shift))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a human-readable label for the current shortcut combination.
        /// </summary>
        public string GetShortcutLabel()
        {
            return modifierKey switch
            {
                ModifierKey.Ctrl => $"Ctrl + {key}",
                ModifierKey.Alt => $"Alt + {key}",
                ModifierKey.Shift => $"Shift + {key}",
                _ => key.ToString()
            };
        }

        public enum ModifierKey
        {
            None = 0,
            Shift = 1,
            Ctrl = 2,
            Alt = 3,
        }

        public enum ShortcutKey
        {
            F1 = KeyCode.F1,
            F2 = KeyCode.F2,
            F3 = KeyCode.F3,
            F4 = KeyCode.F4,
            F5 = KeyCode.F5,
            F6 = KeyCode.F6,
            F7 = KeyCode.F7,
            F8 = KeyCode.F8,
            F9 = KeyCode.F9,
            F10 = KeyCode.F10,
            F11 = KeyCode.F11,
            F12 = KeyCode.F12,
            BackQuote = KeyCode.BackQuote,
            Minus = KeyCode.Minus,
            Equals = KeyCode.Equals,
            LeftBracket = KeyCode.LeftBracket,
            RightBracket = KeyCode.RightBracket,
            Semicolon = KeyCode.Semicolon,
            Quote = KeyCode.Quote,
            Comma = KeyCode.Comma,
            Period = KeyCode.Period,
            Slash = KeyCode.Slash,
            Backslash = KeyCode.Backslash,
        }
    }
}