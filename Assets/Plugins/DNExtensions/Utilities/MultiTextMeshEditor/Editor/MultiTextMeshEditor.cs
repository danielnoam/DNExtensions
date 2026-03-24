using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Editor window for browsing and multi-editing TMP_Text components in the active scene.
    /// </summary>
    internal class MultiTextMeshEditor : EditorWindow
    {
        private List<TMP_Text> _allComponents = new List<TMP_Text>();
        private readonly HashSet<TMP_Text> _selectedComponents = new HashSet<TMP_Text>();
        private int _lastSelectedIndex = -1;

        private string _searchQuery = "";
        private Vector2 _listScrollPosition;
        private Vector2 _inspectorScrollPosition;

        private Editor _cachedEditor;

        private const float ListWidth = 280f;
        private const float RowHeight = 20f;
        private const float InspectorPadding = 10f;

        [MenuItem("Tools/DNExtensions/Multi TextMesh Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<MultiTextMeshEditor>("Multi TextMesh Editor");
            window.minSize = new Vector2(650f, 400f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshComponents();

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            DestroyEditor();

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnHierarchyChanged()
        {
            RefreshComponents();
            Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode)
            {
                RefreshComponents();
                Repaint();
            }
        }

        private void OnGUI()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            DrawToolbar(activeScene);

            if (!activeScene.isLoaded)
            {
                EditorGUILayout.HelpBox("Load a scene to begin editing TMP components.", MessageType.Info);
                return;
            }

            if (_allComponents.Count == 0)
            {
                EditorGUILayout.HelpBox("No TMP components found in the active scene.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            DrawListView();
            DrawInspectorView();

            EditorGUILayout.EndHorizontal();

            HandleGlobalKeyboard();
        }

        private void DrawToolbar(Scene activeScene)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string sceneLabel = activeScene.isLoaded ? activeScene.name : "No Scene Loaded";
            GUILayout.Label(sceneLabel, EditorStyles.toolbarButton, GUILayout.Width(250f));

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200f));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshComponents();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListView()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));

            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

            for (int i = 0; i < _allComponents.Count; i++)
            {
                TMP_Text component = _allComponents[i];
                if (!component) continue;

                bool isSelected = _selectedComponents.Contains(component);
                Rect rowRect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));

                if (isSelected)
                {
                    EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.4f));
                }

                DrawComponentRow(rowRect, component, i);
            }

            EditorGUILayout.EndScrollView();

            DrawListFooter();

            EditorGUILayout.EndVertical();

            Rect separatorRect = new Rect(ListWidth, EditorStyles.toolbar.fixedHeight, 2f, position.height - EditorStyles.toolbar.fixedHeight);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void DrawComponentRow(Rect rowRect, TMP_Text component, int index)
        {
            string typeSuffix = component is TextMeshProUGUI ? "UI" : "3D";
            string label = $"{component.gameObject.name}  ({typeSuffix})";

            Rect labelRect = new Rect(rowRect.x + 5f, rowRect.y, rowRect.width - 10f, rowRect.height);
            EditorGUI.LabelField(labelRect, label);

            HandleRowEvents(rowRect, component, index);
        }

        private void HandleRowEvents(Rect rowRect, TMP_Text component, int index)
        {
            Event e = Event.current;
            if (!rowRect.Contains(e.mousePosition)) return;

            if (e.type == EventType.ContextClick)
            {
                ShowContextMenu(component);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (e.clickCount == 2)
                {
                    Selection.activeGameObject = component.gameObject;
                    EditorGUIUtility.PingObject(component.gameObject);
                }
                else
                {
                    HandleSelection(component, index, e);
                }
                e.Use();
            }
        }

        private void ShowContextMenu(TMP_Text component)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Ping in Hierarchy"), false, () =>
            {
                Selection.activeGameObject = component.gameObject;
                EditorGUIUtility.PingObject(component.gameObject);
            });

            menu.ShowAsContext();
        }

        private void DrawListFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5f);
            GUILayout.Label($"Found: {_allComponents.Count}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("↻", "Refresh"), EditorStyles.miniButton, GUILayout.Width(25f)))
            {
                RefreshComponents();
            }

            GUILayout.Space(5f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }

        private void DrawInspectorView()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5f);

            if (_selectedComponents.Count == 0)
            {
                EditorGUILayout.HelpBox("Select one or more TMP components to edit.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(InspectorPadding);

                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField($"Selected: {_selectedComponents.Count}", EditorStyles.boldLabel);
                EditorGUILayout.Space(5f);

                _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

                if (_cachedEditor)
                {
                    try
                    {
                        _cachedEditor.OnInspectorGUI();
                    }
                    catch (Exception e)
                    {
                        EditorGUILayout.HelpBox(
                            "Unable to display inspector for the current selection. " +
                            "This may occur when mixing TextMeshPro and TextMeshProUGUI components.\n\n" +
                            $"Error: {e.Message}",
                            MessageType.Warning);

                        if (GUILayout.Button("Select Single Component"))
                        {
                            TMP_Text first = _selectedComponents.First();
                            _selectedComponents.Clear();
                            _selectedComponents.Add(first);
                            UpdateInspector();
                            Repaint();
                        }
                    }
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.EndVertical();

                GUILayout.Space(InspectorPadding);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void HandleSelection(TMP_Text component, int index, Event evt)
        {
            if (evt.control || evt.command)
            {
                if (!_selectedComponents.Add(component))
                {
                    _selectedComponents.Remove(component);
                }
            }
            else if (evt.shift && _lastSelectedIndex >= 0)
            {
                int min = Mathf.Min(_lastSelectedIndex, index);
                int max = Mathf.Max(_lastSelectedIndex, index);

                for (int i = min; i <= max; i++)
                {
                    if (_allComponents[i])
                    {
                        _selectedComponents.Add(_allComponents[i]);
                    }
                }
            }
            else
            {
                _selectedComponents.Clear();
                _selectedComponents.Add(component);
            }

            _lastSelectedIndex = index;
            UpdateInspector();
            Repaint();
        }

        private void HandleGlobalKeyboard()
        {
            if (Event.current.type != EventType.KeyDown) return;

            switch (Event.current.keyCode)
            {
                case KeyCode.UpArrow:
                    NavigateList(-1, Event.current.shift);
                    Event.current.Use();
                    break;

                case KeyCode.DownArrow:
                    NavigateList(1, Event.current.shift);
                    Event.current.Use();
                    break;

                case KeyCode.A when Event.current.control || Event.current.command:
                    _selectedComponents.Clear();
                    _selectedComponents.UnionWith(_allComponents.Where(c => c));
                    _lastSelectedIndex = _allComponents.Count - 1;
                    UpdateInspector();
                    Event.current.Use();
                    Repaint();
                    break;
            }
        }

        private void NavigateList(int direction, bool extendSelection)
        {
            if (_allComponents.Count == 0) return;

            int startIndex = _lastSelectedIndex >= 0 ? _lastSelectedIndex : 0;
            int newIndex = Mathf.Clamp(startIndex + direction, 0, _allComponents.Count - 1);

            if (newIndex == startIndex && _selectedComponents.Count > 0) return;

            TMP_Text component = _allComponents[newIndex];
            if (!component) return;

            if (extendSelection)
            {
                _selectedComponents.Add(component);
            }
            else
            {
                _selectedComponents.Clear();
                _selectedComponents.Add(component);
            }

            _lastSelectedIndex = newIndex;
            UpdateInspector();
            ScrollToIndex(newIndex);
            Repaint();
        }

        private void ScrollToIndex(int index)
        {
            float itemTop = index * RowHeight;
            float itemBottom = itemTop + RowHeight;
            float scrollTop = _listScrollPosition.y;
            float viewHeight = position.height - EditorStyles.toolbar.fixedHeight - 30f;
            float scrollBottom = scrollTop + viewHeight;

            if (itemTop < scrollTop)
                _listScrollPosition.y = itemTop;
            else if (itemBottom > scrollBottom)
                _listScrollPosition.y = itemBottom - viewHeight;
        }

        private void RefreshComponents()
        {
            _allComponents.Clear();
            _selectedComponents.Clear();
            _lastSelectedIndex = -1;
            DestroyEditor();

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            foreach (GameObject root in activeScene.GetRootGameObjects())
            {
                _allComponents.AddRange(root.GetComponentsInChildren<TMP_Text>(true));
            }

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                string query = _searchQuery.ToLower();
                _allComponents = _allComponents
                    .Where(c => c &&
                                (c.gameObject.name.ToLower().Contains(query) ||
                                 c.text.ToLower().Contains(query)))
                    .ToList();
            }

            _allComponents = _allComponents
                .Where(c => c)
                .OrderBy(c => c.gameObject.name)
                .ToList();
        }

        private void UpdateInspector()
        {
            DestroyEditor();

            var valid = _selectedComponents.Where(c => c).ToArray();
            if (valid.Length == 0) return;

            Type firstType = valid[0].GetType();
            var sameType = valid.Where(c => c.GetType() == firstType).Cast<UnityEngine.Object>().ToArray();

            _cachedEditor = Editor.CreateEditor(sameType);
        }

        private void DestroyEditor()
        {
            if (!_cachedEditor) return;
            DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }
    }
}