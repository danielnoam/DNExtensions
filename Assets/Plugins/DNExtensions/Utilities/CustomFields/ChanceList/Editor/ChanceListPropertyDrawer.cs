#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(ChanceList<>), true)]
    public class ChanceListPropertyDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, ReorderableList> _lists = new();
        private readonly Dictionary<string, bool> _isDragging = new();
        private readonly Dictionary<string, int> _dragValues = new();

        private const float ElementHeight = 3f;
        private const float ItemWidthRatio = 0.55f;
        private const float IntFieldWidth = 30f;
        private const float LockButtonWidth = 20f;
        private const float Spacing = 5f;

        private ReorderableList GetList(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (_lists.TryGetValue(key, out var list)) return list;

            var internalItemsProperty = property.FindPropertyRelative("internalItems");
            if (internalItemsProperty == null) return null;

            list = new ReorderableList(property.serializedObject, internalItemsProperty, true, false, true, true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) => DrawElement(rect, index, property),
                elementHeight = EditorGUIUtility.singleLineHeight + ElementHeight,
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                drawHeaderCallback = null
            };

            _lists[key] = list;
            return list;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var list = GetList(property);
            if (list == null) return;

            EditorGUI.BeginProperty(position, label, property);

            var internalItemsProperty = property.FindPropertyRelative("internalItems");

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (Event.current.type == EventType.ContextClick && foldoutRect.Contains(Event.current.mousePosition))
            {
                ShowContextMenu(property);
                Event.current.Use();
            }

            var sizeWidth = GUI.skin.textField.CalcSize(new GUIContent(internalItemsProperty.arraySize.ToString())).x + 35f;
            var foldoutWidth = position.width - sizeWidth - 5f;

            var actualFoldoutRect = new Rect(foldoutRect.x, foldoutRect.y, foldoutWidth, foldoutRect.height);
            var sizeFieldRect = new Rect(actualFoldoutRect.xMax + 5f, foldoutRect.y, sizeWidth, foldoutRect.height);

            var boldFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            property.isExpanded = EditorGUI.Foldout(actualFoldoutRect, property.isExpanded, label.text, true, boldFoldout);

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUI.DelayedIntField(sizeFieldRect, internalItemsProperty.arraySize);
            if (EditorGUI.EndChangeCheck())
            {
                internalItemsProperty.arraySize = Mathf.Max(0, newSize);

                for (int i = 0; i < internalItemsProperty.arraySize; i++)
                {
                    var element = internalItemsProperty.GetArrayElementAtIndex(i);
                    var chanceProp = element.FindPropertyRelative("chance");
                    var lockedProp = element.FindPropertyRelative("isLocked");
                    if (chanceProp.intValue == 0 && !lockedProp.boolValue)
                        chanceProp.intValue = 10;
                }

                TriggerNormalization(property, internalItemsProperty);
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var contentStartY = position.y + EditorGUIUtility.singleLineHeight + 2f;

                if (internalItemsProperty.arraySize == 0)
                {
                    var backgroundRect = new Rect(position.x, contentStartY, position.width, EditorGUIUtility.singleLineHeight * 2f);
                    GUI.Box(backgroundRect, "", "RL Background");

                    var headerRect = new Rect(backgroundRect.x, backgroundRect.y, backgroundRect.width, EditorGUIUtility.singleLineHeight);
                    GUI.Box(headerRect, "", "RL Header");
                    DrawHeader(headerRect);

                    var emptyRect = new Rect(backgroundRect.x + 6f, headerRect.yMax, backgroundRect.width - 12f, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(emptyRect, "List is Empty", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic });

                    var buttonGroupRect = new Rect(backgroundRect.xMax - 71f, backgroundRect.yMax + 1f, 60f, 20f);
                    GUI.Box(buttonGroupRect, "", "RL Footer");

                    if (GUI.Button(new Rect(buttonGroupRect.x, buttonGroupRect.y, 30f, 16f), "+", "RL FooterButton"))
                        OnAdd(list);

                    EditorGUI.BeginDisabledGroup(true);
                    GUI.Button(new Rect(buttonGroupRect.x + 30f, buttonGroupRect.y, 30f, 16f), "-", "RL FooterButton");
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    var headerRect = new Rect(position.x, contentStartY, position.width, EditorGUIUtility.singleLineHeight);
                    GUI.Box(headerRect, "", "RL Header");
                    DrawHeader(headerRect);

                    var listRect = new Rect(position.x, headerRect.yMax, position.width, list.GetHeight());

                    if (Event.current.type == EventType.ContextClick && listRect.Contains(Event.current.mousePosition))
                    {
                        ShowContextMenu(property);
                        Event.current.Use();
                    }

                    list.DoList(listRect);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var list = GetList(property);
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded) return height;

            var internalItemsProperty = property.FindPropertyRelative("internalItems");

            if (internalItemsProperty.arraySize == 0)
            {
                height += EditorGUIUtility.singleLineHeight + 2f;
                height += EditorGUIUtility.singleLineHeight;
                height += 20f;
            }
            else
            {
                height += EditorGUIUtility.singleLineHeight + 2f;
                if (list != null) height += list.GetHeight();
            }

            return height;
        }

        private void DrawHeader(Rect rect)
        {
            rect.x += EditorGUI.indentLevel * 15f;
            rect.width -= EditorGUI.indentLevel * 15f;

            var bold = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            var centered = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            GUI.Label(new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height), "Item", bold);
            GUI.Label(new Rect(rect.x + rect.width * ItemWidthRatio + 3f, rect.y, rect.width * 0.35f, rect.height), "Chance %", centered);
            GUI.Label(new Rect(rect.x + rect.width - LockButtonWidth, rect.y, LockButtonWidth, rect.height), "🔒", centered);
        }

        private void DrawElement(Rect rect, int index, SerializedProperty property)
        {
            var internalItemsProperty = property.FindPropertyRelative("internalItems");
            var element = internalItemsProperty.GetArrayElementAtIndex(index);

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;

            var itemProperty = element.FindPropertyRelative("item");
            var chanceProperty = element.FindPropertyRelative("chance");
            var isLockedProperty = element.FindPropertyRelative("isLocked");

            var itemRect = new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height);
            var lockButtonRect = new Rect(rect.x + rect.width - LockButtonWidth, rect.y, LockButtonWidth, rect.height);
            var intFieldRect = new Rect(lockButtonRect.x - Spacing - IntFieldWidth, rect.y, IntFieldWidth, rect.height);
            var sliderRect = new Rect(itemRect.xMax + Spacing, rect.y, intFieldRect.x - itemRect.xMax - (Spacing * 2), rect.height);

            EditorGUI.PropertyField(itemRect, itemProperty, GUIContent.none);

            EditorGUI.BeginDisabledGroup(isLockedProperty.boolValue);

            string dragKey = $"{property.propertyPath}_{index}";

            EditorGUI.BeginChangeCheck();
            float newChance = GUI.HorizontalSlider(sliderRect, chanceProperty.intValue, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                int rounded = Mathf.RoundToInt(newChance);
                chanceProperty.intValue = rounded;
                _dragValues[dragKey] = rounded;
                _isDragging[dragKey] = true;
                internalItemsProperty.serializedObject.ApplyModifiedProperties();
            }

            if (_isDragging.TryGetValue(dragKey, out bool dragging) && dragging && GUIUtility.hotControl == 0)
            {
                _isDragging[dragKey] = false;
                if (_dragValues.TryGetValue(dragKey, out int savedValue))
                {
                    chanceProperty.intValue = savedValue;
                    internalItemsProperty.serializedObject.ApplyModifiedProperties();
                }
                TriggerNormalization(property, internalItemsProperty, index);
            }

            EditorGUI.BeginChangeCheck();
            int intValue = EditorGUI.IntField(intFieldRect, chanceProperty.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProperty.intValue = Mathf.Clamp(intValue, 0, 100);
                TriggerNormalization(property, internalItemsProperty, index);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            bool isLocked = EditorGUI.Toggle(lockButtonRect,
                new GUIContent("", isLockedProperty.boolValue ? "Chance is locked" : "Chance is unlocked"),
                isLockedProperty.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isLockedProperty.boolValue = isLocked;
                TriggerNormalization(property, internalItemsProperty);
            }
        }

        private void OnAdd(ReorderableList list)
        {
            list.serializedProperty.arraySize++;
            var newElement = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
            newElement.FindPropertyRelative("chance").intValue = 10;
            newElement.FindPropertyRelative("isLocked").boolValue = false;

            var so = list.serializedProperty.serializedObject;
            so.ApplyModifiedProperties();
            InvokeNormalize(so.targetObject);
            so.Update();
        }

        private void OnRemove(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            var so = list.serializedProperty.serializedObject;
            so.ApplyModifiedProperties();
            InvokeNormalize(so.targetObject);
            so.Update();
        }

        private void ShowContextMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var internalItemsProperty = property.FindPropertyRelative("internalItems");

            menu.AddItem(new GUIContent("Equalize All Chances"), false, () =>
            {
                property.serializedObject.ApplyModifiedProperties();
                InvokeNormalize(property.serializedObject.targetObject, equalize: true);
                property.serializedObject.Update();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Lock All"), false, () => SetAllLocked(property, internalItemsProperty, true));
            menu.AddItem(new GUIContent("Unlock All"), false, () => SetAllLocked(property, internalItemsProperty, false));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Copy Property Path"), false,
                () => EditorGUIUtility.systemCopyBuffer = property.propertyPath);

            menu.ShowAsContext();
        }

        private void SetAllLocked(SerializedProperty property, SerializedProperty internalItemsProperty, bool locked)
        {
            for (int i = 0; i < internalItemsProperty.arraySize; i++)
                internalItemsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("isLocked").boolValue = locked;

            TriggerNormalization(property, internalItemsProperty);
        }

        private void TriggerNormalization(SerializedProperty property, SerializedProperty internalItemsProperty, int excludeIndex = -1)
        {
            if (excludeIndex >= 0 && excludeIndex < internalItemsProperty.arraySize)
            {
                // Calculate how much room is left after locked items
                int lockedTotal = 0;
                for (int i = 0; i < internalItemsProperty.arraySize; i++)
                {
                    var el = internalItemsProperty.GetArrayElementAtIndex(i);
                    if (i != excludeIndex && el.FindPropertyRelative("isLocked").boolValue)
                        lockedTotal += el.FindPropertyRelative("chance").intValue;
                }

                int maxAllowed = Mathf.Max(0, 100 - lockedTotal);
                var excluded = internalItemsProperty.GetArrayElementAtIndex(excludeIndex);
                var excludedChance = excluded.FindPropertyRelative("chance");
                excludedChance.intValue = Mathf.Clamp(excludedChance.intValue, 0, maxAllowed);

                excluded.FindPropertyRelative("isLocked").boolValue = true;
            }

            internalItemsProperty.serializedObject.ApplyModifiedProperties();

            InvokeNormalize(internalItemsProperty.serializedObject.targetObject);

            internalItemsProperty.serializedObject.Update();

            if (excludeIndex >= 0 && excludeIndex < internalItemsProperty.arraySize)
            {
                internalItemsProperty.GetArrayElementAtIndex(excludeIndex)
                    .FindPropertyRelative("isLocked").boolValue = false;
            }

            internalItemsProperty.serializedObject.ApplyModifiedProperties();
        }

        private static void InvokeNormalize(UnityEngine.Object target, bool equalize = false)
        {
            if (!target) return;

            EditorUtility.SetDirty(target);

            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            foreach (var field in target.GetType().GetFields(flags))
            {
                if (!field.FieldType.IsGenericType) continue;
                if (field.FieldType.GetGenericTypeDefinition() != typeof(ChanceList<>)) continue;

                var instance = field.GetValue(target);
                if (instance == null) continue;

                if (equalize)
                {
                    var itemsField = field.FieldType.GetField("internalItems", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (itemsField?.GetValue(instance) is System.Collections.IList list)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            var chanceField = item.GetType().GetField("chance");
                            var lockedField = item.GetType().GetField("isLocked");
                            if (chanceField != null) chanceField.SetValue(item, 0);
                            if (lockedField != null) lockedField.SetValue(item, false);
                            list[i] = item;
                        }
                    }
                }

                field.FieldType.GetMethod("NormalizeChances")?.Invoke(instance, null);
            }
        }
    }
}
#endif