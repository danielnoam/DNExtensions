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

            // Position
            DrawVectorWithButtons("Position", "m_LocalPosition");
            
            // Rotation
            DrawVectorWithButtons("Rotation", "m_LocalEulerAnglesHint");
            
            // Scale
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
            
            // Copy button
            if (GUILayout.Button(new GUIContent("C", "Copy values to clipboard"), EditorStyles.miniButtonLeft, GUILayout.Width(20)))
            {
                EditorGUIUtility.systemCopyBuffer = $"{prop.vector3Value.x},{prop.vector3Value.y},{prop.vector3Value.z}";
            }
            
            // Paste button
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
            
            // Reset button
            if (GUILayout.Button(new GUIContent("R", $"Reset {label.ToLower()} to default"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                Undo.RecordObjects(targets, $"Reset {label}");
                prop.vector3Value = propertyName == "m_LocalScale" ? Vector3.one : Vector3.zero;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif