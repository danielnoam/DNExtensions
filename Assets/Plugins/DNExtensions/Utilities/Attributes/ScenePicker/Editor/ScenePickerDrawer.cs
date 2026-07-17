using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;

namespace DNExtensions.Utilities
{
    [CustomPropertyDrawer(typeof(ScenePickerAttribute))]
    internal class ScenePickerDrawer : PropertyDrawer
    {
        private const float PickRadius = 50f;
        private const float ButtonWidth = 22f;
        private const float HandleRadius = 3f;
        private const float HandleLabelHeight = 2f;

        private static readonly Color PickingButtonColor = Color.yellow;
        private static readonly Color DefaultButtonColor = Color.white;
        private static readonly Color HoveredHandleColor = Color.green;
        private static readonly Color DefaultHandleColor = new Color(1f, 1f, 1f, 0.4f);

        private const string NoDirectorWarning = "[ScenePicker] No PlayableDirector found.";
        private const string UndoAssignLabel = "Assign Scene Pick";
        private const string UndoBindingLabel = "Assign Binding";

        private static readonly GUIContent CompactButtonContent = EditorGUIUtility.IconContent("d_Search Icon", "Pick from Scene");

        private static UnityEngine.Object _activeTarget;
        private static string _activePropertyPath;
        private static UnityEngine.Object _pendingAssignment;
        private static bool _scenePickerActive;
        private static UnityEngine.Object[] _cachedObjects;

