#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class BetterTransformEditor : Editor
    {
        private static bool scaleLocked = false;
        private Vector3 lastScale;

        private void OnEnable()
        {
            Transform t = target as Transform;
            if (t) lastScale = t.localScale;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawVector3Field("Position", "m_LocalPosition", Vector3.zero, null);
            DrawRotationField();
            DrawVector3Field("Scale", "m_LocalScale", Vector3.one, HandleScaleLock);

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    Transform t = obj as Transform;
                    if (t) EditorUtility.SetDirty(t);
                }
            }
        }

        private delegate void OnValueChanged(SerializedProperty prop, Vector3 oldValue, Vector3 newValue);

        private void DrawVector3Field(string label, string propertyName, Vector3 defaultValue, OnValueChanged onValueChanged)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop == null) return;

            Vector3 currentValue = prop.vector3Value;
            bool isWideMode = EditorGUIUtility.wideMode;
            bool isScale = propertyName == "m_LocalScale";
    
            EditorGUILayout.BeginHorizontal();
    
            if (isWideMode)
            {
                Rect position = EditorGUILayout.GetControlRect(true);
                DrawWideMode(position, label, prop, currentValue, isScale, onValueChanged, defaultValue, propertyName);
            }
            else
            {
                // Narrow mode: label + lock (if scale) + buttons all on one line
                DrawNarrowModeLabel(label, currentValue, isScale);
            }
    
            DrawButtons(currentValue, prop, label, defaultValue, propertyName);
    
            EditorGUILayout.EndHorizontal();
            
            if (!isWideMode)
            {
                DrawNarrowModeField(prop, currentValue, onValueChanged);
            }
        }

        private void DrawWideMode(Rect position, string label, SerializedProperty prop, Vector3 currentValue, bool isScale, OnValueChanged onValueChanged, Vector3 defaultValue, string propertyName)
        {
            float lockWidth = isScale ? 20 : 0;
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - lockWidth, position.height);
            Rect lockRect = isScale ? new Rect(labelRect.xMax, position.y, lockWidth, position.height) : Rect.zero;
            Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
            
            HandleContextClick(labelRect, currentValue, label);
            EditorGUI.LabelField(labelRect, label);
            
            if (isScale)
            {
                EditorGUIUtility.AddCursorRect(lockRect, MouseCursor.Link);
                scaleLocked = GUI.Toggle(lockRect, scaleLocked, 
                    EditorGUIUtility.IconContent(scaleLocked ? "Linked" : "Unlinked"), 
                    EditorStyles.label);
            }
            
            HandleContextClick(fieldRect, currentValue, label);
            
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(fieldRect, prop, GUIContent.none, true);
            EditorGUI.indentLevel = oldIndent;
            
            if (EditorGUI.EndChangeCheck())
            {
                onValueChanged?.Invoke(prop, currentValue, prop.vector3Value);
            }
        }

        private void DrawNarrowModeLabel(string label, Vector3 currentValue, bool isScale)
        {
            if (isScale)
            {
                Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
                HandleContextClick(labelRect, currentValue, label);
                GUI.Label(labelRect, label);
        
                Rect lockRect = GUILayoutUtility.GetRect(20, EditorGUIUtility.singleLineHeight, GUILayout.Width(20));
                EditorGUIUtility.AddCursorRect(lockRect, MouseCursor.Link);
                scaleLocked = GUI.Toggle(lockRect, scaleLocked, 
                    EditorGUIUtility.IconContent(scaleLocked ? "Linked" : "Unlinked"), 
                    EditorStyles.label);
            }
            else
            {
                Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.label, GUILayout.Width(EditorGUIUtility.labelWidth));
                HandleContextClick(labelRect, currentValue, label);
                GUI.Label(labelRect, label);
            }
    
            GUILayout.FlexibleSpace();
        }


        private void DrawNarrowModeField(SerializedProperty prop, Vector3 currentValue, OnValueChanged onValueChanged)
        {
            Rect fieldRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(prop));
            HandleContextClick(fieldRect, currentValue, prop.displayName);
    
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(fieldRect, prop, GUIContent.none);
    
            if (EditorGUI.EndChangeCheck())
            {
                onValueChanged?.Invoke(prop, currentValue, prop.vector3Value);
            }
        }

        private void HandleContextClick(Rect rect, Vector3 value, string label)
        {
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                ShowVectorContextMenu(value, label);
                Event.current.Use();
            }
        }

        private void DrawButtons(Vector3 currentValue, SerializedProperty prop, string label, Vector3 defaultValue, string propertyName)
        {
            if (GUILayout.Button(new GUIContent("C", "Copy"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
            {
                EditorGUIUtility.systemCopyBuffer = $"{currentValue.x},{currentValue.y},{currentValue.z}";
            }
            
            bool canPaste = CanPasteVector3();
            GUI.enabled = canPaste;
            if (GUILayout.Button(new GUIContent("P", "Paste"), EditorStyles.miniButtonMid, GUILayout.Width(20)))
            {
                PasteVector(prop, propertyName, label);
            }
            GUI.enabled = true;
            
            bool isDefault = currentValue == defaultValue;
            GUI.enabled = !isDefault;
            
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent("R"), EditorStyles.miniButtonRight, GUILayout.Width(20));
                if (buttonRect.Contains(Event.current.mousePosition))
                {
                    ShowResetContextMenu(propertyName);
                    Event.current.Use();
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent("R", $"Reset {label.ToLower()}"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    ResetVector(propertyName, defaultValue, label);
                }
            }
            
            GUI.enabled = true;
        }

private void DrawRotationField()
{
    Transform t = target as Transform;
    if (t == null) return;

    Vector3 eulerAngles = t.localEulerAngles;
    Quaternion quaternion = t.localRotation;
    bool isWideMode = EditorGUIUtility.wideMode;
    
    EditorGUILayout.BeginHorizontal();
    
    if (isWideMode)
    {
        Rect position = EditorGUILayout.GetControlRect(true);
        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
        
        HandleRotationContextClick(labelRect, eulerAngles, quaternion);
        EditorGUI.LabelField(labelRect, "Rotation");
        
        HandleRotationContextClick(fieldRect, eulerAngles, quaternion);
        
        EditorGUI.BeginChangeCheck();
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        Vector3 newEuler = EditorGUI.Vector3Field(fieldRect, GUIContent.none, eulerAngles);
        EditorGUI.indentLevel = oldIndent;
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(targets, "Rotation Changed");
            foreach (Object obj in targets)
            {
                Transform transform = obj as Transform;
                if (transform) transform.localEulerAngles = newEuler;
            }
        }
    }
    else
    {
        // Narrow mode: label + buttons on one line
        Rect labelRect = GUILayoutUtility.GetRect(new GUIContent("Rotation"), GUI.skin.label, GUILayout.Width(EditorGUIUtility.labelWidth));
        HandleRotationContextClick(labelRect, eulerAngles, quaternion);
        GUI.Label(labelRect, "Rotation");
        GUILayout.FlexibleSpace();
    }

    if (GUILayout.Button(new GUIContent("C", "Copy"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
    {
        EditorGUIUtility.systemCopyBuffer = $"{eulerAngles.x},{eulerAngles.y},{eulerAngles.z}";
    }

    bool canPaste = CanPasteVector3();
    GUI.enabled = canPaste;
    if (GUILayout.Button(new GUIContent("P", "Paste"), EditorStyles.miniButtonMid, GUILayout.Width(20)))
    {
        string[] values = EditorGUIUtility.systemCopyBuffer.Split(',');
        if (values.Length == 3 &&
            float.TryParse(values[0], out float x) &&
            float.TryParse(values[1], out float y) &&
            float.TryParse(values[2], out float z))
        {
            Undo.RecordObjects(targets, "Paste Rotation");
            foreach (Object obj in targets)
            {
                Transform transform = obj as Transform;
                if (transform != null) transform.localEulerAngles = new Vector3(x, y, z);
            }
        }
    }
    GUI.enabled = true;

    bool isDefault = eulerAngles == Vector3.zero;
    GUI.enabled = !isDefault;

    if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
    {
        Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent("R"), EditorStyles.miniButtonRight, GUILayout.Width(20));
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            ShowResetContextMenu("m_LocalEulerAnglesHint");
            Event.current.Use();
        }
    }
    else
    {
        if (GUILayout.Button(new GUIContent("R", "Reset rotation"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
        {
            Undo.RecordObjects(targets, "Reset Rotation");
            foreach (Object obj in targets)
            {
                Transform transform = obj as Transform;
                if (transform != null) transform.localEulerAngles = Vector3.zero;
            }
        }
    }

    GUI.enabled = true;
    EditorGUILayout.EndHorizontal();
    
    // Narrow mode: vector field on next line
    if (!isWideMode)
    {
        Rect fieldRect = EditorGUILayout.GetControlRect(true);
        HandleRotationContextClick(fieldRect, eulerAngles, quaternion);
        
        EditorGUI.BeginChangeCheck();
        Vector3 newEuler = EditorGUI.Vector3Field(fieldRect, GUIContent.none, eulerAngles);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(targets, "Rotation Changed");
            foreach (Object obj in targets)
            {
                Transform transform = obj as Transform;
                if (transform) transform.localEulerAngles = newEuler;
            }
        }
    }
}

        private void HandleScaleLock(SerializedProperty prop, Vector3 oldValue, Vector3 newValue)
        {
            if (scaleLocked && lastScale != Vector3.zero)
            {
                int changedAxis = -1;
                if (!Mathf.Approximately(newValue.x, oldValue.x)) changedAxis = 0;
                else if (!Mathf.Approximately(newValue.y, oldValue.y)) changedAxis = 1;
                else if (!Mathf.Approximately(newValue.z, oldValue.z)) changedAxis = 2;

                if (changedAxis != -1)
                {
                    float ratio = 1f;
                    if (changedAxis == 0 && !Mathf.Approximately(lastScale.x, 0)) 
                        ratio = newValue.x / lastScale.x;
                    else if (changedAxis == 1 && !Mathf.Approximately(lastScale.y, 0)) 
                        ratio = newValue.y / lastScale.y;
                    else if (changedAxis == 2 && !Mathf.Approximately(lastScale.z, 0)) 
                        ratio = newValue.z / lastScale.z;

                    newValue = lastScale * ratio;
                    prop.vector3Value = newValue;
                }
            }
            lastScale = newValue;
        }

        private void PasteVector(SerializedProperty prop, string propertyName, string label)
        {
            string[] values = EditorGUIUtility.systemCopyBuffer.Split(',');
            if (values.Length == 3 && 
                float.TryParse(values[0], out float x) && 
                float.TryParse(values[1], out float y) && 
                float.TryParse(values[2], out float z))
            {
                Vector3 newValue = new Vector3(x, y, z);
                Undo.RecordObjects(targets, $"Paste {label}");
                foreach (Object obj in targets)
                {
                    Transform t = obj as Transform;
                    if (t != null)
                    {
                        if (propertyName == "m_LocalPosition") t.localPosition = newValue;
                        else if (propertyName == "m_LocalScale") 
                        {
                            t.localScale = newValue;
                            lastScale = newValue;
                        }
                    }
                }
                serializedObject.Update();
            }
        }

        private void ResetVector(string propertyName, Vector3 defaultValue, string label)
        {
            Undo.RecordObjects(targets, $"Reset {label}");
            foreach (Object obj in targets)
            {
                Transform t = obj as Transform;
                if (t != null)
                {
                    if (propertyName == "m_LocalPosition") t.localPosition = defaultValue;
                    else if (propertyName == "m_LocalScale")
                    {
                        t.localScale = defaultValue;
                        lastScale = defaultValue;
                    }
                }
            }
            serializedObject.Update();
        }

        private void HandleRotationContextClick(Rect rect, Vector3 eulerAngles, Quaternion quaternion)
        {
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                ShowRotationContextMenu(eulerAngles, quaternion);
                Event.current.Use();
            }
        }

        private void ShowVectorContextMenu(Vector3 vector, string label)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Copy"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = $"{vector.x},{vector.y},{vector.z}";
            });
            
            menu.AddItem(new GUIContent("Copy X"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = vector.x.ToString();
            });
            
            menu.AddItem(new GUIContent("Copy Y"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = vector.y.ToString();
            });
            
            menu.AddItem(new GUIContent("Copy Z"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = vector.z.ToString();
            });
            
            menu.ShowAsContext();
        }

        private void ShowRotationContextMenu(Vector3 eulerAngles, Quaternion quaternion)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Copy"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = $"{eulerAngles.x},{eulerAngles.y},{eulerAngles.z}";
            });
            
            menu.AddItem(new GUIContent("Copy Quaternion"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = $"{quaternion.x},{quaternion.y},{quaternion.z},{quaternion.w}";
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Copy X"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = eulerAngles.x.ToString();
            });
            
            menu.AddItem(new GUIContent("Copy Y"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = eulerAngles.y.ToString();
            });
            
            menu.AddItem(new GUIContent("Copy Z"), false, () => 
            {
                EditorGUIUtility.systemCopyBuffer = eulerAngles.z.ToString();
            });
            
            menu.ShowAsContext();
        }

        private void ShowResetContextMenu(string propertyName)
        {
            GenericMenu menu = new GenericMenu();
            
            if (propertyName == "m_LocalPosition")
            {
                menu.AddItem(new GUIContent("Reset (Keep Children World Position)"), false, () => ResetPreservingChildren(propertyName));
            }
            else if (propertyName == "m_LocalEulerAnglesHint")
            {
                menu.AddItem(new GUIContent("Reset (Keep Children World Rotation)"), false, () => ResetPreservingChildren(propertyName));
            }
            else if (propertyName == "m_LocalScale")
            {
                menu.AddItem(new GUIContent("Reset (Keep Children World Scale)"), false, () => ResetPreservingChildren(propertyName));
            }
            
            menu.ShowAsContext();
        }

        private void ResetPreservingChildren(string propertyName)
        {
            foreach (Object obj in targets)
            {
                Transform transform = obj as Transform;
                if (transform == null) continue;

                int childCount = transform.childCount;
                Vector3[] worldPositions = new Vector3[childCount];
                Quaternion[] worldRotations = new Quaternion[childCount];
                Vector3[] worldScales = new Vector3[childCount];

                for (int i = 0; i < childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    worldPositions[i] = child.position;
                    worldRotations[i] = child.rotation;
                    worldScales[i] = child.lossyScale;
                }

                Undo.RecordObject(transform, "Reset Preserving Children");

                if (propertyName == "m_LocalPosition")
                    transform.localPosition = Vector3.zero;
                else if (propertyName == "m_LocalEulerAnglesHint")
                    transform.localRotation = Quaternion.identity;
                else if (propertyName == "m_LocalScale")
                    transform.localScale = Vector3.one;

                for (int i = 0; i < childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    Undo.RecordObject(child, "Reset Preserving Children");
                    
                    child.position = worldPositions[i];
                    child.rotation = worldRotations[i];
                    
                    if (propertyName == "m_LocalScale")
                    {
                        Vector3 parentScale = transform.lossyScale;
                        child.localScale = new Vector3(
                            worldScales[i].x / parentScale.x,
                            worldScales[i].y / parentScale.y,
                            worldScales[i].z / parentScale.z
                        );
                    }
                }
            }

            serializedObject.Update();
        }

        private bool CanPasteVector3()
        {
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboard)) return false;

            string[] values = clipboard.Split(',');
            if (values.Length != 3) return false;

            return float.TryParse(values[0], out _) && 
                   float.TryParse(values[1], out _) && 
                   float.TryParse(values[2], out _);
        }
    }
}
#endif