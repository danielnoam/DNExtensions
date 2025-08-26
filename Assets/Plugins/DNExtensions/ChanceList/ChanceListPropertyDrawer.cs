#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace DNExtensions
{
    [CustomPropertyDrawer(typeof(ChanceList<>), true)]

    public class ChanceListPropertyDrawer : PropertyDrawer
    {
        private ReorderableList _reorderableList;
        private bool _isInitialized = false;

        // Layout configuration

        private const float HeaderHeight = 2f;
        private const float ElementHeight = 3f;
        
        private const float ItemWidthRatio = 0.55f;
        private const float IntFieldWidth = 30f;
        private const float LockButtonWidth = 20f;
        private const float Spacing = 5f;

        private void InitializeList(SerializedProperty property)
        {
            if (_isInitialized) return;

            var internalItemsProperty = property.FindPropertyRelative("internalItems");
            if (internalItemsProperty == null) return;

            _reorderableList =
                new ReorderableList(property.serializedObject, internalItemsProperty, true, true, true, true)
                {
                    drawHeaderCallback = DrawHeader,
                    drawElementCallback = DrawElement,
                    elementHeight = EditorGUIUtility.singleLineHeight + ElementHeight,
                    headerHeight = EditorGUIUtility.singleLineHeight + HeaderHeight,
                    onAddCallback = OnAdd,
                    onRemoveCallback = OnRemove
                };

            _isInitialized = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeList(property);

            if (_reorderableList == null) return;

            EditorGUI.BeginProperty(position, label, property);

            // Draw the label
            var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Handle right-click on the label/header area to add custom options to Unity's default context menu
            if (Event.current.type == EventType.ContextClick && labelRect.Contains(Event.current.mousePosition))
            {
                ShowEnhancedContextMenu(property);
                Event.current.Use();
            }

            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

            // Draw the reorderable list
            var listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f,
                position.width, position.height - EditorGUIUtility.singleLineHeight - 2f);

            // Handle right-click on the list area to add custom options to Unity's default context menu
            if (Event.current.type == EventType.ContextClick && listRect.Contains(Event.current.mousePosition))
            {
                ShowEnhancedContextMenu(property);
                Event.current.Use();
            }

            _reorderableList.DoList(listRect);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitializeList(property);

            if (_reorderableList == null) return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight + 2f + _reorderableList.GetHeight();
        }

        private void DrawHeader(Rect rect)
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = EditorStyles.label.normal.textColor }
            };

            var chanceHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorStyles.label.normal.textColor }
            };

            var itemHeaderRect = new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height);
            var chanceHeaderRect = new Rect(rect.x + rect.width * ItemWidthRatio + 3f, rect.y, rect.width * 0.35f,
                rect.height);

            // Calculate lock header position - aligned to the right
            var lockHeaderRect = new Rect(rect.x + rect.width - LockButtonWidth, rect.y, LockButtonWidth, rect.height);

            GUI.Label(itemHeaderRect, "Item", headerStyle);
            GUI.Label(chanceHeaderRect, "Chance %", chanceHeaderStyle);
            GUI.Label(lockHeaderRect, "🔒", chanceHeaderStyle);
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;

            var itemProperty = element.FindPropertyRelative("item");
            var chanceProperty = element.FindPropertyRelative("chance");
            var isLockedProperty = element.FindPropertyRelative("isLocked");

            // Calculate rects - lock button stays aligned to the right
            var itemRect = new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height);
            var lockButtonRect = new Rect(rect.x + rect.width - LockButtonWidth, rect.y, LockButtonWidth, rect.height);
            var intFieldRect = new Rect(lockButtonRect.x - Spacing - IntFieldWidth, rect.y, IntFieldWidth, rect.height);
            var sliderRect = new Rect(itemRect.xMax + Spacing, rect.y, intFieldRect.x - itemRect.xMax - (Spacing * 2),
                rect.height);

            // Draw item field
            EditorGUI.PropertyField(itemRect, itemProperty, GUIContent.none);

            // Draw chance controls (disabled if locked)
            EditorGUI.BeginDisabledGroup(isLockedProperty.boolValue);

            EditorGUI.BeginChangeCheck();
            float newChance = GUI.HorizontalSlider(sliderRect, chanceProperty.intValue, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProperty.intValue = Mathf.RoundToInt(newChance);
                TriggerNormalization(_reorderableList.serializedProperty);
            }

            EditorGUI.BeginChangeCheck();
            int intValue = EditorGUI.IntField(intFieldRect, chanceProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProperty.intValue = Mathf.Clamp(intValue, 0, 100);
                TriggerNormalization(_reorderableList.serializedProperty);
            }

            EditorGUI.EndDisabledGroup();

            // Draw lock checkbox
            var lockTooltip = isLockedProperty.boolValue ? "Chance value is locked" : "Chance value is unlocked";
            EditorGUI.BeginChangeCheck();
            bool isLocked = EditorGUI.Toggle(lockButtonRect, new GUIContent("", lockTooltip),
                isLockedProperty.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isLockedProperty.boolValue = isLocked;
                TriggerNormalization(_reorderableList.serializedProperty);
            }
        }

        private void OnAdd(ReorderableList list)
        {
            list.serializedProperty.arraySize++;
            var newElement = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);

            var itemProperty = newElement.FindPropertyRelative("item");
            var chanceProperty = newElement.FindPropertyRelative("chance");
            var isLockedProperty = newElement.FindPropertyRelative("isLocked");

            // Set default values
            itemProperty.objectReferenceValue = null;
            chanceProperty.intValue = 10;
            isLockedProperty.boolValue = false;

            // Trigger normalization after adding
            TriggerNormalization(list.serializedProperty);
        }

        private void OnRemove(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            // Trigger normalization after removing
            TriggerNormalization(list.serializedProperty);
        }

        private void EqualizeAllChances(SerializedProperty property)
        {
            var internalItemsProperty = property.FindPropertyRelative("internalItems");
            if (internalItemsProperty.arraySize == 0) return;

            var unlockedIndices = new List<int>();
            int lockedTotal = 0;

            for (int i = 0; i < internalItemsProperty.arraySize; i++)
            {
                var element = internalItemsProperty.GetArrayElementAtIndex(i);
                var isLockedProp = element.FindPropertyRelative("isLocked");
                var chanceProp = element.FindPropertyRelative("chance");

                if (isLockedProp.boolValue)
                {
                    lockedTotal += chanceProp.intValue;
                }
                else
                {
                    unlockedIndices.Add(i);
                }
            }

            if (unlockedIndices.Count == 0) return;

            int remainingPercentage = Mathf.Max(0, 100 - lockedTotal);
            int equalChance = remainingPercentage / unlockedIndices.Count;
            int remainder = remainingPercentage % unlockedIndices.Count;

            for (int i = 0; i < unlockedIndices.Count; i++)
            {
                var element = internalItemsProperty.GetArrayElementAtIndex(unlockedIndices[i]);
                var chanceProp = element.FindPropertyRelative("chance");
                chanceProp.intValue = equalChance + (i < remainder ? 1 : 0);
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private void OnMouseUp(ReorderableList list)
        {
            // This callback is kept for compatibility but context menu is handled in OnGUI
            // since onMouseUpCallback has limited coverage
        }

        private void ShowEnhancedContextMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var internalItemsProperty = property.FindPropertyRelative("internalItems");

            // Add Unity's default property context menu items
            // Copy Property Path
            menu.AddItem(new GUIContent("Copy Property Path"), false,
                () => { EditorGUIUtility.systemCopyBuffer = property.propertyPath; });

            // Copy
            menu.AddItem(new GUIContent("Copy"), false,
                () =>
                {
                    EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(property.serializedObject.targetObject);
                });

            // Copy Path  
            menu.AddItem(new GUIContent("Copy Path"), false,
                () => { EditorGUIUtility.systemCopyBuffer = property.propertyPath; });

            // Copy GUID (if applicable)
            if (property.serializedObject.targetObject != null)
            {
                menu.AddItem(new GUIContent("Copy GUID"), false, () =>
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(property.serializedObject.targetObject,
                            out string guid, out long localId))
                    {
                        EditorGUIUtility.systemCopyBuffer = guid;
                    }
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy GUID"));
            }

            // Paste - simplified version
            menu.AddItem(new GUIContent("Paste"), false, () =>
            {
                // Basic paste functionality - you could enhance this further
                Undo.RecordObject(property.serializedObject.targetObject, "Paste Property");
                property.serializedObject.ApplyModifiedProperties();
                TriggerNormalization(internalItemsProperty);
            });

            menu.AddSeparator("");

            // Properties... - open Inspector window
            menu.AddItem(new GUIContent("Properties..."), false, () =>
            {
                Selection.activeObject = property.serializedObject.targetObject;
                EditorGUIUtility.PingObject(property.serializedObject.targetObject);
            });

            menu.AddSeparator("");

            // ChanceList-specific options
            menu.AddItem(new GUIContent("Equalize All Chances"), false,
                () => EqualizeAllChances_ContextMenu(internalItemsProperty));

            menu.AddSeparator("");

            // Lock/Unlock options
            menu.AddItem(new GUIContent("Lock All"), false,
                () => SetAllLocked_ContextMenu(internalItemsProperty, true));
            menu.AddItem(new GUIContent("Unlock All"), false,
                () => SetAllLocked_ContextMenu(internalItemsProperty, false));

            menu.ShowAsContext();
        }

        private void EqualizeAllChances_ContextMenu(SerializedProperty internalItemsProperty)
        {
            // Find the parent property to pass to the existing method
            var parentProperty = internalItemsProperty.serializedObject.FindProperty(
                internalItemsProperty.propertyPath.Replace(".internalItems", ""));

            if (parentProperty != null)
            {
                EqualizeAllChances(parentProperty);
            }
        }

        private void SetAllLocked_ContextMenu(SerializedProperty internalItemsProperty, bool locked)
        {
            if (internalItemsProperty.arraySize == 0) return;

            // Set all items to the specified lock state
            for (int i = 0; i < internalItemsProperty.arraySize; i++)
            {
                var element = internalItemsProperty.GetArrayElementAtIndex(i);
                var isLockedProp = element.FindPropertyRelative("isLocked");
                isLockedProp.boolValue = locked;
            }

            internalItemsProperty.serializedObject.ApplyModifiedProperties();

            // Trigger normalization
            TriggerNormalization(internalItemsProperty);
        }

        private void TriggerNormalization(SerializedProperty internalItemsProperty)
        {
            // Apply changes immediately
            internalItemsProperty.serializedObject.ApplyModifiedProperties();

            // Force normalization by calling it on the target object
            var targetObject = internalItemsProperty.serializedObject.targetObject;
            if (targetObject != null)
            {
                EditorUtility.SetDirty(targetObject);

                // Use reflection to find and call NormalizeChances on the ChanceList
                var targetType = targetObject.GetType();
                var fields = targetType.GetFields(BindingFlags.NonPublic |
                                                  BindingFlags.Public |
                                                  BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType.IsGenericType &&
                        field.FieldType.GetGenericTypeDefinition() == typeof(ChanceList<>))
                    {
                        var chanceListInstance = field.GetValue(targetObject);
                        if (chanceListInstance != null)
                        {
                            var normalizeMethod = field.FieldType.GetMethod("NormalizeChances");
                            normalizeMethod?.Invoke(chanceListInstance, null);
                        }
                    }
                }

                // Update the serialized object to reflect changes
                internalItemsProperty.serializedObject.Update();
            }
        }
    }
}
#endif