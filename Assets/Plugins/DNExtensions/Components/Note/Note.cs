using DNExtensions.Utilities.CustomFields;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Attach note to GameObjects for documentation and reminders.
    /// Notes are displayed in the inspector and optionally in the scene view via gizmos.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("DNExtensions/Note")]
    [DisallowMultipleComponent]
    public class Note : MonoBehaviour
    {
        [SerializeField] private NoteField text = new("Add your note here...", isEditable: true);
        [Space(10)]
        [SerializeField] private bool showInSceneView;
        [SerializeField] private Color textColor = new Color(1f, 1f, 0.5f, 1f);

        private void OnEnable()
        {
            #if UNITY_EDITOR
            EditorApplication.update += ValidatePosition;
            #endif
        }
        
        private void OnDisable()
        {
            #if UNITY_EDITOR
            EditorApplication.update -= ValidatePosition;
            #endif
        }
        
        #if UNITY_EDITOR
        private void ValidatePosition()
        {
            if (Application.isPlaying) return;
        
            if (Selection.activeGameObject != gameObject) return;

            Component[] components = gameObject.GetComponents<Component>();
        
            if (components.Length > 1 && components[1] != this)
            {
                while (ComponentUtility.MoveComponentUp(this)) { }
            }
        }
        #endif

        public string Text
        {
            get => text;
            set => text.Note = value;
        }

        public bool ShowInSceneView
        {
            get => showInSceneView;
            set => showInSceneView = value;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showInSceneView || string.IsNullOrWhiteSpace(text)) return;
            
            Vector3 labelPosition = transform.position + Vector3.up;
            Handles.Label(labelPosition, text, GetGizmoStyle());
        }

        private GUIStyle GetGizmoStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                normal = { textColor = textColor },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(4, 4, 4, 4)
            };
            return style;
        }
#endif
    }
}