using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Hierarchy
{
    /// <summary>
    /// Hover keybinds for the Hierarchy. Events arrive through GlobalKeyCapture so they fire even
    /// when another window holds focus; the cached hover from HierarchyModule decides the target.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyKeybinds
    {
        static HierarchyKeybinds()
        {
            GlobalKeyCapture.KeyEvent -= OnKeyEvent;
            GlobalKeyCapture.KeyEvent += OnKeyEvent;
        }

        private static void OnKeyEvent()
        {
            HierarchySettings settings = HelpfulEditorSettings.Hierarchy;
            if (!settings.moduleEnabled) return;
            if (!HelpfulEditorWindows.MouseOverHierarchy) return;
            if (EditorGUIUtility.editingTextField) return;

            Event evt = Event.current;
            if (evt == null || evt.type != EventType.KeyDown) return;

            GameObject target = HierarchyModule.HoveredObject ? HierarchyModule.HoveredObject : Selection.activeGameObject;

            if (settings.collapseAllKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.CollapseAllHierarchy();
                evt.Use();
                return;
            }

            if (settings.toggleActiveKey.Matches(evt))
            {
                ToggleActive(target);
                evt.Use();
                return;
            }

            if (settings.isolateKey.Matches(evt))
            {
                HierarchyIsolation.Toggle(target);
                evt.Use();
                return;
            }

            if (settings.expandCollapseRecursiveKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.ToggleHierarchyExpanded(ResolveRowId(target), true);
                evt.Use();
                return;
            }

            if (settings.expandCollapseKey.Matches(evt))
            {
                HelpfulEditorTreeReflection.ToggleHierarchyExpanded(ResolveRowId(target), false);
                evt.Use();
            }
        }

        /// <summary>
        /// Prefers the hovered row's own id so scene headers expand and collapse like any other row,
        /// falling back to the selected object when the cursor is not on a row.
        /// </summary>
        private static object ResolveRowId(GameObject target)
        {
            if (HierarchyModule.HoveredRawId != null) return HierarchyModule.HoveredRawId;

            return target ? HelpfulEditorObjectId.Raw(target) : null;
        }

        private static void ToggleActive(GameObject target)
        {
            GameObject[] targets = ResolveTargets(target);
            if (targets.Length == 0) return;

            bool newState = !targets[0].activeSelf;

            foreach (GameObject gameObject in targets)
            {
                Undo.RecordObject(gameObject, "Toggle Active");
                gameObject.SetActive(newState);
                EditorUtility.SetDirty(gameObject);
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>Acts on the whole selection when the hovered object is part of it, otherwise just that object.</summary>
        private static GameObject[] ResolveTargets(GameObject target)
        {
            GameObject[] selected = Selection.gameObjects;

            if (!target) return selected;

            foreach (GameObject gameObject in selected)
            {
                if (gameObject == target) return selected;
            }

            return new[] { target };
        }
    }
}
