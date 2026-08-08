using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor
{
    /// <summary>
    /// Floating mini inspector used by the Hierarchy component strip (Alt+Click an icon) and the
    /// Project window's quick object window keybind. Draws the target through Editor.CreateEditor so
    /// it behaves like the real Inspector without leaving the window.
    ///
    /// The one panel in the suite that carries a full header: it is the only one meant to stay put
    /// while you work in it, so it gets a title, a close button and somewhere to drag it by.
    /// </summary>
    internal class HelpfulEditorQuickEditWindow : HelpfulEditorDropdownWindow
    {
        private const float DefaultWidth = 340f;

        // Only a starting guess. An IMGUI inspector's height cannot be known before it is drawn, so
        // the window opens at this and settles onto the real height on the first repaint.
        private const float InitialHeight = 240f;

        // Bounds on the measured height. The cap is what keeps a heavy object — a dozen components,
        // or one with a long array — from opening a panel the size of the screen; past it the
        // content scrolls as before.
        private const float MinHeight = 80f;
        private const float MaxHeight = 560f;

        // Breathing room under the measured content. A layout group's rect stops at its last
        // element, taking in neither that element's bottom margin nor the scroll view's own inset,
        // so sizing to it exactly leaves the last field against the frame — and close enough to the
        // edge that a scrollbar appears over a couple of pixels that were never really missing.
        private const float ContentPadding = 8f;

        private readonly List<Editor> _editors = new List<Editor>();
        private Object _target;
        private Vector2 _scroll;

        protected override string HeaderTitle => _target ? _target.name : string.Empty;
        protected override bool IsValid => _target;

        public static void Open(Object target, Vector2 screenPosition)
        {
            if (!target) return;

            HelpfulEditorQuickEditWindow window = CreateInstance<HelpfulEditorQuickEditWindow>();
            window.Initialize(target);

            window.ShowAnchored(new Rect(screenPosition.x, screenPosition.y, 1f, 1f),
                new Vector2(DefaultWidth, InitialHeight));
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

        protected override void DrawContent(Rect content, Event evt)
        {
            GUILayout.BeginArea(content);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.BeginVertical();

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

            EditorGUILayout.EndVertical();

            // Read here but applied after the layout groups are closed: the group's rect is the
            // content's natural height even when the scroll view is showing less of it, and only
            // repaint has real numbers in it.
            float contentHeight = evt.type == EventType.Repaint ? GUILayoutUtility.GetLastRect().height : 0f;

            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();

            if (evt.type == EventType.Repaint) ApplyContentHeight(contentHeight);
        }

        /// <summary>
        /// Resizes to fit what was just drawn. Re-measured every repaint rather than once, so
        /// expanding a foldout or an array grows the window with it. Resize ignores sub-pixel
        /// changes, which is what stops that turning into a resize-repaint-resize loop.
        /// </summary>
        private void ApplyContentHeight(float contentHeight)
        {
            if (contentHeight <= 0f) return;

            float wanted = Mathf.Clamp(contentHeight + ChromeHeight + ContentPadding, MinHeight, MaxHeight);

            Resize(new Vector2(DefaultWidth, wanted));
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
