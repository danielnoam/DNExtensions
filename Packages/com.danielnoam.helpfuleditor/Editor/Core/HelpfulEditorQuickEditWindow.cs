using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Floating, non-resizable mini inspector used by the Hierarchy component strip (Alt+Click an
    /// icon) and the Project window's quick object window keybind. Draws the target through
    /// Editor.CreateEditor so it behaves like the real Inspector without leaving the window.
    /// </summary>
    internal class HelpfulEditorQuickEditWindow : EditorWindow
    {
        private const float DefaultWidth = 340f;
        private const float DefaultHeight = 420f;

        private const float FramePadding = 6f;

        private static Color OuterBorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.08f, 0.08f, 0.08f)
            : new Color(0.35f, 0.35f, 0.35f);

        private static Color InnerBorderColor => EditorGUIUtility.isProSkin
            ? new Color(0.42f, 0.42f, 0.42f)
            : new Color(0.72f, 0.72f, 0.72f);

        private static Color SeparatorColor => EditorGUIUtility.isProSkin
            ? new Color(0.25f, 0.25f, 0.25f)
            : new Color(0.65f, 0.65f, 0.65f);

        private readonly List<Editor> _editors = new List<Editor>();
        private Object _target;
        private Vector2 _scroll;

        public static void Open(Object target, Vector2 screenPosition)
        {
            if (!target) return;

            HelpfulEditorQuickEditWindow window = CreateInstance<HelpfulEditorQuickEditWindow>();
            window.Initialize(target);

            Rect activator = new Rect(screenPosition.x, screenPosition.y, 1f, 1f);
            window.ShowAsDropDown(activator, new Vector2(DefaultWidth, DefaultHeight));
        }

        public static Vector2 MouseScreenPosition()
        {
            return GUIUtility.GUIToScreenPoint(Event.current != null ? Event.current.mousePosition : Vector2.zero);
        }

        private void Initialize(Object target)
        {
            _target = target;
            titleContent = new GUIContent(target.name);

            if (target is GameObject gameObject)
            {
                foreach (Component component in gameObject.GetComponents<Component>())
                {
                    if (!component) continue;
                    _editors.Add(Editor.CreateEditor(component));
                }
            }
            else
            {
                _editors.Add(Editor.CreateEditor(target));
            }
        }

        private void OnGUI()
        {
            if (!_target)
            {
                Close();
                return;
            }

            Event evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.Use();
                return;
            }

            DrawFrame();

            GUILayout.BeginArea(new Rect(FramePadding, FramePadding,
                position.width - FramePadding * 2f, position.height - FramePadding * 2f));

            EditorGUILayout.LabelField(_target.name, EditorStyles.boldLabel);
            HelpfulEditorGUI.DrawHorizontalLine(0f, position.width - FramePadding * 2f,
                GUILayoutUtility.GetLastRect().yMax + 2f, SeparatorColor, LineStyle.Solid);
            EditorGUILayout.Space(5);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            bool multiple = _editors.Count > 1;
            foreach (Editor editor in _editors)
            {
                if (!editor || !editor.target) continue;

                if (multiple)
                {
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(editor.target.GetType().Name), EditorStyles.miniBoldLabel);
                }

                editor.OnInspectorGUI();

                if (multiple) EditorGUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        /// <summary>
        /// A dropdown window gets no chrome from Unity, so the panel edge is drawn here: a dark
        /// outer stroke to separate it from whatever is behind, and a lighter inner stroke so the
        /// edge stays visible against a dark background too.
        /// </summary>
        private void DrawFrame()
        {
            if (Event.current.type != EventType.Repaint) return;

            Rect frame = new Rect(0f, 0f, position.width, position.height);

            EditorGUI.DrawRect(frame, HelpfulEditorGUI.WindowBackground);
            HelpfulEditorGUI.DrawBorder(frame, OuterBorderColor);
            HelpfulEditorGUI.DrawBorder(new Rect(frame.x + 1f, frame.y + 1f, frame.width - 2f, frame.height - 2f), InnerBorderColor);
        }

        private void OnDisable()
        {
            foreach (Editor editor in _editors)
            {
                if (editor) DestroyImmediate(editor);
            }

            _editors.Clear();
        }
    }
}
