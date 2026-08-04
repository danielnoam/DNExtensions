using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Routes KeyDown/KeyUp events to subscribers regardless of which EditorWindow has focus, by
    /// chaining onto EditorApplication's internal globalEventHandler (the same field Unity uses for
    /// its own tool hotkeys). Subscribers read Event.current directly and call Event.current.Use()
    /// when they consume the event. If the internal field is ever removed, keybinds silently fall
    /// back to requiring window focus rather than throwing.
    /// </summary>
    internal static class GlobalKeyCapture
    {
        public static event Action KeyEvent;

        public static bool Available { get; private set; }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            try
            {
                FieldInfo field = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
                if (field == null)
                {
                    Available = false;
                    Debug.LogWarning("[HelpfulEditor] EditorApplication.globalEventHandler not found — hover keybinds will require window focus.");
                    return;
                }

                EditorApplication.CallbackFunction existing = field.GetValue(null) as EditorApplication.CallbackFunction;
                EditorApplication.CallbackFunction handler = Dispatch;
                field.SetValue(null, handler + (existing - handler));
                Available = true;
            }
            catch (Exception e)
            {
                Available = false;
                Debug.LogWarning($"[HelpfulEditor] Global keybind capture unavailable, hover keybinds will require window focus. ({e.Message})");
            }
        }

        private static void Dispatch()
        {
            Event evt = Event.current;
            if (evt == null) return;
            if (evt.type != EventType.KeyDown && evt.type != EventType.KeyUp) return;

            KeyEvent?.Invoke();
        }
    }
}
