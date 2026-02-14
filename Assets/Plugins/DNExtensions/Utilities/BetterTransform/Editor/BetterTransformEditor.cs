#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class BetterTransformEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawVectorWithButtons("Position", "m_LocalPosition");
            DrawRotationWithButtons();
            DrawVectorWithButtons("Scale", "m_LocalScale");

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    Transform t = obj as Transform;
                    if (t)
                    {
                        EditorUtility.SetDirty(t);
                    }
                }
            }
        }

        private void DrawRotationWithButtons()
        {
            EditorGUILayout.BeginHorizontal();

            Transform t = target as Transform;
            if (t)
            {
                Vector3 eulerAngles = t.localEulerAngles;
            
                EditorGUI.BeginChangeCheck();
                Vector3 newEuler = EditorGUILayout.Vector3Field("Rotation", eulerAngles);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets, "Rotation Changed");
                    foreach (Object obj in targets)
                    {
                        Transform transform = obj as Transform;
                        if (transform)
                        {
                            transform.localEulerAngles = newEuler;
                        }
                    }
                }

                if (GUILayout.Button(new GUIContent("C", "Copy values to clipboard"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
                {
                    EditorGUIUtility.systemCopyBuffer = $"{eulerAngles.x},{eulerAngles.y},{eulerAngles.z}";
                }

                bool canPaste = CanPasteVector3();
                GUI.enabled = canPaste;
                if (GUILayout.Button(new GUIContent("P", "Paste values from clipboard"), EditorStyles.miniButtonMid, GUILayout.Width(20)))
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
                            if (transform != null)
                            {
                                transform.localEulerAngles = new Vector3(x, y, z);
                            }
                        }
                    }
                }
                GUI.enabled = true;

                bool isDefault = eulerAngles == Vector3.zero;
                GUI.enabled = !isDefault;
            }

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
                if (GUILayout.Button(new GUIContent("R", "Reset rotation to default"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    Undo.RecordObjects(targets, "Reset Rotation");
                    foreach (Object obj in targets)
                    {
                        Transform transform = obj as Transform;
                        if (transform != null)
                        {
                            transform.localEulerAngles = Vector3.zero;
                        }
                    }
                }
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawVectorWithButtons(string label, string propertyName)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            
            if (prop == null)
            {
                EditorGUILayout.LabelField(label, "Property not found: " + propertyName);
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
            
            if (GUILayout.Button(new GUIContent("C", "Copy values to clipboard"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
            {
                EditorGUIUtility.systemCopyBuffer = $"{prop.vector3Value.x},{prop.vector3Value.y},{prop.vector3Value.z}";
            }
            
            bool canPaste = CanPasteVector3();
            GUI.enabled = canPaste;
            if (GUILayout.Button(new GUIContent("P", "Paste values from clipboard"), EditorStyles.miniButtonMid, GUILayout.Width(20)))
            {
                string[] values = EditorGUIUtility.systemCopyBuffer.Split(',');
                if (values.Length == 3 && 
                    float.TryParse(values[0], out float x) && 
                    float.TryParse(values[1], out float y) && 
                    float.TryParse(values[2], out float z))
                {
                    Undo.RecordObjects(targets, $"Paste {label}");
                    foreach (Object obj in targets)
                    {
                        Transform t = obj as Transform;
                        if (t != null)
                        {
                            if (propertyName == "m_LocalPosition")
                                t.localPosition = new Vector3(x, y, z);
                            else if (propertyName == "m_LocalScale")
                                t.localScale = new Vector3(x, y, z);
                        }
                    }
                    serializedObject.Update();
                }
            }
            GUI.enabled = true;
            
            Vector3 defaultValue = propertyName == "m_LocalScale" ? Vector3.one : Vector3.zero;
            bool isDefault = prop.vector3Value == defaultValue;
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
                if (GUILayout.Button(new GUIContent("R", $"Reset {label.ToLower()} to default"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    Undo.RecordObjects(targets, $"Reset {label}");
                    foreach (Object obj in targets)
                    {
                        Transform t = obj as Transform;
                        if (t != null)
                        {
                            if (propertyName == "m_LocalPosition")
                                t.localPosition = defaultValue;
                            else if (propertyName == "m_LocalScale")
                                t.localScale = defaultValue;
                        }
                    }
                    serializedObject.Update();
                }
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
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
            if (string.IsNullOrEmpty(clipboard))
                return false;

            string[] values = clipboard.Split(',');
            if (values.Length != 3)
                return false;

            return float.TryParse(values[0], out _) && 
                   float.TryParse(values[1], out _) && 
                   float.TryParse(values[2], out _);
        }
    }
}
#endif