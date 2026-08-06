using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.HelpfulEditor.Inspector
{
    /// <summary>
    /// Object-level control panel drawn once at the top of the Inspector, plus the per-component
    /// copy-values button. Both hang off Editor.finishedDefaultHeaderGUI, which fires for whichever
    /// object owns the large header — a GameObject when inspecting a scene object, or the component
    /// itself when a single component is the inspected target.
    /// </summary>
    [InitializeOnLoad]
    internal static class InspectorHeaderBar
    {
        public const string SearchControlName = "helpfuleditor_field_search";

        /// <summary>Component under the cursor in the header bar, for the hover keybinds to act on.</summary>
        public static Component HoveredComponent { get; private set; }

        private const float HorizontalPadding = 6f;
        private const float IconGap = 2f;
        private const float ButtonSpacing = 3f;
        private const float RowSpacing = 2f;

        /// <summary>Darken-only, so it reads the same whichever editor theme is in use.</summary>
        private static readonly Color DisabledTint = new Color(0f, 0f, 0f, 0.2f);
        private const float IconInset = 5f;
        private const float EdgeGap = 3f;

        private static readonly GUIContent ButtonContent = new GUIContent();
        private static readonly GUIContent EyeContent = new GUIContent();

        private const double RepaintInterval = 0.05;

        private static GUIStyle _buttonStyle;
        private static GUIStyle _eyeStyle;
        private static string _search = string.Empty;
        private static double _lastRepaint;
        private static EditorWindow _repaintTarget;
        private static bool _repaintPending;

        private static Color SeparatorColor => EditorGUIUtility.isProSkin
            ? new Color(0.14f, 0.14f, 0.14f)
            : new Color(0.6f, 0.6f, 0.6f);

        /// <summary>Icon size follows the button height but stays well inside it, so icons read as small marks rather than filling the button.</summary>
        private static float IconSize(InspectorSettings settings)
        {
            return Mathf.Max(8f, settings.headerBarButtonHeight - IconInset * 2f);
        }

        /// <summary>
        /// The style draws text only and reserves the icon's width as left padding — the icon is
        /// then drawn manually at its own size. Letting the style draw it instead stretches it to
        /// the button's content height, which is what made the icons oversized.
        /// </summary>
        private static GUIStyle ButtonStyle(InspectorSettings settings)
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                    imagePosition = ImagePosition.TextOnly,
                    padding = new RectOffset(3, 6, 1, 1),
                    margin = new RectOffset(0, 0, 0, 0),
                    richText = false
                };
            }

            _buttonStyle.fixedHeight = Mathf.Max(16f, settings.headerBarButtonHeight);
            _buttonStyle.padding.left = Mathf.RoundToInt(EdgeGap + IconSize(settings) + IconGap);
            return _buttonStyle;
        }

        private static void DrawIcon(Rect rect, Texture icon, float iconSize)
        {
            if (!icon || Event.current.type != EventType.Repaint) return;

            Rect iconRect = new Rect(rect.x + EdgeGap, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// The component buttons are left-aligned with asymmetric padding so icon and label sit
        /// together. An icon-only button needs its own style, or it renders off to one side.
        /// </summary>
        private static GUIStyle EyeStyle(GUIStyle buttonStyle)
        {
            if (_eyeStyle == null)
            {
                _eyeStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageOnly,
                    padding = new RectOffset(1, 1, 1, 1),
                    margin = new RectOffset(0, 0, 0, 0)
                };
            }

            _eyeStyle.fixedHeight = buttonStyle.fixedHeight;
            return _eyeStyle;
        }

        /// <summary>Spans the full Inspector width — a divider that stops short of the edges reads as an unfinished line.</summary>
        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            rect.xMin = 0f;
            rect.xMax = EditorGUIUtility.currentViewWidth;

            EditorGUI.DrawRect(rect, SeparatorColor);
        }

        static InspectorHeaderBar()
        {
            Editor.finishedDefaultHeaderGUI -= OnFinishedHeaderGUI;
            Editor.finishedDefaultHeaderGUI += OnFinishedHeaderGUI;

            InspectorComponentHover.PointerMoved -= OnPointerMoved;
            InspectorComponentHover.PointerMoved += OnPointerMoved;

            InspectorComponentHover.PointerLeft -= OnPointerLeft;
            InspectorComponentHover.PointerLeft += OnPointerLeft;

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// The Inspector does not repaint on mouse move, so the hover cache would only refresh when
        /// something else happened to redraw — leaving the keybinds acting on whichever button was
        /// under the cursor at the last repaint. The repaints are asked for by the cursor actually
        /// moving rather than by a timer: a stationary cursor cannot change which button it is over,
        /// and the Inspector is the most expensive window in the editor to redraw.
        /// </summary>
        private static void OnPointerMoved(EditorWindow window)
        {
            RequestRepaint(window);
        }

        /// <summary>
        /// Leaving drops the hover outright, and takes one last repaint with it so the button the
        /// cursor was on stops being drawn hovered.
        /// </summary>
        private static void OnPointerLeft(EditorWindow window)
        {
            HoveredComponent = null;
            RequestRepaint(window);
        }

        private static void RequestRepaint(EditorWindow window)
        {
            if (!window) return;

            _repaintTarget = window;
            _repaintPending = true;

            Flush();
        }

        private static void OnUpdate()
        {
            // PointerLeave is not guaranteed — a window closing or the cursor jumping straight out
            // can skip it — so a hover still naming a button the cursor has left is dropped here as
            // a backstop. Only on positive evidence though: a null mouseOverWindow means the editor
            // cannot say where the cursor is, not that it left the Inspector.
            if (HoveredComponent && EditorWindow.mouseOverWindow && !HelpfulEditorWindows.MouseOverInspector)
            {
                HoveredComponent = null;
            }

            // Moves arrive far faster than the Inspector is worth redrawing, so they are throttled.
            // This is what guarantees the last one still lands once the cursor comes to rest, rather
            // than being dropped by the throttle and leaving the hover a step behind.
            if (_repaintPending) Flush();
        }

        private static void Flush()
        {
            if (!HelpfulEditorSettings.Inspector.moduleEnabled || !_repaintTarget)
            {
                _repaintPending = false;
                return;
            }

            if (EditorApplication.timeSinceStartup - _lastRepaint < RepaintInterval) return;

            _repaintPending = false;
            _lastRepaint = EditorApplication.timeSinceStartup;
            _repaintTarget.Repaint();
        }

        public static void FocusSearchField()
        {
            EditorGUI.FocusTextInControl(SearchControlName);
        }

        private static void OnFinishedHeaderGUI(Editor editor)
        {
            InspectorSettings settings = HelpfulEditorSettings.Inspector;
            if (!settings.moduleEnabled || !editor) return;

            if (settings.headerBarEnabled && editor.target is GameObject gameObject) DrawObjectBar(gameObject, settings);
        }

        private static void DrawObjectBar(GameObject gameObject, InspectorSettings settings)
        {
            List<Component> components = HelpfulEditorGUI.GetDisplayComponents(gameObject, settings.excludedComponentTypes);
            if (components.Count == 0) return;

            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(3);

            DrawComponentButtons(components, settings);

            if (!settings.fieldSearchEnabled)
            {
                // A leftover search must not outlive the box that would let you clear it.
                _search = string.Empty;
                return;
            }

            EditorGUILayout.Space(5);

            DrawFieldSearch(gameObject, settings);
        }

        /// <summary>
        /// Lays the buttons out as a flow: each one is sized to its own label, and a row is closed
        /// as soon as the next button would overrun the Inspector's width. GUILayout has no wrapping
        /// horizontal group, so the wrap point is worked out here.
        /// </summary>
        private static void DrawComponentButtons(List<Component> components, InspectorSettings settings)
        {
            GUIStyle style = ButtonStyle(settings);
            float available = EditorGUIUtility.currentViewWidth - HorizontalPadding * 2f - 22f;
            float used = 0f;

            // Rebuilt each repaint so a button the cursor has left stops being the keybind target.
            // Only on repaint: layout passes have no real rects yet, so every hit test would miss.
            if (Event.current.type == EventType.Repaint) HoveredComponent = null;

            BeginPaddedRow();

            float iconSize = IconSize(settings);
            GUIStyle eyeStyle = EyeStyle(style);
            float eyeWidth = eyeStyle.fixedHeight;

            DrawEyeButton(GUILayoutUtility.GetRect(eyeWidth, eyeStyle.fixedHeight, GUILayout.Width(eyeWidth)), eyeStyle, iconSize);
            GUILayout.Space(ButtonSpacing);
            used += eyeWidth + ButtonSpacing;

            for (int i = 0; i < components.Count; i++)
            {
                Component component = components[i];
                if (!component) continue;

                ButtonContent.text = ObjectNames.NicifyVariableName(component.GetType().Name);
                ButtonContent.tooltip = component.GetType().FullName;

                // The icon's width is already reserved as the style's left padding, so measuring
                // the label alone covers the whole button. The content is deliberately left without
                // an image: CalcSize measures one at its native resolution, and component icons are
                // often 32px or larger, so it would return a button far wider than the drawn one.
                float width = style.CalcSize(ButtonContent).x;

                if (used > 0f && used + width > available)
                {
                    EndPaddedRow();
                    EditorGUILayout.Space(RowSpacing);
                    BeginPaddedRow();
                    used = 0f;
                }

                Rect rect = GUILayoutUtility.GetRect(width, style.fixedHeight, GUILayout.Width(width));
                DrawComponentButton(rect, style, components, i, iconSize);

                GUILayout.Space(ButtonSpacing);
                used += width + ButtonSpacing;
            }

            EndPaddedRow();
        }

        /// <summary>
        /// Shows whether every component is visible, and clears isolation when clicked. Drawn
        /// pressed while nothing is hidden, so losing the pressed state is what flags an incomplete
        /// list.
        /// </summary>
        private static void DrawEyeButton(Rect rect, GUIStyle style, float iconSize)
        {
            Event evt = Event.current;
            if (evt == null) return;

            bool isolating = ComponentIsolation.IsIsolating;
            bool hovered = rect.Contains(evt.mousePosition);

            EyeContent.tooltip = isolating
                ? "Some components are hidden — click to show all"
                : "All components are shown";

            if (evt.type == EventType.Repaint)
            {
                // Pressed while everything is visible, matching the open eye: the button reads as
                // "showing all", and releasing it is what a hidden component looks like.
                style.Draw(rect, EyeContent, hovered, false, !isolating, false);

                Texture eye = EditorGUIUtility.IconContent(isolating
                    ? "animationvisibilitytoggleoff"
                    : "animationvisibilitytoggleon")?.image;

                if (eye)
                {
                    Rect iconRect = new Rect(rect.center.x - iconSize * 0.5f, rect.center.y - iconSize * 0.5f, iconSize, iconSize);
                    GUI.DrawTexture(iconRect, eye, ScaleMode.ScaleToFit);
                }
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (!hovered || evt.type != EventType.MouseDown || evt.button != 0) return;

            ComponentIsolation.Clear();
            evt.Use();
        }

        private static void BeginPaddedRow()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(HorizontalPadding);
        }

        private static void EndPaddedRow()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(HorizontalPadding);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawComponentButton(Rect rect, GUIStyle style, List<Component> components, int index, float iconSize)
        {
            Component component = components[index];
            if (!component) return;

            Event evt = Event.current;
            if (evt == null) return;

            bool hovered = rect.Contains(evt.mousePosition);

            // Painted straight from the style rather than through GUI.Button or GUI.Toggle: those
            // are real controls and would swallow the click before the modifier handling below runs.
            // An isolated component reads as pressed instead of as a tint behind a loose icon.
            if (evt.type == EventType.Repaint)
            {
                style.Draw(rect, ButtonContent, hovered, false, ComponentIsolation.Contains(component), false);
                DrawIcon(rect, HelpfulEditorGUI.GetIcon(component), iconSize);

                // The button's own background drawn again in black, rather than a filled rect: the
                // style's texture carries the rounded corners, and tinting through GUI.color darkens
                // only where that texture is opaque. A rect would square off the corners.
                if (!HelpfulEditorComponents.IsEnabled(component))
                {
                    Color previous = GUI.color;
                    GUI.color = DisabledTint;
                    style.Draw(rect, GUIContent.none, hovered, false, ComponentIsolation.Contains(component), false);
                    GUI.color = previous;
                }

                if (hovered) HoveredComponent = component;
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (!hovered) return;

            if (evt.type == EventType.ContextClick)
            {
                ShowIconContextMenu(component);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseDrag && evt.button == 0 && component is not Transform)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { component };
                DragAndDrop.StartDrag(component.GetType().Name);
                evt.Use();
                return;
            }

            if (evt.type != EventType.MouseDown || evt.button != 0) return;

            if (evt.control || evt.command) ComponentIsolation.Toggle(component, index);
            else if (evt.shift) ComponentIsolation.SelectRange(components, index);
            else ComponentIsolation.Solo(component, index);

            evt.Use();
        }

        private static void ShowIconContextMenu(Component component)
        {
            List<Component> selection = ComponentIsolation.GetSelection(component);
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent(selection.Count > 1 ? $"Copy {selection.Count} Components" : "Copy Component"),
                false, () => ComponentClipboard.Copy(selection));

            // Pasting values writes into one existing component, so it has no meaning for a
            // multi-selection.
            if (selection.Count > 1) menu.AddDisabledItem(new GUIContent("Copy Component Values"));
            else menu.AddItem(new GUIContent("Copy Component Values"), false, () => ComponentUtility.CopyComponent(component));

            int copied = ComponentClipboard.Count;
            if (copied > 0)
            {
                GameObject target = component.gameObject;
                menu.AddSeparator("");
                menu.AddItem(new GUIContent(copied > 1 ? $"Paste {copied} Components" : "Paste Component"),
                    false, () => ComponentClipboard.PasteTo(target));
            }

            menu.ShowAsContext();
        }

        private static void DrawFieldSearch(GameObject gameObject, InspectorSettings settings)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(HorizontalPadding);

            GUI.SetNextControlName(SearchControlName);
            string search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);

            if (!string.IsNullOrEmpty(_search) && GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22f)))
            {
                search = string.Empty;
                GUI.FocusControl(null);
            }

            GUILayout.Space(HorizontalPadding);
            EditorGUILayout.EndHorizontal();

            _search = search;

            if (string.IsNullOrWhiteSpace(_search)) return;

            EditorGUILayout.Space(2);
            InspectorFieldSearch.Draw(gameObject, settings, _search);
        }

    }
}
