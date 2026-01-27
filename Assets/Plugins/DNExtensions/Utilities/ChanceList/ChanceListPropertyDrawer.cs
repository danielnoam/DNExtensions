#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(ChanceList<>), true)]
    public class ChanceListPropertyDrawer : PropertyDrawer
    {
        private ReorderableList _reorderableList;
        private bool _isInitialized;
        
        // Layout configuration
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

            _reorderableList = new ReorderableList(property.serializedObject, internalItemsProperty, true, false, true, true)
            {
                drawElementCallback = DrawElement,
                elementHeight = EditorGUIUtility.singleLineHeight + ElementHeight,
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                drawHeaderCallback = null // We'll handle the header ourselves
            };

            _isInitialized = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeList(property);
            if (_reorderableList == null) return;

            EditorGUI.BeginProperty(position, label, property);

            var internalItemsProperty = property.FindPropertyRelative("internalItems");
            
            // Create foldout header layout like Unity's default
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            // Handle right-click context menu on foldout
            if (Event.current.type == EventType.ContextClick && foldoutRect.Contains(Event.current.mousePosition))
            {
                ShowEnhancedContextMenu(property);
                Event.current.Use();
            }

            // Calculate the width for the foldout text and size field
            var sizeText = internalItemsProperty.arraySize.ToString();
            var sizeWidth = GUI.skin.textField.CalcSize(new GUIContent(sizeText)).x + 35f;
            
            // Foldout takes most of the space, size field on the right
            var foldoutWidth = position.width - sizeWidth - 5f;
            var actualFoldoutRect = new Rect(foldoutRect.x, foldoutRect.y, foldoutWidth, foldoutRect.height);
            var sizeFieldRect = new Rect(actualFoldoutRect.xMax + 5f, foldoutRect.y, sizeWidth, foldoutRect.height);

            // Draw foldout with bold label and hover effect (no size in title)
            var boldFoldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            // Handle hover effect like Unity's default lists
            bool isHovering = actualFoldoutRect.Contains(Event.current.mousePosition);

            // Unity's default hover behavior - only show on mouse move and repaint events
            if (isHovering)
            {
                // Request repaint to ensure smooth hover effect
                if (Event.current.type == EventType.MouseMove)
                {
                    Event.current.Use();
                    GUI.changed = true;
                }
            }

            // Draw the foldout
            property.isExpanded = EditorGUI.Foldout(actualFoldoutRect, property.isExpanded, label.text, true, boldFoldoutStyle);

            // Draw size field on the right
            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.DelayedIntField(sizeFieldRect, internalItemsProperty.arraySize);
            if (EditorGUI.EndChangeCheck())
            {
                internalItemsProperty.arraySize = Mathf.Max(0, newSize);
                
                // Initialize new elements with default values
                for (int i = 0; i < internalItemsProperty.arraySize; i++)
                {
                    var element = internalItemsProperty.GetArrayElementAtIndex(i);
                    var chanceProperty = element.FindPropertyRelative("chance");
                    var isLockedProperty = element.FindPropertyRelative("isLocked");
                    
                    if (chanceProperty.intValue == 0 && !isLockedProperty.boolValue)
                    {
                        chanceProperty.intValue = 10;
                        isLockedProperty.boolValue = false;
                    }
                }
                
                TriggerNormalization(internalItemsProperty);
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Calculate the content area for the list background
                var contentStartY = position.y + EditorGUIUtility.singleLineHeight + 2f;
                var backgroundRect = new Rect(position.x, contentStartY, position.width, 0f);
                
                if (internalItemsProperty.arraySize == 0)
                {
                    // Calculate background height for empty list (header + empty message only)
                    var headerHeight = EditorGUIUtility.singleLineHeight;
                    var emptyMessageHeight = EditorGUIUtility.singleLineHeight;
                    var footerHeight = 18; // Standard ReorderableList footer height
                    backgroundRect.height = headerHeight + emptyMessageHeight;
                    
                    // Draw background (doesn't include footer)
                    var backgroundStyle = "RL Background";
                    GUI.Box(backgroundRect, "", backgroundStyle);
                    
                    // Draw header for empty list
                    var headerRect = new Rect(backgroundRect.x, backgroundRect.y, backgroundRect.width, headerHeight);
                    var headerBackgroundStyle = "RL Header";
                    GUI.Box(headerRect, "", headerBackgroundStyle);
                    DrawHeader(headerRect);
                    
                    // Show "List is Empty" message (left-aligned like Unity's default)
                    var emptyRect = new Rect(backgroundRect.x + 6f, headerRect.yMax, backgroundRect.width - 12f, emptyMessageHeight);
                    var emptyStyle = new GUIStyle(EditorStyles.label) 
                    { 
                        fontStyle = FontStyle.Italic,
                        alignment = TextAnchor.MiddleLeft
                    };
                    EditorGUI.LabelField(emptyRect, "List is Empty", emptyStyle);
                    
                    // Draw footer with add/remove buttons below the background
                    var footerRect = new Rect(backgroundRect.x, backgroundRect.yMax, backgroundRect.width, footerHeight);
                    
                    // Create button group background (darker area like Unity's default)
                    var buttonGroupWidth = 60f;
                    var buttonGroupHeight = 20f;
                    var buttonGroupRect = new Rect(footerRect.xMax - buttonGroupWidth - 11f, footerRect.y + (footerRect.height - buttonGroupHeight) / 2f, buttonGroupWidth, buttonGroupHeight);
                    GUI.Box(buttonGroupRect, "", "RL Footer");
                    
                    // Add and remove buttons in the button group (+ first, then -)
                    var buttonWidth = 30f;
                    var buttonHeight = 16f;
                    var addButtonRect = new Rect(buttonGroupRect.x, buttonGroupRect.y, buttonWidth, buttonHeight);
                    var removeButtonRect = new Rect(buttonGroupRect.x + buttonWidth, buttonGroupRect.y, buttonWidth, buttonHeight);
                    
                    if (GUI.Button(addButtonRect, "+", "RL FooterButton"))
                    {
                        OnAdd(_reorderableList);
                    }
                    
                    EditorGUI.BeginDisabledGroup(true); // Always disabled when list is empty
                    GUI.Button(removeButtonRect, "-", "RL FooterButton");
                    EditorGUI.EndDisabledGroup();
                    
                }
                else
                {
                    // Draw header for the list elements with background
                    var headerRect = new Rect(position.x, contentStartY, position.width, EditorGUIUtility.singleLineHeight);
                    var headerBackgroundStyle = "RL Header";
                    GUI.Box(headerRect, "", headerBackgroundStyle);
                    DrawHeader(headerRect);
                    
                    // Draw the reorderable list without its own header
                    var listRect = new Rect(position.x, headerRect.y + EditorGUIUtility.singleLineHeight,
                        position.width, _reorderableList.GetHeight());
                    
                    // Handle right-click context menu on list area
                    if (Event.current.type == EventType.ContextClick && listRect.Contains(Event.current.mousePosition))
                    {
                        ShowEnhancedContextMenu(property);
                        Event.current.Use();
                    }
                    
                    _reorderableList.DoList(listRect);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitializeList(property);
            
            float height = EditorGUIUtility.singleLineHeight; // Foldout line
            
            if (property.isExpanded)
            {
                var internalItemsProperty = property.FindPropertyRelative("internalItems");
                
                if (internalItemsProperty.arraySize == 0)
                {
                    // Header + empty message + footer for empty list
                    height += EditorGUIUtility.singleLineHeight + 2f; // Header
                    height += EditorGUIUtility.singleLineHeight; // "List is Empty" message
                    height += 20f; // Footer with buttons
                }
                else
                {
                    height += EditorGUIUtility.singleLineHeight + 2f; // Header
                    if (_reorderableList != null)
                    {
                        height += _reorderableList.GetHeight();
                    }
                }
            }
            
            return height;
        }

        private void DrawHeader(Rect rect)
        {
            var headerStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold
            };

            var chanceHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            // Adjust for indent
            rect.x += EditorGUI.indentLevel * 15f;
            rect.width -= EditorGUI.indentLevel * 15f;

            var itemHeaderRect = new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height);
            var chanceHeaderRect = new Rect(rect.x + rect.width * ItemWidthRatio + 3f, rect.y, rect.width * 0.35f, rect.height);
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

            // Calculate rects
            var itemRect = new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height);
            var lockButtonRect = new Rect(rect.x + rect.width - LockButtonWidth, rect.y, LockButtonWidth, rect.height);
            var intFieldRect = new Rect(lockButtonRect.x - Spacing - IntFieldWidth, rect.y, IntFieldWidth, rect.height);
            var sliderRect = new Rect(itemRect.xMax + Spacing, rect.y, intFieldRect.x - itemRect.xMax - (Spacing * 2), rect.height);

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
            bool isLocked = EditorGUI.Toggle(lockButtonRect, new GUIContent("", lockTooltip), isLockedProperty.boolValue);
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

            var chanceProperty = newElement.FindPropertyRelative("chance");
            var isLockedProperty = newElement.FindPropertyRelative("isLocked");
            
            chanceProperty.intValue = 10;
            isLockedProperty.boolValue = false;

            TriggerNormalization(list.serializedProperty);
        }

        private void OnRemove(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            TriggerNormalization(list.serializedProperty);
        }

        private void ShowEnhancedContextMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var internalItemsProperty = property.FindPropertyRelative("internalItems");

            // Add Unity's default property context menu items
            menu.AddItem(new GUIContent("Copy Property Path"), false,
                () => { EditorGUIUtility.systemCopyBuffer = property.propertyPath; });

            menu.AddItem(new GUIContent("Copy"), false,
                () => { EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(property.serializedObject.targetObject); });

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

            menu.AddItem(new GUIContent("Paste"), false, () =>
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Paste Property");
                property.serializedObject.ApplyModifiedProperties();
                TriggerNormalization(internalItemsProperty);
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Properties..."), false, () =>
            {
                Selection.activeObject = property.serializedObject.targetObject;
                EditorGUIUtility.PingObject(property.serializedObject.targetObject);
            });

            menu.AddSeparator("");

            // ChanceList-specific options
            menu.AddItem(new GUIContent("Equalize All Chances"), false,
                () => EqualizeAllChances(property));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Lock All"), false,
                () => SetAllLocked(internalItemsProperty, true));
            menu.AddItem(new GUIContent("Unlock All"), false,
                () => SetAllLocked(internalItemsProperty, false));

            menu.ShowAsContext();
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

        private void SetAllLocked(SerializedProperty internalItemsProperty, bool locked)
        {
            if (internalItemsProperty.arraySize == 0) return;

            for (int i = 0; i < internalItemsProperty.arraySize; i++)
            {
                var element = internalItemsProperty.GetArrayElementAtIndex(i);
                var isLockedProp = element.FindPropertyRelative("isLocked");
                isLockedProp.boolValue = locked;
            }

            internalItemsProperty.serializedObject.ApplyModifiedProperties();
            TriggerNormalization(internalItemsProperty);
        }

        private void TriggerNormalization(SerializedProperty internalItemsProperty)
        {
            internalItemsProperty.serializedObject.ApplyModifiedProperties();

            var targetObject = internalItemsProperty.serializedObject.targetObject;
            if (targetObject != null)
            {
                EditorUtility.SetDirty(targetObject);

                var targetType = targetObject.GetType();
                var fields = targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(ChanceList<>))
                    {
                        var chanceListInstance = field.GetValue(targetObject);
                        if (chanceListInstance != null)
                        {
                            var normalizeMethod = field.FieldType.GetMethod("NormalizeChances");
                            normalizeMethod?.Invoke(chanceListInstance, null);
                        }
                    }
                }

                internalItemsProperty.serializedObject.Update();
            }
        }
    }
}
#endif