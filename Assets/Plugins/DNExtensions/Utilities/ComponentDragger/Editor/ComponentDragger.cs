using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DNExtenstions.Utilities.ComponentDragger
{
    [InitializeOnLoad]
    public static class ComponentDragger
    {
        private static Component[] draggedComponents;
        private static bool isCopyMode;
        private static bool handledThisFrame;
        
        static ComponentDragger()
        {
            // Hook into the hierarchy window item GUI
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
            
            // Hook into update to check drag state
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            // Reset drag state when no longer dragging
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
            {
                draggedComponents = null;
            }
            
            // Reset frame handler
            handledThisFrame = false;
        }

        private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            Event evt = Event.current;
            
            if (evt == null)
                return;

            // Handle drag operations
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                // If we already handled this frame, skip
                if (handledThisFrame)
                    return;

                // Get the dragged objects first
                UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences;
                
                if (draggedObjects == null || draggedObjects.Length == 0)
                    return;

                // Check if any dragged objects are components
                Component[] components = draggedObjects.OfType<Component>().ToArray();
                
                if (components.Length == 0)
                    return;

                // Filter out Transform components (they can't be moved)
                components = components.Where(c => c != null && !(c is Transform)).ToArray();
                
                if (components.Length == 0)
                    return;

                // Check if mouse is actually over this item's rect
                bool isOverItem = selectionRect.Contains(evt.mousePosition);

                if (!isOverItem)
                {
                    // Not over this item, just return without handling
                    return;
                }

                // We're over THIS item - mark as handled
                handledThisFrame = true;

                // Check if we're dragging over a valid GameObject in the hierarchy
#pragma warning disable CS0618 // Type or member is obsolete
                GameObject targetObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#pragma warning restore CS0618
                
                if (targetObject == null)
                    return;

                // Don't allow dropping on the same GameObject
                bool isDroppingOnSameObject = components.Any(c => c.gameObject == targetObject);
                
                if (evt.type == EventType.DragUpdated)
                {
                    draggedComponents = components;
                    isCopyMode = evt.alt;
                    
                    // Set visual mode based on whether it's valid
                    if (isDroppingOnSameObject && !isCopyMode)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                    else
                    {
                        DragAndDrop.visualMode = isCopyMode ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                    }
                    
                    evt.Use();
                }
                else if (evt.type == EventType.DragPerform)
                {
                    // Don't allow moving to the same object
                    if (isDroppingOnSameObject && !isCopyMode)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        evt.Use();
                        return;
                    }
                    
                    DragAndDrop.AcceptDrag();
                    
                    // Perform the component transfer
                    TransferComponents(components, targetObject, isCopyMode);
                    
                    draggedComponents = null;
                    evt.Use();
                }
            }
        }

        private static void TransferComponents(Component[] components, GameObject targetObject, bool copyMode)
        {
            if (components == null || components.Length == 0 || targetObject == null)
                return;

            Undo.SetCurrentGroupName(copyMode ? "Copy Components" : "Move Components");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                foreach (Component component in components)
                {
                    if (component == null || component is Transform)
                        continue;

                    // Get dependencies
                    List<Component> dependentComponents = GetDependentComponents(component);
                    
                    // Transfer the main component
                    TransferSingleComponent(component, targetObject, copyMode);
                    
                    // Transfer dependent components
                    foreach (Component dependent in dependentComponents)
                    {
                        if (dependent != null && dependent.gameObject == component.gameObject)
                        {
                            TransferSingleComponent(dependent, targetObject, copyMode);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error transferring components: {e.Message}");
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void TransferSingleComponent(Component component, GameObject targetObject, bool copyMode)
        {
            if (component == null || targetObject == null)
                return;

            if (copyMode)
            {
                // Copy the component
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetObject);
            }
            else
            {
                // Move the component by copying then destroying the original
                GameObject sourceObject = component.gameObject;
                
                // Record the source object for undo
                Undo.RecordObject(sourceObject, "Remove Component");
                
                // Copy to target
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetObject);
                
                // Destroy the original
                Undo.DestroyObjectImmediate(component);
            }
        }

        private static List<Component> GetDependentComponents(Component component)
        {
            List<Component> dependents = new List<Component>();
            
            if (component == null)
                return dependents;

            GameObject go = component.gameObject;
            Component[] allComponents = go.GetComponents<Component>();

            foreach (Component otherComponent in allComponents)
            {
                if (otherComponent == component || otherComponent == null)
                    continue;

                // Check if otherComponent depends on component
                if (DependsOn(otherComponent, component))
                {
                    dependents.Add(otherComponent);
                }
            }

            return dependents;
        }

        private static bool DependsOn(Component dependent, Component dependency)
        {
            if (dependent == null || dependency == null)
                return false;

            Type dependentType = dependent.GetType();
            Type dependencyType = dependency.GetType();
            
            // Check RequireComponent attribute
            var requireAttributes = dependentType.GetCustomAttributes(typeof(RequireComponent), true);
            foreach (RequireComponent req in requireAttributes)
            {
                if (req.m_Type0 == dependencyType || 
                    req.m_Type1 == dependencyType || 
                    req.m_Type2 == dependencyType)
                {
                    return true;
                }
            }

            // Whitelist of known Unity component dependencies
            // AudioSource dependencies
            if (dependency is AudioSource)
            {
                if (dependent is AudioReverbFilter || 
                    dependent is AudioLowPassFilter || 
                    dependent is AudioHighPassFilter ||
                    dependent is AudioDistortionFilter ||
                    dependent is AudioEchoFilter ||
                    dependent is AudioChorusFilter)
                {
                    return true;
                }
            }

            // Rigidbody dependencies
            if (dependency is Rigidbody)
            {
                if (dependent is Collider || 
                    dependent is Joint ||
                    dependent is ConstantForce)
                {
                    return true;
                }
            }

            // Rigidbody2D dependencies
            if (dependency is Rigidbody2D)
            {
                if (dependent is Collider2D || 
                    dependent is Joint2D ||
                    dependent is ConstantForce2D ||
                    dependent is Effector2D)
                {
                    return true;
                }
            }

            // Animator dependencies
            if (dependency is Animator)
            {
                //TODO: Add any components that commonly depend on Animator
            }

            // Camera dependencies
            if (dependency is Camera)
            {
                //TODO:  Add any components that commonly depend on Camera
            }

            return false;
        }
    }
}