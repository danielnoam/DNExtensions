using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Viewport
{
    /// <summary>
    /// The list of overlapping objects, shown where the click landed. Rows read like Hierarchy rows —
    /// icon, name, component strip — and previewing the hovered one in the Scene View is what this
    /// gives that Unity's own menu does not.
    /// </summary>
    internal class SceneViewPickerWindow : HelpfulEditorDropdownListWindow
    {
        private const float IconWidth = 18f;
        private const float IconSize = 14f;
        private const float LabelStripGap = 12f;

        private readonly List<GameObject> _candidates = new List<GameObject>();
        private readonly List<Component> _components = new List<Component>();
        private readonly List<Rect> _iconRects = new List<Rect>();
        private readonly GUIContent _rowContent = new GUIContent();
        private readonly GUIContent _overflowContent = new GUIContent();

        protected override int RowCount => _candidates.Count;

        // A header purely to say how deep the stack is. Nothing to close it with and nowhere to drag
        // it: it is a dropdown that answers one click and then goes away.
        protected override string HeaderTitle => $"{_candidates.Count} under cursor";
        protected override bool ShowCloseButton => false;
        protected override bool Movable => false;

        protected override float MinWidth => 200f;
        protected override float MaxWidth => 520f;
        protected override float MinHeight => 56f;
        protected override float MaxHeight => 640f;

        public static void Open(List<GameObject> candidates, Vector2 screenPosition, SceneView origin)
        {
            if (candidates == null || candidates.Count == 0) return;

            SceneViewPickerWindow window = CreateInstance<SceneViewPickerWindow>();
            window.Initialize(candidates);

            window.ShowList(new Rect(screenPosition.x, screenPosition.y, 1f, 1f));

            if (origin) origin.Repaint();
        }

        private void Initialize(List<GameObject> candidates)
        {
            foreach (GameObject candidate in candidates)
            {
                if (candidate) _candidates.Add(candidate);
            }
        }

        protected override float MeasureRowWidth(int index)
        {
            GameObject candidate = _candidates[index];

            SceneViewSettings settings = HelpfulEditorSettings.SceneView;
            HelpfulEditorGUI.GetDisplayComponents(candidate, settings.pickerExcludedComponentTypes, _components);

            int icons = settings.pickerMaxIcons > 0
                ? Mathf.Min(_components.Count, settings.pickerMaxIcons)
                : _components.Count;

            float strip = icons * (IconSize + 1f);
            if (icons < _components.Count) strip += IconSize + 6f;

            _rowContent.text = candidate.name;
            _rowContent.tooltip = string.Empty;

            return IconWidth + EditorStyles.label.CalcSize(_rowContent).x + LabelStripGap + strip;
        }

        protected override void DrawRow(Rect rowRect, int index)
        {
            GameObject candidate = _candidates[index];
            if (!candidate) return;

            SceneViewSettings settings = HelpfulEditorSettings.SceneView;

            Rect iconRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - 16f) * 0.5f, 16f, 16f);
            Texture icon = EditorGUIUtility.ObjectContent(candidate, typeof(GameObject)).image;
            if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            _rowContent.text = candidate.name;
            _rowContent.tooltip = HierarchyPath(candidate);

            float stripWidth = DrawComponentStrip(rowRect, candidate, settings);

            Rect labelRect = Rect.MinMaxRect(rowRect.x + IconWidth, rowRect.y, rowRect.xMax - stripWidth, rowRect.yMax);
            if (labelRect.width > 0f) GUI.Label(labelRect, _rowContent, EditorStyles.label);
        }

        /// <summary>Right-aligned component icons, matching the Hierarchy strip so rows read the same in both.</summary>
        private float DrawComponentStrip(Rect rowRect, GameObject candidate, SceneViewSettings settings)
        {
            HelpfulEditorGUI.GetDisplayComponents(candidate, settings.pickerExcludedComponentTypes, _components);
            if (_components.Count == 0) return 0f;

            Rect area = Rect.MinMaxRect(rowRect.x + IconWidth, rowRect.y, rowRect.xMax, rowRect.yMax);

            HelpfulEditorGUI.LayoutIconStrip(area, _components.Count, IconSize,
                settings.pickerMaxIcons, _iconRects, out int shown, out Rect overflowRect);

            if (shown == 0) return 0f;

            Color previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * HelpfulEditorGUI.IconStripOpacity);

            for (int i = 0; i < shown; i++)
            {
                Texture icon = HelpfulEditorGUI.GetIcon(_components[i]);
                if (icon) GUI.DrawTexture(_iconRects[i], icon, ScaleMode.ScaleToFit);
            }

            if (shown < _components.Count)
            {
                _overflowContent.text = $"+{_components.Count - shown}";
                GUI.Label(overflowRect, _overflowContent, HelpfulEditorGUI.BadgeStyle);
            }

            GUI.color = previousColor;

            return area.xMax - _iconRects[0].x;
        }

        protected override void OnHighlightChanged(int index)
        {
            SceneViewPicker.SetHoverTarget(_candidates[index]);
        }

        /// <param name="additive">
        /// Shift, not Ctrl: the picker is opened with Ctrl held by default, so a Ctrl-click here
        /// would fire the moment someone clicked a row without having let go first.
        /// </param>
        protected override void Activate(int index, bool additive)
        {
            GameObject target = _candidates[index];
            if (!target) return;

            if (additive)
            {
                List<Object> selection = new List<Object>(Selection.objects);

                if (selection.Contains(target)) selection.Remove(target);
                else selection.Add(target);

                Selection.objects = selection.ToArray();
                Repaint();
                return;
            }

            Selection.activeGameObject = target;
            Close();
        }

        private static string HierarchyPath(GameObject candidate)
        {
            string path = candidate.name;

            for (Transform parent = candidate.transform.parent; parent; parent = parent.parent)
            {
                path = $"{parent.name}/{path}";
            }

            return path;
        }

        private void OnDisable()
        {
            SceneViewPicker.SetHoverTarget(null);
        }
    }
}
