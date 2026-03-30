using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Toggles the focused editor window into a borderless fullscreen popup on the configured shortcut.
    /// Creates a new instance of the same window type for fullscreen display.
    /// </summary>
    [InitializeOnLoad]
    internal static class FullscreenWindow
    {
        private const string FullscreenMarker = "DNExtensions_Fullscreen";

        private static readonly MethodInfo _getBoundsMethod;

        private static EditorWindow _fullscreenWindow;

        static FullscreenWindow()
        {
            var globalEventHandlerField = typeof(EditorApplication).GetField(
                "globalEventHandler",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (globalEventHandlerField == null)
            {
                Debug.LogWarning("[FullscreenWindow] Could not find globalEventHandler. Shortcut will not work.");
                return;
            }

            _getBoundsMethod = typeof(InternalEditorUtility).GetMethod(
                "GetBoundsOfDesktopAtPoint",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Vector2) },
                null);

            var current = (EditorApplication.CallbackFunction)globalEventHandlerField.GetValue(null);
            current -= OnGlobalEvent;
            current += OnGlobalEvent;
            globalEventHandlerField.SetValue(null, current);

            TryRecoverFullscreenWindow();
        }

        private static void OnGlobalEvent()
        {
            var e = Event.current;

            if (e == null)
            {
                return;
            }

            if (!FullscreenWindowSettings.Instance.MatchesEvent(e))
            {
                return;
            }

            e.Use();

            if (_fullscreenWindow)
            {
                CloseFullscreen();
            }
            else
            {
                var focusedWindow = EditorWindow.focusedWindow;

                if (focusedWindow)
                {
                    OpenFullscreen(focusedWindow);
                }
            }
        }

        private static void OpenFullscreen(EditorWindow source)
        {
            var screenRect = GetScreenRect(source.position.center);

            _fullscreenWindow = Object.Instantiate(source);
            _fullscreenWindow.name = FullscreenMarker;

            _fullscreenWindow.ShowPopup();
            _fullscreenWindow.position = screenRect;
            _fullscreenWindow.minSize = screenRect.size;
            _fullscreenWindow.maxSize = screenRect.size;
            _fullscreenWindow.Focus();
        }

        private static void CloseFullscreen()
        {
            if (!_fullscreenWindow)
            {
                return;
            }

            _fullscreenWindow.Close();
            _fullscreenWindow = null;

            InternalEditorUtility.RepaintAllViews();
        }

        private static Rect GetScreenRect(Vector2 point)
        {
            if (_getBoundsMethod != null)
            {
                try
                {
                    return (Rect)_getBoundsMethod.Invoke(null, new object[] { point });
                }
                catch
                {
                    // Fall through to default
                }
            }

            return new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height);
        }

        private static void TryRecoverFullscreenWindow()
        {
            var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (var window in allWindows)
            {
                if (window.name != FullscreenMarker)
                {
                    continue;
                }

                _fullscreenWindow = window;
                return;
            }
        }
    }
}