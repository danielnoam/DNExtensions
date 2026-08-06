using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Drag a component onto another GameObject in the Hierarchy to move it there; hold Alt to copy
    /// instead. Dependent components are carried along. Dropping between rows, or past the end of
    /// the list, creates a new GameObject carrying the dragged components.
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentDragger
    {
        static ComponentDragger()
        {
#if UNITY_6000_3_OR_NEWER
            DragAndDrop.RemoveDropHandlerV2((DragAndDrop.HierarchyDropHandlerV2)OnHierarchyDrop);
            DragAndDrop.AddDropHandlerV2((DragAndDrop.HierarchyDropHandlerV2)OnHierarchyDrop);
#else
            DragAndDrop.RemoveDropHandler(OnHierarchyDrop);
            DragAndDrop.AddDropHandler(OnHierarchyDrop);
#endif
        }

        // The V2 handlers arrive in 6000.3, a version before EditorUtility's id APIs change over —
        // so this splits at 6.3 while HelpfulEditorObjectId, which resolves the id, splits at 6.4.
#if UNITY_6000_3_OR_NEWER
        private static DragAndDropVisualMode OnHierarchyDrop(EntityId dropTargetId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            return HandleDrop(dropTargetId, dropMode, perform);
        }
#else
        private static DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceId, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            return HandleDrop(dropTargetInstanceId, dropMode, perform);
        }
