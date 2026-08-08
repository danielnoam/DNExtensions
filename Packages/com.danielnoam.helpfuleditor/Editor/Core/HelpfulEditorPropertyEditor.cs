using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Unity's Properties window, built without being shown.
    ///
    /// EditorUtility.OpenPropertyEditor is public but always floats the window, and a window that has
    /// already shown itself cannot be handed to a dock area — so docking one needs the internal
    /// overload that takes a showWindow flag. Present on every version the suite targets, with the
    /// public call kept as the fallback for when it is not.
    /// </summary>
    internal static class HelpfulEditorPropertyEditor
    {
        private static MethodInfo _open;
        private static bool _resolved;

        /// <summary>The window for this object, not yet shown, or null if this editor will not build one.</summary>
        public static EditorWindow CreateHidden(Object target)
        {
            if (!target) return null;

            Resolve();
            if (_open == null) return null;

            try
            {
                return _open.Invoke(null, new object[] { target, false }) as EditorWindow;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Unity's own floating Properties window, for when the docked route is unavailable.</summary>
        public static void OpenFloating(Object target)
        {
            if (target) EditorUtility.OpenPropertyEditor(target);
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyEditor");
            if (type == null) return;

            _open = type.GetMethod("OpenPropertyEditor",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Object), typeof(bool) }, null);
        }
    }
}
