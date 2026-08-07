using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.GameView
{
    /// <summary>
    /// The Game View's resolution list, which is entirely internal. Wrapped here so the one thing that
    /// uses it — a screenshot at a forced resolution — reads as a handful of named calls rather than as
    /// reflection, and so a Unity version that moves any of it fails in one place.
    ///
    /// Sizes are added and removed through Unity's own methods rather than by touching the lists: the
    /// remove takes an index into the combined built-in and custom lists and refuses anything that is
    /// not a custom one, which is the guard that keeps a mistake here from deleting a built-in size.
    /// </summary>
    internal static class GameViewSizeApi
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static PropertyInfo _instanceProperty;
        private static PropertyInfo _currentGroupTypeProperty;
        private static MethodInfo _getGroupMethod;
        private static MethodInfo _addCustomSizeMethod;
        private static MethodInfo _removeCustomSizeMethod;
        private static MethodInfo _getTotalCountMethod;
        private static ConstructorInfo _sizeConstructor;
        private static Type _sizeTypeEnum;
        private static PropertyInfo _selectedSizeIndexProperty;
        private static MethodInfo _sizeSelectionCallback;
        private static bool _resolved;

        public static bool Available
        {
            get
            {
                Resolve();

                return _instanceProperty != null && _currentGroupTypeProperty != null && _getGroupMethod != null &&
                       _addCustomSizeMethod != null && _removeCustomSizeMethod != null && _getTotalCountMethod != null &&
                       _sizeConstructor != null && _sizeTypeEnum != null && _selectedSizeIndexProperty != null;
            }
        }

        /// <summary>The size group for the current build target, which is the list the Game View is showing.</summary>
        public static object CurrentGroup()
        {
            if (!Available) return null;

            try
            {
                object sizes = _instanceProperty.GetValue(null, null);
                object groupType = _currentGroupTypeProperty.GetValue(sizes, null);

                return _getGroupMethod.Invoke(sizes, new[] { groupType });
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static object CreateFixedSize(int width, int height, string name)
        {
            if (!Available) return null;

            try
            {
                // GameViewSizeType.FixedResolution, which is the one that gives an exact pixel size.
                object fixedResolution = Enum.ToObject(_sizeTypeEnum, 1);

                return _sizeConstructor.Invoke(new[] { fixedResolution, (object)width, height, name });
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void AddCustomSize(object group, object size)
        {
            Invoke(_addCustomSizeMethod, group, size);
        }

        public static void RemoveCustomSize(object group, int totalIndex)
        {
            Invoke(_removeCustomSizeMethod, group, totalIndex);
        }

        public static int GetTotalCount(object group)
        {
            if (!Available || group == null) return 0;

            try
            {
                return (int)_getTotalCountMethod.Invoke(group, null);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static int GetSelectedIndex(EditorWindow gameView)
        {
            if (!Available || !gameView) return 0;

            try
            {
                return (int)_selectedSizeIndexProperty.GetValue(gameView, null);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Goes through the callback the size dropdown itself uses where possible. Setting the index
        /// alone leaves the render target at its old size, which for a capture is the whole point missed.
        /// </summary>
        public static void Select(EditorWindow gameView, int index)
        {
            if (!Available || !gameView) return;

            try
            {
                if (_sizeSelectionCallback != null)
                {
                    _sizeSelectionCallback.Invoke(gameView, new object[] { index, null });
                    return;
                }

                _selectedSizeIndexProperty.SetValue(gameView, index, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HelpfulEditor] Could not change the Game View size: {e.Message}");
            }
        }

        private static void Invoke(MethodInfo method, object target, object argument)
        {
            if (!Available || method == null || target == null) return;

            try
            {
                method.Invoke(target, new[] { argument });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HelpfulEditor] Could not update the Game View size list: {e.Message}");
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            Assembly editor = typeof(Editor).Assembly;

            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type groupType = editor.GetType("UnityEditor.GameViewSizeGroup");
            Type sizeType = editor.GetType("UnityEditor.GameViewSize");
            Type gameViewType = editor.GetType("UnityEditor.GameView");

            _sizeTypeEnum = editor.GetType("UnityEditor.GameViewSizeType");

            if (sizesType != null)
            {
                // instance comes from ScriptableSingleton<GameViewSizes>, not from the type itself.
                Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);

                _instanceProperty = singleton.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                _currentGroupTypeProperty = sizesType.GetProperty("currentGroupType", Any);
                _getGroupMethod = sizesType.GetMethod("GetGroup", Any);
            }

            if (groupType != null)
            {
                _addCustomSizeMethod = groupType.GetMethod("AddCustomSize", Any);
                _removeCustomSizeMethod = groupType.GetMethod("RemoveCustomSize", Any);
                _getTotalCountMethod = groupType.GetMethod("GetTotalCount", Any);
            }

            if (sizeType != null && _sizeTypeEnum != null)
            {
                _sizeConstructor = sizeType.GetConstructor(Any, null,
                    new[] { _sizeTypeEnum, typeof(int), typeof(int), typeof(string) }, null);
            }

            if (gameViewType == null) return;

            _selectedSizeIndexProperty = gameViewType.GetProperty("selectedSizeIndex", Any);
            _sizeSelectionCallback = gameViewType.GetMethod("SizeSelectionCallback", Any, null,
                new[] { typeof(int), typeof(object) }, null);
        }
    }
}
