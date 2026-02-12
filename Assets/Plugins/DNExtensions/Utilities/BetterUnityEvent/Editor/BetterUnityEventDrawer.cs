using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using UnityEditorInternal;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Custom property drawer for UnityEvent that enables drag-and-drop of GameObjects/Components,
    /// multi-object operations, and copy/paste functionality.
    /// </summary>
    [CustomPropertyDrawer(typeof(UnityEventBase), true)]
    public class BetterUnityEventDrawer : UnityEventDrawer
    {
        private static readonly List<SerializedEventData> CopiedEvents = new List<SerializedEventData>();
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dropRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            HandleDragAndDrop(dropRect, property);
            
            base.OnGUI(position, property, label);
            
            HandleEntryContextMenu(position, property);
        }

        private void HandleDragAndDrop(Rect dropRect, SerializedProperty property)
        {
            Event evt = Event.current;
            
            if (!dropRect.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            var objectsToDrop = new List<UnityEngine.Object>();
                            
                            foreach (var draggedObject in DragAndDrop.objectReferences)
                            {
                                if (draggedObject is GameObject go)
                                {
                                    objectsToDrop.Add(go);
                                }
                                else if (draggedObject is Component comp)
                                {
                                    objectsToDrop.Add(comp);
                                }
                                else if (draggedObject is ScriptableObject so)
                                {
                                    objectsToDrop.Add(so);
                                }
                            }
                            
                            if (objectsToDrop.Count == 1 && objectsToDrop[0] is GameObject singleGO)
                            {
                                ShowComponentMenu(property, singleGO);
                            }
                            else
                            {
                                foreach (var obj in objectsToDrop)
                                {
                                    AddEventWithTarget(property, obj);
                                }
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                        
                        evt.Use();
                    }
                    break;
                    
                case EventType.ContextClick:
                    if (dropRect.Contains(evt.mousePosition))
                    {
                        ShowHeaderContextMenu(property);
                        evt.Use();
                    }
                    break;
            }
        }

        private void HandleEntryContextMenu(Rect position, SerializedProperty property)
        {
            Event evt = Event.current;
            
            if (evt.type != EventType.ContextClick)
                return;
            
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            for (int i = 0; i < callsProperty.arraySize; i++)
            {
                float entryHeight = EditorGUI.GetPropertyHeight(callsProperty.GetArrayElementAtIndex(i));
                var entryRect = new Rect(position.x, position.y + yOffset, position.width, entryHeight);
                
                if (entryRect.Contains(evt.mousePosition))
                {
                    ShowEntryContextMenu(property, i);
                    evt.Use();
                    return;
                }
                
                yOffset += entryHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void ShowHeaderContextMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            if (callsProperty.arraySize > 0)
            {
                menu.AddItem(new GUIContent("Clear All Entries"), false, () =>
                {
                    callsProperty.arraySize = 0;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.AddItem(new GUIContent("Copy All Entries"), false, () => CopyAllEvents(property));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Clear All Entries"));
                menu.AddDisabledItem(new GUIContent("Copy All Entries"));

            }
            
            if (CopiedEvents.Count > 0)
            {
                menu.AddItem(new GUIContent("Paste All Entries"), false, () => PasteEvents(property, true));
                
                if (CopiedEvents.Count == 1)
                {
                    menu.AddItem(new GUIContent("Paste Entry"), false, () => PasteEvents(property, false));
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste All Entries"));
                menu.AddDisabledItem(new GUIContent("Paste Entry"));
            }
            
            menu.ShowAsContext();
        }

        private void ShowEntryContextMenu(SerializedProperty property, int index)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Copy Entry"), false, () => CopySingleEvent(property, index));
            
            if (CopiedEvents.Count == 1)
            {
                menu.AddItem(new GUIContent("Paste Entry"), false, () => PasteEventAtIndex(property, index));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste Entry"));
            }
            
            menu.ShowAsContext();
        }

        private void ShowComponentMenu(SerializedProperty property, GameObject go)
        {
            var components = go.GetComponents<Component>();
            
            if (components.Length == 0)
            {
                AddEventWithTarget(property, go);
                property.serializedObject.ApplyModifiedProperties();
                return;
            }
            
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("GameObject"), false, () =>
            {
                AddEventWithTarget(property, go);
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddSeparator("");
            
            foreach (var comp in components)
            {
                if (comp == null) continue;
                
                var componentType = comp.GetType().Name;
                var localComp = comp;
                
                menu.AddItem(new GUIContent(componentType), false, () =>
                {
                    AddEventWithTarget(property, localComp);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            
            menu.ShowAsContext();
        }

        private void AddEventWithTarget(SerializedProperty property, UnityEngine.Object target)
        {
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            callsProperty.arraySize++;
            var newCall = callsProperty.GetArrayElementAtIndex(callsProperty.arraySize - 1);
            
            var targetProperty = newCall.FindPropertyRelative("m_Target");
            var methodNameProperty = newCall.FindPropertyRelative("m_MethodName");
            var modeProperty = newCall.FindPropertyRelative("m_Mode");
            var argumentsProperty = newCall.FindPropertyRelative("m_Arguments");
            
            targetProperty.objectReferenceValue = target;
            methodNameProperty.stringValue = "";
            modeProperty.enumValueIndex = 1;
            
            argumentsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = 
                "UnityEngine.Object, UnityEngine";
        }

        private void CopySingleEvent(SerializedProperty property, int index)
        {
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            if (index >= 0 && index < callsProperty.arraySize)
            {
                CopiedEvents.Clear();
                var eventData = SerializeEventCall(callsProperty.GetArrayElementAtIndex(index));
                CopiedEvents.Add(eventData);
            }
        }

        private void CopyAllEvents(SerializedProperty property)
        {
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            CopiedEvents.Clear();
            
            for (int i = 0; i < callsProperty.arraySize; i++)
            {
                var eventData = SerializeEventCall(callsProperty.GetArrayElementAtIndex(i));
                CopiedEvents.Add(eventData);
            }
        }

        private void PasteEvents(SerializedProperty property, bool pasteAll)
        {
            if (CopiedEvents.Count == 0) return;
            
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            int eventsToPaste = pasteAll ? CopiedEvents.Count : 1;
            
            for (int i = 0; i < eventsToPaste; i++)
            {
                callsProperty.arraySize++;
                var newCall = callsProperty.GetArrayElementAtIndex(callsProperty.arraySize - 1);
                DeserializeEventCall(newCall, CopiedEvents[i]);
            }
            
            property.serializedObject.ApplyModifiedProperties();
        }

        private void PasteEventAtIndex(SerializedProperty property, int index)
        {
            if (CopiedEvents.Count != 1) return;
            
            var callsProperty = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            if (index >= 0 && index < callsProperty.arraySize)
            {
                var call = callsProperty.GetArrayElementAtIndex(index);
                DeserializeEventCall(call, CopiedEvents[0]);
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private SerializedEventData SerializeEventCall(SerializedProperty call)
        {
            var data = new SerializedEventData
            {
                target = call.FindPropertyRelative("m_Target").objectReferenceValue,
                methodName = call.FindPropertyRelative("m_MethodName").stringValue,
                mode = call.FindPropertyRelative("m_Mode").enumValueIndex,
                callState = call.FindPropertyRelative("m_CallState").enumValueIndex
            };
            
            var argsProperty = call.FindPropertyRelative("m_Arguments");
            data.objectArgument = argsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue;
            data.objectArgumentAssemblyTypeName = argsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue;
            data.intArgument = argsProperty.FindPropertyRelative("m_IntArgument").intValue;
            data.floatArgument = argsProperty.FindPropertyRelative("m_FloatArgument").floatValue;
            data.stringArgument = argsProperty.FindPropertyRelative("m_StringArgument").stringValue;
            data.boolArgument = argsProperty.FindPropertyRelative("m_BoolArgument").boolValue;
            
            return data;
        }

        private void DeserializeEventCall(SerializedProperty call, SerializedEventData data)
        {
            call.FindPropertyRelative("m_Target").objectReferenceValue = data.target;
            call.FindPropertyRelative("m_MethodName").stringValue = data.methodName;
            call.FindPropertyRelative("m_Mode").enumValueIndex = data.mode;
            call.FindPropertyRelative("m_CallState").enumValueIndex = data.callState;
            
            var argsProperty = call.FindPropertyRelative("m_Arguments");
            argsProperty.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = data.objectArgument;
            argsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = data.objectArgumentAssemblyTypeName;
            argsProperty.FindPropertyRelative("m_IntArgument").intValue = data.intArgument;
            argsProperty.FindPropertyRelative("m_FloatArgument").floatValue = data.floatArgument;
            argsProperty.FindPropertyRelative("m_StringArgument").stringValue = data.stringArgument;
            argsProperty.FindPropertyRelative("m_BoolArgument").boolValue = data.boolArgument;
        }

        [Serializable]
        private class SerializedEventData
        {
            public UnityEngine.Object target;
            public string methodName;
            public int mode;
            public int callState;
            public UnityEngine.Object objectArgument;
            public string objectArgumentAssemblyTypeName;
            public int intArgument;
            public float floatArgument;
            public string stringArgument;
            public bool boolArgument;
        }
    }
}