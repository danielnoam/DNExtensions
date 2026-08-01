using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Isolation selects the object under the cursor, expands it and its ancestors so it stays
    /// reachable, and collapses everything else. Toggling off puts the previous expansion state
    /// back. Nothing about the scene or its objects changes — only what the tree is showing.
    /// </summary>
    internal static class HierarchyIsolation
    {
        private static object[] _expandedBefore;

        public static GameObject Target { get; private set; }

        public static bool Active => Target;

        public static void Toggle(GameObject target)
        {
            ValidateTarget();

            if (Active)
            {
                GameObject previous = Target;

                // Always unwind first so the saved expansion state stays the pre-isolation one
                // rather than a snapshot of an already-isolated tree.
                Clear();

                // Pressing isolate over a different object retargets onto it instead of just
                // switching off — only re-pressing it over the isolated object ends isolation.
                if (!target || target == previous) return;
            }

            if (!target) return;

            _expandedBefore = HelpfulEditorTreeReflection.GetHierarchyExpandedIds();
            Target = target;

            HelpfulEditorTreeReflection.SetHierarchyExpandedIds(BuildIsolatedExpansion(target, _expandedBefore));

            Selection.activeGameObject = target;

            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// The expanded set is replaced wholesale, so every row that must stay open has to be listed
        /// again. Only GameObject expansions are dropped — scene headers are tree rows with ids of
        /// their own, and collapsing those would hide the whole scene rather than isolate anything.
        /// </summary>
        private static List<object> BuildIsolatedExpansion(GameObject target, object[] previouslyExpanded)
        {
            List<object> expanded = new List<object>();

            if (previouslyExpanded != null)
            {
                foreach (object id in previouslyExpanded)
                {
                    Object resolved = HelpfulEditorObjectId.Resolve(id);
                    if (resolved is GameObject) continue;

                    expanded.Add(id);
                }
            }

            for (Transform current = target.transform; current; current = current.parent)
            {
                expanded.Add(HelpfulEditorObjectId.Raw(current.gameObject));
            }

            return expanded;
        }

        public static void Clear()
        {
            // Deliberately not gated on Active: a destroyed target reads as inactive but still owns
            // the saved expansion state that has to be put back.
            if (Target is null && _expandedBefore == null) return;

            Target = null;

            if (_expandedBefore != null)
            {
                HelpfulEditorTreeReflection.SetHierarchyExpandedIds(_expandedBefore);
                _expandedBefore = null;
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>Isolation is display state, so a destroyed or replaced target simply ends it.</summary>
        private static void ValidateTarget()
        {
            if (Target is null) return;
            if (!Target) Clear();
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.hierarchyChanged -= ValidateTarget;
            EditorApplication.hierarchyChanged += ValidateTarget;
        }
    }
}