        private bool IsExposedRef => fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ExposedReference<>);
        private bool IsChanceList => fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ChanceList<>);
        private bool IsGenericList => fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>);
        private bool IsArray => fieldInfo.FieldType.IsArray;

        private Type PickType
        {
            get
            {
                var ft = fieldInfo.FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(ExposedReference<>))
                    return ft.GetGenericArguments()[0];
                if (ft.IsGenericType)
                {
                    var arg = ft.GetGenericArguments()[0];
                    if (arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(ExposedReference<>))
                        return arg.GetGenericArguments()[0];
                }
                return typeof(UnityEngine.Object);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsChanceList)
                return EditorGUI.GetPropertyHeight(property, label)
                       + EditorGUIUtility.standardVerticalSpacing
                       + EditorGUIUtility.singleLineHeight;

            if (IsExposedRef)
                return EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

            float height = 0f;
            var iter = property.Copy();
            var end = property.GetEndProperty();
            if (iter.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iter, end))
                {
                    height += EditorGUI.GetPropertyHeight(iter, true) + EditorGUIUtility.standardVerticalSpacing;
                    if (!iter.NextVisible(false)) break;
                }
            }

            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
                                                     + height
                                                     + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_scenePickerActive && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                StopPicking();
                return;
            }

            bool isThisProperty = _activeTarget == property.serializedObject.targetObject &&
                                  _activePropertyPath == property.propertyPath;

            if (isThisProperty && _pendingAssignment)
            {
                if (IsChanceList) ApplyListAssignment(property, _pendingAssignment);
                else if (IsGenericList || IsArray) ApplyArrayAssignment(property, _pendingAssignment);
                else ApplySingleAssignment(property, _pendingAssignment);
                _pendingAssignment = null;
                StopPicking();
            }

            if (IsChanceList) DrawChanceList(position, property, label, isThisProperty);
            else if (IsGenericList || IsArray) DrawList(position, property, label, isThisProperty);
            else DrawSingle(position, property, label, isThisProperty);
        }

        private void DrawChanceList(Rect position, SerializedProperty property, GUIContent label, bool isThisProperty)
        {
            float listHeight = EditorGUI.GetPropertyHeight(property, label);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, listHeight), property, label, true);

            float buttonY = position.y + listHeight + EditorGUIUtility.standardVerticalSpacing;
            DrawPickButton(new Rect(position.x, buttonY, position.width, EditorGUIUtility.singleLineHeight), property, isThisProperty);
        }

        private void DrawSingle(Rect position, SerializedProperty property, GUIContent label, bool isThisProperty)
        {
            var director = TimelineEditor.inspectedDirector;
            var exposedNameProp = property.FindPropertyRelative("exposedName");
            string nameStr = exposedNameProp.stringValue;

            UnityEngine.Object resolved = null;
            if (director && !string.IsNullOrEmpty(nameStr))
                resolved = director.GetReferenceValue(new PropertyName(nameStr), out _);

            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = new Rect(position.x, position.y, position.width - ButtonWidth - 2f, position.height);
            var newValue = EditorGUI.ObjectField(fieldRect, label, resolved, PickType, true);

            DrawPickButton(new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, position.height), property, isThisProperty);

            if (newValue != resolved && director)
            {
                Undo.RecordObject(director, UndoBindingLabel);
                if (string.IsNullOrEmpty(nameStr))
                {
                    nameStr = Guid.NewGuid().ToString();
                    exposedNameProp.stringValue = nameStr;
                    property.serializedObject.ApplyModifiedProperties();
                }
                director.SetReferenceValue(new PropertyName(nameStr), newValue);
                EditorUtility.SetDirty(director);
            }

            EditorGUI.EndProperty();
        }

        private void DrawList(Rect position, SerializedProperty property, GUIContent label, bool isThisProperty)
        {
            float y = position.y;

            var foldoutRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                var iter = property.Copy();
                var end = property.GetEndProperty();
                if (iter.NextVisible(true))
                {
                    while (!SerializedProperty.EqualContents(iter, end))
                    {
                        float h = EditorGUI.GetPropertyHeight(iter, true);
                        EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iter, true);
                        y += h + EditorGUIUtility.standardVerticalSpacing;
                        if (!iter.NextVisible(false)) break;
                    }
                }
            }

            DrawPickButton(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), property, isThisProperty);
        }

        private void DrawPickButton(Rect rect, SerializedProperty property, bool isThisProperty)
        {
            bool isPicking = isThisProperty && _scenePickerActive;
            var prevColor = GUI.color;
            GUI.color = isPicking ? PickingButtonColor : DefaultButtonColor;

            if (GUI.Button(rect, CompactButtonContent))
            {
                if (isPicking)
                {
                    StopPicking();
                }
                else
                {
                    _activeTarget = property.serializedObject.targetObject;
                    _activePropertyPath = property.propertyPath;
                    _cachedObjects = UnityEngine.Object.FindObjectsByType(PickType);
                    _scenePickerActive = true;
                    SceneView.duringSceneGui += OnSceneGUI;
                    SceneView.RepaintAll();
                }
            }

            GUI.color = prevColor;
        }

        private static void ApplySingleAssignment(SerializedProperty property, UnityEngine.Object obj)
        {
            var director = TimelineEditor.inspectedDirector;
            if (!director) return;

            var exposedNameProp = property.FindPropertyRelative("exposedName");
            string nameStr = exposedNameProp.stringValue;

            Undo.RecordObject(property.serializedObject.targetObject, UndoAssignLabel);
            Undo.RecordObject(director, UndoAssignLabel);

            if (string.IsNullOrEmpty(nameStr))
            {
                nameStr = Guid.NewGuid().ToString();
                exposedNameProp.stringValue = nameStr;
            }

            director.SetReferenceValue(new PropertyName(nameStr), obj);
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            EditorUtility.SetDirty(director);
        }

        private static void ApplyArrayAssignment(SerializedProperty property, UnityEngine.Object obj)
        {
            var director = TimelineEditor.inspectedDirector;
            if (!director) { Debug.LogWarning(NoDirectorWarning); return; }

            for (int i = 0; i < property.arraySize; i++)
            {
                var nameProp = property.GetArrayElementAtIndex(i).FindPropertyRelative("exposedName");
                if (director.GetReferenceValue(new PropertyName(nameProp.stringValue), out _) == obj)
                    return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, UndoAssignLabel);
            Undo.RecordObject(director, UndoAssignLabel);

            int index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            var newEntry = property.GetArrayElementAtIndex(index);
            newEntry.FindPropertyRelative("exposedName").stringValue = string.Empty;
            BindExposedReference(newEntry, obj, director);

            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            EditorUtility.SetDirty(director);
        }

        private static void ApplyListAssignment(SerializedProperty property, UnityEngine.Object obj)
        {
            var director = TimelineEditor.inspectedDirector;
            if (!director)
            {
                Debug.LogWarning(NoDirectorWarning);
                return;
            }

            var internalItems = property.FindPropertyRelative("internalItems");

            for (int i = 0; i < internalItems.arraySize; i++)
            {
                var existingNameProp = internalItems.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("item")
                    .FindPropertyRelative("exposedName");
                if (director.GetReferenceValue(new PropertyName(existingNameProp.stringValue), out _) == obj)
                    return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, UndoAssignLabel);
            Undo.RecordObject(director, UndoAssignLabel);

            int index = internalItems.arraySize;
            internalItems.InsertArrayElementAtIndex(index);
            var newElement = internalItems.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("chance").intValue = 10;
            newElement.FindPropertyRelative("isLocked").boolValue = false;
            var itemProp = newElement.FindPropertyRelative("item");
            itemProp.FindPropertyRelative("exposedName").stringValue = string.Empty;

            BindExposedReference(itemProp, obj, director);

            property.serializedObject.ApplyModifiedProperties();
            ChanceListPropertyDrawer.NormalizeViaSp(internalItems);
            property.serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(property.serializedObject.targetObject);
            EditorUtility.SetDirty(director);
        }

        private static void BindExposedReference(SerializedProperty exposedRefProp, UnityEngine.Object obj, PlayableDirector director)
        {
            var exposedNameProp = exposedRefProp.FindPropertyRelative("exposedName");
            string nameStr = exposedNameProp.stringValue;
            if (string.IsNullOrEmpty(nameStr))
            {
                nameStr = Guid.NewGuid().ToString();
                exposedNameProp.stringValue = nameStr;
            }
            director.SetReferenceValue(new PropertyName(nameStr), obj);
        }

        private static void StopPicking()
        {
            _activeTarget = null;
            _activePropertyPath = null;
            _scenePickerActive = false;
            _cachedObjects = null;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (_cachedObjects == null) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var e = Event.current;
            UnityEngine.Object hovered = null;
            float closestDist = float.MaxValue;

            foreach (var obj in _cachedObjects)
            {
                if (obj is not Component comp) continue;
                float dist = Vector2.Distance(HandleUtility.WorldToGUIPoint(comp.transform.position), e.mousePosition);
                if (dist < closestDist) { closestDist = dist; hovered = obj; }
            }

            bool canSelect = hovered != null && closestDist < PickRadius;

            foreach (var obj in _cachedObjects)
            {
                if (obj is not Component comp) continue;
                Handles.color = obj == hovered && canSelect ? HoveredHandleColor : DefaultHandleColor;
                Handles.DrawWireDisc(comp.transform.position, Vector3.up, HandleRadius);
                Handles.Label(comp.transform.position + Vector3.up * HandleLabelHeight, comp.name);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && canSelect)
            {
                _pendingAssignment = hovered;
                e.Use();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                StopPicking();
            }

            HandleUtility.Repaint();
        }
    }
}
