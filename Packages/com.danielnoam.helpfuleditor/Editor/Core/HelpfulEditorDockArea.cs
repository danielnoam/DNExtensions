using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// DockArea access, shared by everything that creates, restores or renames a docked window.
    ///
    /// A window's host is only a DockArea while it is docked — a floating one has a plain HostView
    /// with none of this — so every lookup here reports nothing rather than failing, and callers
    /// fall back to showing a window free-floating.
    /// </summary>
    internal static class HelpfulEditorDockArea
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type DockAreaType = typeof(EditorWindow).Assembly.GetType("UnityEditor.DockArea");

        private static bool _warned;

        /// <summary>The dock area hosting the window, or null when it is floating.</summary>
        public static Object Of(EditorWindow window)
        {
            if (!window || DockAreaType == null) return null;

            try
            {
                object host = typeof(EditorWindow).GetField("m_Parent", AnyInstance)?.GetValue(window);
                return DockAreaType.IsInstanceOfType(host) ? host as Object : null;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        public static IList Tabs(Object dockArea)
        {
            if (!dockArea) return null;

            try
            {
                return DockAreaType?.GetField("m_Panes", AnyInstance)?.GetValue(dockArea) as IList;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        public static int IndexOfTab(Object dockArea, EditorWindow window)
        {
            IList tabs = Tabs(dockArea);
            return tabs?.IndexOf(window) ?? -1;
        }

        /// <summary>The window currently shown by this dock area, which owns its visual tree.</summary>
        public static EditorWindow ActiveTab(Object dockArea)
        {
            if (!dockArea) return null;

            try
            {
                return DockAreaType?.GetProperty("actualView", AnyInstance)?.GetValue(dockArea) as EditorWindow;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        /// <summary>
        /// Height of the strip the tabs are drawn in. Read from the dock area rather than assumed:
        /// it differs between editor versions, and a guess either misses the strip or claims part of
        /// the window below it.
        /// </summary>
        public static float TabStripHeight(Object dockArea)
        {
            const float fallback = 20f;

            if (!dockArea) return fallback;

            try
            {
                object value = DockAreaType?.GetField("m_TabAreaRect", AnyInstance)?.GetValue(dockArea);
                return value is Rect rect && rect.height > 0f ? rect.height : fallback;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return fallback;
            }
        }

        /// <param name="index">Where to insert, or -1 to append. Falls back to appending when the indexed overload is missing.</param>
        public static bool AddTab(Object dockArea, EditorWindow window, int index = -1)
        {
            if (!dockArea || !window) return false;

            try
            {
                Detach(window, dockArea);

                MethodInfo method = index >= 0 ? FindAddTab(dockArea.GetType(), true) : null;
                bool withIndex = method != null;

                method ??= FindAddTab(dockArea.GetType(), false);
                if (method == null) return false;

                method.Invoke(dockArea, BuildArguments(method, window, withIndex ? index : -1));
                return true;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        /// <summary>
        /// Takes a window out of the dock area it is already in, closing that area's container when it
        /// held nothing else.
        ///
        /// AddTab only registers the window with its new host; it never detaches it from the old one.
        /// For a freshly created window that is nothing to do, but moving one that is already open
        /// leaves the frame it came from behind as an empty floating window. Unity's own tab drag goes
        /// through RemoveTab first for the same reason.
        ///
        /// Skipped when the window is already in the target, where removing it could destroy the very
        /// dock area it is being added to.
        /// </summary>
        private static void Detach(EditorWindow window, Object target)
        {
            Object current = Of(window);
            if (!current || current == target) return;

            typeof(EditorWindow).GetMethod("RemoveFromDockArea", AnyInstance)?.Invoke(window, null);
        }

        /// <summary>
        /// Matched by shape rather than by an exact signature: AddTab carries trailing parameters that
        /// differ between versions, and asking for a specific one finds nothing at all — which looks
        /// exactly like the window not being docked anywhere.
        /// </summary>
        private static MethodInfo FindAddTab(Type dockAreaType, bool withIndex)
        {
            foreach (MethodInfo candidate in dockAreaType.GetMethods(AnyInstance))
            {
                if (candidate.Name != "AddTab") continue;

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length == 0) continue;

                bool indexFirst = parameters[0].ParameterType == typeof(int);
                if (indexFirst != withIndex) continue;

                int windowAt = withIndex ? 1 : 0;
                if (parameters.Length <= windowAt) continue;
                if (!parameters[windowAt].ParameterType.IsAssignableFrom(typeof(EditorWindow))) continue;

                return candidate;
            }

            return null;
        }

        private static object[] BuildArguments(MethodInfo method, EditorWindow window, int index)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];

            int next = 0;
            if (index >= 0) arguments[next++] = index;
            arguments[next++] = window;

            for (int i = next; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                // The pane events are what tell the new tab it has been added; skipping them leaves
                // it in the dock area without ever being told to lay itself out.
                if (parameter.ParameterType == typeof(bool) && parameter.Name == "sendPaneEvents")
                {
                    arguments[i] = true;
                    continue;
                }

                arguments[i] = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }

            return arguments;
        }

        /// <summary>
        /// Drops the GUIContent DockArea caches per window. A window whose title has changed keeps
        /// showing the old tab label until this runs, which reads as the rename not having worked.
        /// </summary>
        public static void ClearTitleCache()
        {
            try
            {
                (DockAreaType?.GetField("s_GUIContents", AnyStatic)?.GetValue(null) as IDictionary)?.Clear();
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private static void WarnOnce(Exception e)
        {
            if (_warned) return;

            _warned = true;
            Debug.LogWarning($"[HelpfulEditor] Docked window handling is unavailable on this Unity version — new windows will float and tab titles will not update. ({e.Message})");
        }
    }
}
