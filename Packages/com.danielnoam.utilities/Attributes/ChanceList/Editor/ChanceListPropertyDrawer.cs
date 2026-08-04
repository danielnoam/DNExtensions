using System.Collections.Generic;
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

        private const float ItemWidthRatio = 0.55f;
        private const float ChanceRowHeight = 18f;
        private const float IntFieldWidth = 35f;
        private const float LockButtonWidth = 20f;
        private const float Spacing = 4f;
        private const float ElementPadding = 3f;

        private ReorderableList GetOrCreateList(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (_lists.TryGetValue(key, out var existing)) return existing;

            var itemsProp = property.FindPropertyRelative("internalItems");
            var list = new ReorderableList(property.serializedObject, itemsProp, true, true, true, true)
            {
                drawHeaderCallback = rect => DrawHeader(rect, property),
                drawElementCallback = (rect, index, _, _) => DrawElement(rect, index, property),
                elementHeightCallback = index => GetElementHeight(index, itemsProp),
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
            };

            _lists[key] = list;
            return list;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float foldoutHeight = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, foldoutHeight),
                property.isExpanded, label, true);

            if (property.isExpanded)
            {
                var list = GetOrCreateList(property);
                list.DoList(new Rect(position.x, position.y + foldoutHeight + 2f, position.width, list.GetHeight()));
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
            return EditorGUIUtility.singleLineHeight + 2f + GetOrCreateList(property).GetHeight();
        }

        private static void DrawHeader(Rect rect, SerializedProperty property)
        {
            var bold = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            var centered = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            float lockX = rect.xMax - LockButtonWidth;
            float intX = lockX - Spacing - IntFieldWidth;
            float sliderX = rect.x + rect.width * ItemWidthRatio + Spacing;
            float chanceWidth = lockX - sliderX - Spacing;

            GUI.Label(new Rect(rect.x, rect.y, rect.width * ItemWidthRatio, rect.height), "Item", bold);
            GUI.Label(new Rect(sliderX, rect.y, chanceWidth + IntFieldWidth + Spacing, rect.height), "Chance %", centered);
            GUI.Label(new Rect(lockX, rect.y, LockButtonWidth, rect.height), "🔒", centered);

            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                ShowContextMenu(property);
                Event.current.Use();
            }
        }

        private static void ShowContextMenu(SerializedProperty property)
        {
            var itemsProp = property.FindPropertyRelative("internalItems");
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Equalize All"), false, () =>
            {
                for (int i = 0; i < itemsProp.arraySize; i++)
                {
                    var el = itemsProp.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("isLocked").boolValue = false;
                    el.FindPropertyRelative("chance").intValue = 0;
                }
                itemsProp.serializedObject.ApplyModifiedProperties();
                NormalizeViaSp(itemsProp);
                itemsProp.serializedObject.ApplyModifiedProperties();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Lock All"), false, () =>
            {
                for (int i = 0; i < itemsProp.arraySize; i++)
                    itemsProp.GetArrayElementAtIndex(i).FindPropertyRelative("isLocked").boolValue = true;
                itemsProp.serializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(new GUIContent("Unlock All"), false, () =>
            {
                for (int i = 0; i < itemsProp.arraySize; i++)
                    itemsProp.GetArrayElementAtIndex(i).FindPropertyRelative("isLocked").boolValue = false;
                ApplyAndNormalize(itemsProp);
            });

            menu.ShowAsContext();
        }

        private float GetElementHeight(int index, SerializedProperty itemsProp)
        {
            if (index >= itemsProp.arraySize) return EditorGUIUtility.singleLineHeight;
            var itemProp = itemsProp.GetArrayElementAtIndex(index).FindPropertyRelative("item");
            float itemHeight = EditorGUI.GetPropertyHeight(itemProp, true);
            return Mathf.Max(itemHeight, ChanceRowHeight) + ElementPadding * 2f;
        }

        private void DrawElement(Rect rect, int index, SerializedProperty property)
        {
            var itemsProp = property.FindPropertyRelative("internalItems");
            var element = itemsProp.GetArrayElementAtIndex(index);
            var itemProp = element.FindPropertyRelative("item");
            var chanceProp = element.FindPropertyRelative("chance");
            var isLockedProp = element.FindPropertyRelative("isLocked");

            float itemHeight = EditorGUI.GetPropertyHeight(itemProp, true);
            float contentY = rect.y + ElementPadding;

            float lockX = rect.xMax - LockButtonWidth;
            float intX = lockX - Spacing - IntFieldWidth;
            float sliderX = rect.x + rect.width * ItemWidthRatio + Spacing;

            var itemRect = new Rect(rect.x, contentY, rect.width * ItemWidthRatio, itemHeight);

            // Chance controls anchor to the top of the element row
            float chanceY = contentY + (itemHeight - ChanceRowHeight) * 0.5f; // vertically center relative to item
            var sliderRect = new Rect(sliderX, chanceY, intX - sliderX - Spacing, ChanceRowHeight);
            var intRect = new Rect(intX, chanceY, IntFieldWidth, ChanceRowHeight);
            var lockRect = new Rect(lockX, chanceY, LockButtonWidth, ChanceRowHeight);

            EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none, true);

            string dragKey = $"{property.propertyPath}_{index}";

            EditorGUI.BeginDisabledGroup(isLockedProp.boolValue);

            EditorGUI.BeginChangeCheck();
            float sliderVal = GUI.HorizontalSlider(sliderRect, chanceProp.intValue, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProp.intValue = Mathf.RoundToInt(sliderVal);
                _dragValues[dragKey] = chanceProp.intValue;
                _isDragging[dragKey] = true;
            }

            if (_isDragging.TryGetValue(dragKey, out bool dragging) && dragging && GUIUtility.hotControl == 0)
            {
                _isDragging[dragKey] = false;
                chanceProp.intValue = _dragValues.GetValueOrDefault(dragKey, chanceProp.intValue);
                ApplyAndNormalize(itemsProp, index);
            }

            EditorGUI.BeginChangeCheck();
            int intVal = EditorGUI.IntField(intRect, chanceProp.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                chanceProp.intValue = Mathf.Clamp(intVal, 0, 100);
                ApplyAndNormalize(itemsProp, index);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            bool locked = EditorGUI.Toggle(lockRect,
                new GUIContent("", isLockedProp.boolValue ? "Locked" : "Unlocked"),
                isLockedProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isLockedProp.boolValue = locked;
                ApplyAndNormalize(itemsProp);
            }
        }

        private void OnAdd(ReorderableList list)
        {
            list.serializedProperty.arraySize++;
            var newEl = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
            newEl.FindPropertyRelative("chance").intValue = 10;
            newEl.FindPropertyRelative("isLocked").boolValue = false;
            ApplyAndNormalize(list.serializedProperty);
        }

        private void OnRemove(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            ApplyAndNormalize(list.serializedProperty);
        }

        private static void ApplyAndNormalize(SerializedProperty itemsProp, int lockedIndex = -1)
        {
            itemsProp.serializedObject.ApplyModifiedProperties();
            NormalizeViaSp(itemsProp, lockedIndex);
            itemsProp.serializedObject.ApplyModifiedProperties();
        }

        public static void NormalizeViaSp(SerializedProperty itemsProp, int lockedIndex = -1)
        {
            int count = itemsProp.arraySize;
            if (count == 0) return;

            if (lockedIndex >= 0 && lockedIndex < count)
            {
                int otherLockedTotal = 0;
                for (int i = 0; i < count; i++)
                {
                    if (i == lockedIndex) continue;
                    var el = itemsProp.GetArrayElementAtIndex(i);
                    if (el.FindPropertyRelative("isLocked").boolValue)
                        otherLockedTotal += Mathf.Max(0, el.FindPropertyRelative("chance").intValue);
                }
                var editedChance = itemsProp.GetArrayElementAtIndex(lockedIndex).FindPropertyRelative("chance");
                editedChance.intValue = Mathf.Clamp(editedChance.intValue, 0, Mathf.Max(0, 100 - otherLockedTotal));
                itemsProp.GetArrayElementAtIndex(lockedIndex).FindPropertyRelative("isLocked").boolValue = true;
            }

            var unlocked = new List<int>();
            int lockedTotal = 0;

            for (int i = 0; i < count; i++)
            {
                var el = itemsProp.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("isLocked").boolValue)
                    lockedTotal += Mathf.Max(0, el.FindPropertyRelative("chance").intValue);
                else
                    unlocked.Add(i);
            }

            int remaining = Mathf.Max(0, 100 - lockedTotal);

            if (unlocked.Count > 0)
            {
                int unlockedTotal = 0;
                foreach (int i in unlocked)
                    unlockedTotal += Mathf.Max(0, itemsProp.GetArrayElementAtIndex(i).FindPropertyRelative("chance").intValue);

                if (unlockedTotal <= 0)
                {
                    int equal = remaining / unlocked.Count;
                    int rem = remaining % unlocked.Count;
                    for (int i = 0; i < unlocked.Count; i++)
                        itemsProp.GetArrayElementAtIndex(unlocked[i]).FindPropertyRelative("chance").intValue = equal + (i < rem ? 1 : 0);
                }
                else if (unlockedTotal != remaining)
                {
                    int newTotal = 0;
                    foreach (int i in unlocked)
                    {
                        var cp = itemsProp.GetArrayElementAtIndex(i).FindPropertyRelative("chance");
                        int val = Mathf.RoundToInt((cp.intValue / (float)unlockedTotal) * remaining);
                        cp.intValue = val;
                        newTotal += val;
                    }

                    int diff = remaining - newTotal;
                    if (diff != 0)
                    {
                        unlocked.Sort((a, b) =>
                            itemsProp.GetArrayElementAtIndex(b).FindPropertyRelative("chance").intValue
                            .CompareTo(itemsProp.GetArrayElementAtIndex(a).FindPropertyRelative("chance").intValue));

                        for (int i = 0; i < Mathf.Abs(diff) && i < unlocked.Count; i++)
                        {
                            var cp = itemsProp.GetArrayElementAtIndex(unlocked[i]).FindPropertyRelative("chance");
                            if (diff > 0) cp.intValue++;
                            else if (cp.intValue > 0) cp.intValue--;
                        }
                    }
                }
            }

            if (lockedIndex >= 0 && lockedIndex < count)
                itemsProp.GetArrayElementAtIndex(lockedIndex).FindPropertyRelative("isLocked").boolValue = false;
        }
    }
}