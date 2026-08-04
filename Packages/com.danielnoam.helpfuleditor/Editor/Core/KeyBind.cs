using System;
using System.Text;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    [Serializable]
    internal struct KeyBind
    {
        public KeyCode key;
        public bool ctrl;
        public bool alt;
        public bool shift;

        public bool IsAssigned => key != KeyCode.None;

        /// <summary>
        /// Mouse bindings ride on KeyCode's own Mouse0..Mouse6 entries rather than a separate field,
        /// so they serialise and appear in the settings dropdown like any other key. They arrive as
        /// MouseDown events on a row, not through the global key hook.
        /// </summary>
        public bool IsMouseButton => key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;

        public static KeyBind None => new KeyBind { key = KeyCode.None };

        public static KeyBind Of(KeyCode key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            return new KeyBind { key = key, ctrl = ctrl, alt = alt, shift = shift };
        }

        public bool Matches(Event evt)
        {
            if (evt == null || !IsAssigned) return false;

            if (IsMouseButton)
            {
                if (evt.type != EventType.MouseDown) return false;
                if (evt.button != key - KeyCode.Mouse0) return false;
            }
            else
            {
                if (evt.type != EventType.KeyDown) return false;
                if (evt.keyCode != key) return false;
            }

            bool commandHeld = evt.control || evt.command;
            return commandHeld == ctrl && evt.alt == alt && evt.shift == shift;
        }

        public override string ToString()
        {
            if (!IsAssigned) return "None";

            StringBuilder builder = new StringBuilder();
            if (ctrl) builder.Append("Ctrl+");
            if (alt) builder.Append("Alt+");
            if (shift) builder.Append("Shift+");
            builder.Append(MouseButtonName() ?? key.ToString());
            return builder.ToString();
        }

        private string MouseButtonName()
        {
            return key switch
            {
                KeyCode.Mouse0 => "LMB",
                KeyCode.Mouse1 => "RMB",
                KeyCode.Mouse2 => "MMB",
                _ => null
            };
        }
    }
}
