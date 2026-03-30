using UnityEditor;
using UnityEngine;

namespace DNExtensions.Systems.Scriptables
{
    /// <summary>
    /// Custom editor for SOBase-derived ScriptableObjects.
    /// </summary>
    [CustomEditor(typeof(SOBase), true)]
    public class SOBaseEditor : Editor
    {
        private SerializedProperty _allowEditingProp;

        private void OnEnable()
        {
            _allowEditingProp = serializedObject.FindProperty("allowEditingInReference");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);

            SerializedProperty iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);

            while (iterator.NextVisible(false))
            {
                if (iterator.name == _allowEditingProp.name)
                {
                    continue;
                }

                if (iterator.name == "value")
                {
                    DrawValueProperty(iterator);
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            EditorGUILayout.PropertyField(_allowEditingProp, new GUIContent("Show In References"));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the value property. Override to customize how the value is displayed.
        /// </summary>
        protected virtual void DrawValueProperty(SerializedProperty valueProperty)
        {
            EditorGUILayout.PropertyField(valueProperty, true);
        }
    }
}