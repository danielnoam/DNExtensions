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
    /// instead. Dependent components are carried along. Dropping on empty Hierarchy space creates a
    /// new GameObject carrying the dragged components.
    /// </summary>
    [InitializeOnLoad]
    internal static class ComponentDragger
    {
        private static float _passMaxBottom;
        private static float _lastPassMaxBottom;
        private static float _previousRowY = float.MaxValue;
        private static int _rowIndexInPass;
        private static bool _handledThisFrame;

        static ComponentDragger()
        {
            HelpfulEditorHooks.HierarchyItem -= OnHierarchyItem;
            HelpfulEditorHooks.HierarchyItem += OnHierarchyItem;

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            _handledThisFrame = false;
        }

        private static void OnHierarchyItem(object rawId, Object item, Rect rowRect)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !settings.componentDraggerEnabled) return;

            TrackRowBounds(rowRect);

            Event evt = Event.current;
            if (evt == null) return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (_handledThisFrame) return;

            Component[] components = CollectDraggedComponents();
            if (components.Length == 0) return;

            bool copyMode = settings.altInvertsMoveCopyDefault ? !evt.alt : evt.alt;

            if (rowRect.Contains(evt.mousePosition))
            {
                _handledThisFrame = true;
                HandleDropOnRow(item as GameObject, components, copyMode, evt);
                return;
            }

            if (IsFirstRowOfPass() && evt.mousePosition.y > _lastPassMaxBottom)
            {
                _handledThisFrame = true;
                HandleDropOnEmptySpace(components, copyMode, evt);
            }
        }

        private static void HandleDropOnRow(GameObject target, Component[] components, bool copyMode, Event evt)
        {
            if (!target) return;

            bool sameObject = components.Any(c => c && c.gameObject == target);

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = sameObject && !copyMode
                    ? DragAndDropVisualMode.Rejected
                    : copyMode ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;

                evt.Use();
                return;
            }

            if (sameObject && !copyMode)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            TransferComponents(components, target, copyMode);
            evt.Use();
        }

        private static void HandleDropOnEmptySpace(Component[] components, bool copyMode, Event evt)
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = copyMode ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                evt.Use();
                return;
            }

            DragAndDrop.AcceptDrag();

            string undoName = copyMode ? "Copy Components To New GameObject" : "Move Components To New GameObject";
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            GameObject created = new GameObject(components[0] ? components[0].gameObject.name : "GameObject");
            Undo.RegisterCreatedObjectUndo(created, undoName);

            TransferComponents(components, created, copyMode, undoGroup);

            Selection.activeGameObject = created;
            Undo.CollapseUndoOperations(undoGroup);
            evt.Use();
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

        private static void TransferSingleComponent(Component component, GameObject target, bool copyMode, string undoName)
        {
            if (!component || !target) return;

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

            foreach (RequireComponent required in dependent.GetType().GetCustomAttributes(typeof(RequireComponent), true))
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

        /// <summary>
        /// The per-row callback never fires for empty space below the last row, so the bottom of the
        /// lowest row is remembered across passes and used to recognise an empty-space drop.
        /// </summary>
        private static void TrackRowBounds(Rect rowRect)
        {
            if (rowRect.y <= _previousRowY)
            {
                if (_passMaxBottom > 0f) _lastPassMaxBottom = _passMaxBottom;
                _passMaxBottom = 0f;
                _rowIndexInPass = 0;
            }

            _previousRowY = rowRect.y;
            _passMaxBottom = Mathf.Max(_passMaxBottom, rowRect.yMax);
            _rowIndexInPass++;
        }

        private static bool IsFirstRowOfPass()
        {
            return _rowIndexInPass == 1 && _lastPassMaxBottom > 0f;
        }
    }
}
