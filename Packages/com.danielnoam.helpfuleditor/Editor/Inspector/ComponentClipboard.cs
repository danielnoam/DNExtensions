using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Holds one or more copied components. Unity's own clipboard only ever holds a single
    /// component, so copying a multi-selection needs a buffer of its own — a single copy is still
    /// mirrored into Unity's clipboard so its native Paste Component entries keep working.
    ///
    /// Pasting a type the target already has overwrites that component rather than adding another.
    /// </summary>
    internal static class ComponentClipboard
    {
        private static readonly List<Component> Copied = new List<Component>();

        public static int Count
        {
            get
            {
                Prune();
                return Copied.Count;
            }
        }

        public static void Copy(IReadOnlyList<Component> components)
        {
            Copied.Clear();

            foreach (Component component in components)
            {
                if (component) Copied.Add(component);
            }

            if (Copied.Count == 1) ComponentUtility.CopyComponent(Copied[0]);
        }

        public static void PasteTo(GameObject target)
        {
            Prune();
            if (!target || Copied.Count == 0) return;

            const string undoName = "Paste Components";
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            foreach (Component source in Copied)
            {
                if (!source) continue;

                ComponentUtility.CopyComponent(source);

                // Pasted onto the one already there rather than beside it. Most things worth copying
                // are DisallowMultipleComponent — a Rigidbody, a Collider — and PasteComponentAsNew
                // simply fails on those, so the old behaviour was not a second component but no
                // component and no error.
                if (target.TryGetComponent(source.GetType(), out Component existing))
                {
                    Undo.RecordObject(existing, undoName);
                    ComponentUtility.PasteComponentValues(existing);
                    continue;
                }

                Component[] before = target.GetComponents<Component>();

                if (!ComponentUtility.PasteComponentAsNew(target)) continue;

                // Without this the pasted component is invisible to the undo stack and a single
                // Ctrl+Z would leave it behind.
                Component pasted = FindAdded(before, target.GetComponents<Component>());
                if (pasted) Undo.RegisterCreatedObjectUndo(pasted, undoName);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private static Component FindAdded(Component[] before, Component[] after)
        {
            HashSet<Component> existing = new HashSet<Component>(before);

            for (int i = after.Length - 1; i >= 0; i--)
            {
                if (after[i] && !existing.Contains(after[i])) return after[i];
            }

            return null;
        }

        private static void Prune() => Copied.RemoveAll(component => !component);
    }
}
