using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// The asset actions the Project window answers its keyboard with — delete, duplicate, cut, copy,
    /// paste — reached from a window that is not a ProjectBrowser.
    ///
    /// Unity's own implementations rather than reimplementations: ProjectWindowUtil.DeleteAssets is
    /// what puts up the "cannot be undone" dialog and decides trash versus permanent, and
    /// AssetClipboardUtility is what makes a cut asset dim until it is pasted. Both are internal, and
    /// only the id type in DeleteAssets has moved across 2022.3 to 6000.5.
    /// </summary>
    internal static class HelpfulEditorAssetCommands
    {
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Assembly EditorAssembly = typeof(EditorWindow).Assembly;

        private static MethodInfo _deleteAssets;
        private static MethodInfo _duplicate;
        private static MethodInfo _cutCopy;
        private static MethodInfo _paste;
        private static Type _performedActionType;

        private static bool _resolved;
        private static bool _warned;

        public static bool Available
        {
            get
            {
                Resolve();
                return _deleteAssets != null;
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                Type projectWindowUtil = EditorAssembly.GetType("UnityEditor.ProjectWindowUtil");
                Type clipboard = EditorAssembly.GetType("UnityEditor.AssetClipboardUtility");

                _deleteAssets = projectWindowUtil?.GetMethod("DeleteAssets", AnyStatic);
                _duplicate = clipboard?.GetMethod("DuplicateSelectedAssets", AnyStatic);
                _cutCopy = clipboard?.GetMethod("CutCopySelectedAssets", AnyStatic);
                _paste = clipboard?.GetMethod("PasteSelectedAssets", AnyStatic);
                _performedActionType = clipboard?.GetNestedType("PerformedAction", BindingFlags.Public | BindingFlags.NonPublic);
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// Deletes the given assets. askIfSure follows the editor's own split: the Delete key arrives
        /// as SoftDelete and asks first, Shift+Delete arrives as Delete and does not.
        /// </summary>
        public static bool Delete(Object[] targets, bool askIfSure)
        {
            Resolve();

            if (_deleteAssets == null || targets == null || targets.Length == 0) return false;

            try
            {
                // List<int> through 2022, List<EntityId> from 6.3 — built from the signature rather
                // than branched on, so there is one path for both.
                Type listType = _deleteAssets.GetParameters()[0].ParameterType;
                Type idType = listType.GetGenericArguments()[0];

                if (Activator.CreateInstance(listType) is not IList ids) return false;

                foreach (Object target in targets)
                {
                    object id = HelpfulEditorObjectId.ConvertTo(HelpfulEditorObjectId.Raw(target), idType);
                    if (id != null) ids.Add(id);
                }

                if (ids.Count == 0) return false;

                return _deleteAssets.Invoke(null, new object[] { ids, askIfSure }) is true;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return false;
            }
        }

        public static void Duplicate() => Invoke(_duplicate, null);

        public static void CutOrCopy(bool cut)
        {
            Resolve();

            if (_cutCopy == null || _performedActionType == null) return;

            try
            {
                object action = Enum.Parse(_performedActionType, cut ? "Cut" : "Copy");
                _cutCopy.Invoke(null, new[] { action });
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        /// <summary>
        /// Pastes into the folder the selection is in, which is how the editor's own paste picks its
        /// destination — it only asks which folder the Project window is showing when the selection
        /// gives it nothing to go on. A caller with a folder of its own seeds the selection first.
        /// </summary>
        public static void Paste() => Invoke(_paste, new object[] { true });

        private static void Invoke(MethodInfo method, object[] arguments)
        {
            Resolve();

            if (method == null) return;

            try
            {
                method.Invoke(null, arguments);
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
            Debug.LogWarning($"[HelpfulEditor] Asset keyboard commands are unavailable on this Unity version. ({e.Message})");
        }
    }
}
