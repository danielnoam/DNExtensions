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
            DrawVectorWithButtons("Rotation", "m_LocalEulerAnglesHint");
            DrawVectorWithButtons("Scale", "m_LocalScale");

            serializedObject.ApplyModifiedProperties();
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
                    prop.vector3Value = new Vector3(x, y, z);
                }
            }
            GUI.enabled = true;
            
            Vector3 defaultValue = propertyName == "m_LocalScale" ? Vector3.one : Vector3.zero;
            bool isDefault = prop.vector3Value == defaultValue;
            GUI.enabled = !isDefault;
            if (GUILayout.Button(new GUIContent("R", $"Reset {label.ToLower()} to default"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                Undo.RecordObjects(targets, $"Reset {label}");
                prop.vector3Value = defaultValue;
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
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