#endif

        /// <summary>
        /// The Hierarchy says where the cursor is rather than it having to be worked out: DropUpon is
        /// a drop onto a row, anything else is between two rows or past the end of the list. The row
        /// callback this replaced never fired for the empty space below the last row at all, so the
        /// bottom of the lowest row had to be carried across passes and compared against the cursor.
        ///
        /// Registered for the whole session rather than only while dragging. None is the answer for
        /// any drag that is not carrying components, which leaves the Hierarchy's own handling alone.
        /// </summary>
        private static DragAndDropVisualMode HandleDrop(object rawTargetId, HierarchyDropFlags dropMode, bool perform)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.componentDraggerEnabled) return DragAndDropVisualMode.None;

            Component[] components = CollectDraggedComponents();
            if (components.Length == 0) return DragAndDropVisualMode.None;

            // The handler runs outside the row callback, so there is no guarantee of an event to read
            // the modifier from; no event means no Alt rather than a failed drop.
            Event evt = Event.current;
            bool alt = evt != null && evt.alt;
            bool copyMode = settings.altInvertsMoveCopyDefault ? !alt : alt;

            if (dropMode.HasFlag(HierarchyDropFlags.DropUpon))
            {
                return DropOnObject(HelpfulEditorObjectId.Resolve(rawTargetId) as GameObject, components, copyMode, perform);
            }

            return DropAsNewObject(components, copyMode, perform);
        }

        private static DragAndDropVisualMode DropOnObject(GameObject target, Component[] components, bool copyMode, bool perform)
        {
            if (!target) return DragAndDropVisualMode.None;

            // Moving a component onto the object it already lives on does nothing; copying onto it
            // duplicates it, which is a reasonable thing to have asked for.
            if (components.Any(c => c && c.gameObject == target) && !copyMode) return DragAndDropVisualMode.Rejected;

            // The whole drop is refused rather than transferring the parts that fit — a drop that
            // moved three of four components would read as having worked.
            if (components.Any(c => !CanAdd(c, target))) return DragAndDropVisualMode.Rejected;

            if (perform)
            {
                DragAndDrop.AcceptDrag();
                TransferComponents(components, target, copyMode);
            }

            return copyMode ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
        }

        private static DragAndDropVisualMode DropAsNewObject(Component[] components, bool copyMode, bool perform)
        {
            if (perform)
            {
                DragAndDrop.AcceptDrag();

                string undoName = copyMode ? "Copy Components To New GameObject" : "Move Components To New GameObject";
                Undo.SetCurrentGroupName(undoName);
                int undoGroup = Undo.GetCurrentGroup();

                // Named like Unity's own Create Empty. Naming it after the object the components came
                // from reads as a copy of that object, which is not what was made.
                GameObject created = new GameObject("GameObject");
                Undo.RegisterCreatedObjectUndo(created, undoName);

                TransferComponents(components, created, copyMode, undoGroup);

                Selection.activeGameObject = created;
                Undo.CollapseUndoOperations(undoGroup);
            }

            return copyMode ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
        }

        private static Component[] CollectDraggedComponents()
        {
            Object[] dragged = DragAndDrop.objectReferences;
            if (dragged == null || dragged.Length == 0) return Array.Empty<Component>();

            return dragged.OfType<Component>().Where(c => c && c is not Transform).ToArray();
        }

        private static void TransferComponents(Component[] components, GameObject target, bool copyMode, int? existingUndoGroup = null)
        {
            if (components == null || components.Length == 0 || !target) return;

            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            string undoName = copyMode ? "Copy Component" : "Move Component";

            int undoGroup;
            if (existingUndoGroup.HasValue)
            {
                undoGroup = existingUndoGroup.Value;
            }
            else
            {
                Undo.SetCurrentGroupName(undoName);
                undoGroup = Undo.GetCurrentGroup();
            }

            HashSet<Component> processed = new HashSet<Component>();

            try
            {
                foreach (Component component in components)
                {
                    if (!component || component is Transform) continue;
                    if (!processed.Add(component)) continue;

                    List<Component> dependents = settings.transferDependencies
                        ? GetDependentComponents(component, settings)
                        : new List<Component>();

                    TransferSingleComponent(component, target, copyMode, undoName);

                    foreach (Component dependent in dependents)
                    {
                        if (!dependent || dependent is Transform) continue;
                        if (!processed.Add(dependent)) continue;

                        TransferSingleComponent(dependent, target, copyMode, undoName);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[HelpfulEditor] Error transferring components: {e.Message}");
            }

            if (!existingUndoGroup.HasValue) Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>
        /// Whether Unity will actually take this component. The case that matters is one requiring a
        /// RectTransform landing on an object with a plain Transform: normally Unity swaps the
        /// Transform out to satisfy that, but a prefab instance's Transform cannot be replaced, so it
        /// logs an error and refuses. Asking first is what keeps a refused drop out of the console.
        /// </summary>
        private static bool CanAdd(Component component, GameObject target)
        {
            if (!component || !target) return false;
            if (!RequiresRectTransform(component.GetType())) return true;
            if (target.transform is RectTransform) return true;

            return !PrefabUtility.IsPartOfPrefabInstance(target);
        }

        private static bool RequiresRectTransform(Type type)
        {
            foreach (RequireComponent required in HelpfulEditorMembers.AttributesOf<RequireComponent>(type))
            {
                if (IsRectTransform(required.m_Type0) || IsRectTransform(required.m_Type1) || IsRectTransform(required.m_Type2)) return true;
            }

            return false;
        }

        private static bool IsRectTransform(Type type)
        {
            return type != null && typeof(RectTransform).IsAssignableFrom(type);
        }

        private static void TransferSingleComponent(Component component, GameObject target, bool copyMode, string undoName)
        {
            if (!component || !target) return;

            // Dependents are collected after the drop was accepted, so they get the same check here
            // rather than being handed to Unity to reject noisily.
            if (!CanAdd(component, target)) return;

            Component[] before = target.GetComponents<Component>();

            ComponentUtility.CopyComponent(component);
            if (!ComponentUtility.PasteComponentAsNew(target)) return;

            Component pasted = FindAddedComponent(before, target.GetComponents<Component>());

            // Without this the created component is invisible to the undo stack, so undoing a move
            // restores the original and leaves the pasted copy behind, duplicating the component.
            if (pasted) Undo.RegisterCreatedObjectUndo(pasted, undoName);

            if (!copyMode) Undo.DestroyObjectImmediate(component);
        }

        private static Component FindAddedComponent(Component[] before, Component[] after)
        {
            HashSet<Component> existing = new HashSet<Component>(before);

            for (int i = after.Length - 1; i >= 0; i--)
            {
                if (after[i] && !existing.Contains(after[i])) return after[i];
            }

            return null;
        }

        private static List<Component> GetDependentComponents(Component component, InspectorSettings settings)
        {
            List<Component> dependents = new List<Component>();
            if (!component) return dependents;

            foreach (Component other in component.gameObject.GetComponents<Component>())
            {
                if (!other || other == component) continue;
                if (DependsOn(other, component, settings)) dependents.Add(other);
            }

            return dependents;
        }

        private static bool DependsOn(Component dependent, Component dependency, InspectorSettings settings)
        {
            if (!dependent || !dependency) return false;

            Type dependencyType = dependency.GetType();

            foreach (RequireComponent required in HelpfulEditorMembers.AttributesOf<RequireComponent>(dependent.GetType()))
            {
                if (required.m_Type0 == dependencyType || required.m_Type1 == dependencyType || required.m_Type2 == dependencyType) return true;
            }

            if (dependency is AudioSource && dependent is AudioReverbFilter or AudioLowPassFilter or AudioHighPassFilter
                    or AudioDistortionFilter or AudioEchoFilter or AudioChorusFilter) return true;

            if (dependency is Rigidbody && dependent is Collider or Joint or ConstantForce) return true;

            if (dependency is Rigidbody2D && dependent is Collider2D or Joint2D or ConstantForce2D or Effector2D) return true;

            return MatchesUserWhitelist(dependent, dependency, settings);
        }

        private static bool MatchesUserWhitelist(Component dependent, Component dependency, InspectorSettings settings)
        {
            if (settings.dependencyWhitelist == null) return false;

            foreach (ComponentDependencyPair pair in settings.dependencyWhitelist)
            {
                if (pair == null) continue;
                if (IsTypeOrBase(dependency.GetType(), pair.dependencyType) && IsTypeOrBase(dependent.GetType(), pair.dependentType)) return true;
            }

            return false;
        }

        private static bool IsTypeOrBase(Type type, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return false;

            for (Type current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.Name, typeName, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

    }
}
