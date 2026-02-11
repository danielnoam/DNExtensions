using DNExtensions.Utilities.CustomFields;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Attach notes to GameObjects for documentation and reminders.
    /// Notes are displayed in the inspector and optionally in the scene view via gizmos.
    /// </summary>
    [AddComponentMenu("DNExtensions/Note")]
    [DisallowMultipleComponent]
    public class Note : MonoBehaviour
    {
        [SerializeField] private NoteField text = new("Add your note here...", isEditable: true);
        [Space(10)]
        [SerializeField] private bool showInSceneView;
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0.5f, 1f);

        /// <summary>
        /// Gets or sets the note text.
        /// </summary>
        public string Text
        {
            get => text;
            set => text.Note = value;
        }

        /// <summary>
        /// Gets or sets whether the note is visible in the scene view.
        /// </summary>
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
                normal = { textColor = gizmoColor },
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