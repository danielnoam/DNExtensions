using UnityEngine;
using UnityEditor;

namespace DNExtensions.Utilities.ComponentDragger
{
    public class ComponentDraggerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("Tools/DNExtensions/Component Dragger")]
        public static void ShowWindow()
        {
            ComponentDraggerWindow window = GetWindow<ComponentDraggerWindow>();
            window.titleContent = new GUIContent("Component Dragger");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Component Dragger", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Basic Usage
            EditorGUILayout.LabelField("Basic Usage", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Select a GameObject in the hierarchy", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. In the Inspector, click and drag any component header", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. Drop it onto another GameObject in the hierarchy", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("4. The component will be moved to the target GameObject", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            // Copy Mode
            EditorGUILayout.LabelField("Copy Mode", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Hold ALT while dropping to copy instead of move", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            // Smart Dependencies
            EditorGUILayout.LabelField("Smart Dependencies", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Dependent components are automatically transferred:\n" +
                "• Audio filters follow AudioSource\n" +
                "• Colliders follow Rigidbody\n" +
                "• Joints follow Rigidbody\n" +
                "• And more..."
            );

            EditorGUILayout.Space(10);

            // Undo/Redo
            EditorGUILayout.LabelField("Undo/Redo", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Full support - use Ctrl+Z / Ctrl+Y as normal", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            // Limitations
            EditorGUILayout.LabelField("Limitations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Transform components cannot be moved (Unity restriction)\n" +
                "• Some built-in components may have special behaviors",
                MessageType.Warning
            );

            EditorGUILayout.EndScrollView();
        }
    }
}