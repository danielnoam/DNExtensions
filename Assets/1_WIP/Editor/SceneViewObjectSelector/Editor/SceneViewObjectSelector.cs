// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// namespace DNExtensions.Utilities {
//     
//     /// <summary>
//     /// Editor window that displays a dropdown list of overlapping GameObjects in scene view.
//     /// Supports hierarchy display, multi-selection, and scene highlighting.
//     /// </summary>
//     public class SceneViewObjectSelector : EditorWindow {
//         private static readonly Color SelectionColor = new Color(0.50f, 0.70f, 1.00f);
//         private static readonly Color HighlightWireColor = Color.yellow;
//
//         private const float SizeXPadding = 2f;
//         private const float SizeYPadding = 2f;
//         private const float ButtonYSpacing = 0.0f;
//         private const float ButtonYSize = 20f;
//         private const float SizeXOffset = -30f;
//         private const float IndentWidth = 12f;
//
//         private readonly List<Renderer> _highlightedRenderers = new List<Renderer>();
//
//         private List<DisplayEntry> _displayEntries;
//         private GameObject _highlightedObject;
//         private Vector2 _size;
//         private Vector2 _buttonSize;
//
//         private int _shiftMinSelectionId = -1;
//         private int _shiftMaxSelectionId = -1;
//         private int _shiftLastId = -1;
//
//         /// <summary>
//         /// Shows the object selector dropdown at the specified position with the given GameObjects.
//         /// </summary>
//         public static void Show(List<GameObject> gameObjects, Vector2 position) {
//             var rect = new Rect(position, Vector2.one);
//
//             var window = CreateInstance<SceneViewObjectSelector>();
//             window.wantsMouseMove = true;
//             window.wantsMouseEnterLeaveWindow = true;
//             window._displayEntries = window.BuildDisplayEntries(gameObjects);
//             window.CalculateSize();
//             window.ShowAsDropDown(rect, window._size);
//         }
//
//         private void OnEnable() {
// #if UNITY_2019_1_OR_NEWER
//             SceneView.duringSceneGui += OnSceneViewGui;
// #endif
//         }
//
//         private void OnDisable() {
// #if UNITY_2019_1_OR_NEWER
//             SceneView.duringSceneGui -= OnSceneViewGui;
// #endif
//             _highlightedRenderers.Clear();
//             _highlightedObject = null;
//         }
//
//         private void OnGUI() {
//             if (Event.current.type == EventType.Layout) {
//                 return;
//             }
//
//             switch (Event.current.type) {
//                 case EventType.MouseMove:
//                     OnGuiMouseMove();
//                     break;
//                 case EventType.MouseLeaveWindow:
//                     OnGuiMouseLeave();
//                     break;
//                 default:
//                     OnGuiNormal();
//                     break;
//             }
//         }
//
//         private void OnGuiMouseLeave() {
//             HighlightedObject = null;
//         }
//
//         private void OnGuiMouseMove() {
//             var rect = new Rect(SizeXPadding, SizeYPadding, _buttonSize.x, _buttonSize.y);
//             for (var i = 0; i < _displayEntries.Count; i++) {
//                 var entry = _displayEntries[i];
//                 var gameObject = entry.GameObject;
//                 if (gameObject == null) {
//                     continue;
//                 }
//
//                 var indentOffset = entry.Depth * IndentWidth;
//                 var drawRect = new Rect(rect);
//                 drawRect.x += indentOffset;
//                 drawRect.width -= indentOffset;
//
//                 var content = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject));
//                 GUI.Button(drawRect, content, Style.ButtonStyle);
//                 if (drawRect.Contains(Event.current.mousePosition)) {
//                     HighlightedObject = gameObject;
//                 }
//
//                 rect.y += ButtonYSize + ButtonYSpacing;
//             }
//         }
//
//         private void OnGuiNormal() {
//             var rect = new Rect(SizeXPadding, SizeYPadding, _buttonSize.x, _buttonSize.y);
//             for (var i = 0; i < _displayEntries.Count; i++) {
//                 var entry = _displayEntries[i];
//                 var gameObject = entry.GameObject;
//                 if (gameObject == null) {
//                     continue;
//                 }
//
//                 var indentOffset = entry.Depth * IndentWidth;
//                 var drawRect = new Rect(rect);
//                 drawRect.x += indentOffset;
//                 drawRect.width -= indentOffset;
//
//                 var content = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject));
//                 var objectSelected = Selection.Contains(gameObject);
//                 if (objectSelected) {
//                     GUI.backgroundColor = SelectionColor;
//                 }
//
//                 if (GUI.Button(drawRect, content, Style.ButtonStyle)) {
//                     GameObjectButtonPress(i);
//                 }
//
//                 GUI.backgroundColor = Color.white;
//                 rect.y += ButtonYSize + ButtonYSpacing;
//             }
//         }
//
//         private Vector2 CalculateSize() {
//             _size = Vector2.zero;
//
//             var maxIndent = 0f;
//             foreach (var entry in _displayEntries) {
//                 var content = EditorGUIUtility.ObjectContent(entry.GameObject, typeof(GameObject));
//                 var currentSize = Style.ButtonStyle.CalcSize(content);
//                 if (currentSize.x > _size.x) {
//                     _size.x = currentSize.x;
//                 }
//
//                 var currentIndent = entry.Depth * IndentWidth;
//                 if (currentIndent > maxIndent) {
//                     maxIndent = currentIndent;
//                 }
//             }
//
//             _size.x += maxIndent;
//             _size.x += SizeXOffset;
//
//             _buttonSize.x = _size.x;
//             _buttonSize.y = ButtonYSize;
//
//             _size.y = _displayEntries.Count * ButtonYSize + SizeYPadding * 2.0f + ButtonYSpacing * _displayEntries.Count - 1;
//             _size.x += SizeXPadding * 2.0f;
//
//             return _size;
//         }
//
//         private List<DisplayEntry> BuildDisplayEntries(List<GameObject> objectsUnderCursor) {
//             var entries = new List<DisplayEntry>();
//
//             if (objectsUnderCursor == null || objectsUnderCursor.Count == 0) {
//                 return entries;
//             }
//
//             var underCursorSet = new HashSet<Transform>();
//             foreach (var gameObject in objectsUnderCursor) {
//                 if (gameObject == null) {
//                     continue;
//                 }
//
//                 underCursorSet.Add(gameObject.transform);
//             }
//
//             var relevantTransforms = new HashSet<Transform>();
//             foreach (var transform in underCursorSet) {
//                 var current = transform;
//                 while (current != null && relevantTransforms.Add(current)) {
//                     current = current.parent;
//                 }
//             }
//
//             var orderedRoots = new List<Transform>();
//             foreach (var gameObject in objectsUnderCursor) {
//                 if (gameObject == null) {
//                     continue;
//                 }
//
//                 var top = gameObject.transform;
//                 while (top.parent != null && relevantTransforms.Contains(top.parent)) {
//                     top = top.parent;
//                 }
//
//                 if (!orderedRoots.Contains(top)) {
//                     orderedRoots.Add(top);
//                 }
//             }
//
//             foreach (var root in orderedRoots) {
//                 AppendHierarchy(root, 0, relevantTransforms, entries);
//             }
//
//             return entries;
//         }
//
//         private void AppendHierarchy(Transform current, int depth, HashSet<Transform> relevantTransforms, List<DisplayEntry> entries) {
//             entries.Add(new DisplayEntry(current.gameObject, depth));
//
//             for (var i = 0; i < current.childCount; i++) {
//                 var child = current.GetChild(i);
//                 if (!relevantTransforms.Contains(child)) {
//                     continue;
//                 }
//
//                 AppendHierarchy(child, depth + 1, relevantTransforms, entries);
//             }
//         }
//
//         private void GameObjectButtonPress(int id) {
//             SelectObject(id, Event.current.control, Event.current.shift);
//             if (Event.current.control || Event.current.shift) {
//                 return;
//             }
//
//             Close();
//         }
//
//         private void UpdateShiftSelectionIDs(int id) {
//             if (_shiftLastId == -1) {
//                 _shiftLastId = id;
//             }
//
//             if (_shiftMinSelectionId == -1) {
//                 _shiftMinSelectionId = id;
//             }
//
//             if (_shiftMaxSelectionId == -1) {
//                 _shiftMaxSelectionId = id;
//             }
//
//             if (id < _shiftMinSelectionId) {
//                 _shiftMinSelectionId = id;
//             }
//             else if (id >= _shiftMaxSelectionId) {
//                 _shiftMaxSelectionId = id;
//             }
//             else if (id > _shiftMinSelectionId) {
//                 if (_shiftLastId < id) {
//                     _shiftMaxSelectionId = id;
//                 }
//                 else {
//                     _shiftMinSelectionId = id;
//                 }
//             }
//
//             _shiftLastId = id;
//         }
//
//         private void SelectObject(int id, bool control, bool shift) {
//             var gameObject = _displayEntries[id].GameObject;
//
//             if (shift) {
//                 UpdateShiftSelectionIDs(id);
//                 SelectObjects(_shiftMinSelectionId, _shiftMaxSelectionId);
//             }
//             else if (control) {
//                 UpdateShiftSelectionIDs(id);
//                 if (Selection.Contains(gameObject)) {
//                     RemoveSelectedObject(gameObject);
//                 }
//                 else {
//                     AppendSelectedObject(gameObject);
//                 }
//             }
//             else {
//                 Selection.objects = new Object[] { gameObject };
//             }
//         }
//
//         private void SelectObjects(int minId, int maxId) {
//             var size = maxId - minId + 1;
//             var newSelection = new Object[size];
//
//             var index = 0;
//
//             for (var i = minId; i <= maxId; i++) {
//                 newSelection[index] = _displayEntries[i].GameObject;
//                 index++;
//             }
//
//             Selection.objects = newSelection;
//         }
//
//         private void AppendSelectedObject(GameObject gameObject) {
//             var currentSelection = Selection.objects;
//             var newSelection = new Object[currentSelection.Length + 1];
//
//             currentSelection.CopyTo(newSelection, 0);
//             newSelection[newSelection.Length - 1] = gameObject;
//
//             Selection.objects = newSelection;
//         }
//
//         private void RemoveSelectedObject(GameObject gameObject) {
//             var currentSelection = Selection.objects;
//             var newSelection = new Object[currentSelection.Length - 1];
//
//             var index = 0;
//
//             for (var i = 0; i < currentSelection.Length; i++) {
//                 if (currentSelection[i] == gameObject) {
//                     continue;
//                 }
//
//                 newSelection[index] = currentSelection[i];
//                 index++;
//             }
//
//             Selection.objects = newSelection;
//         }
//
//         private void UpdateHighlightedRenderers() {
//             _highlightedRenderers.Clear();
//
//             if (_highlightedObject == null) {
//                 return;
//             }
//
//             _highlightedRenderers.AddRange(_highlightedObject.GetComponentsInChildren<Renderer>(true));
//         }
//
// #if UNITY_2019_1_OR_NEWER
//         private void OnSceneViewGui(SceneView sceneView) {
//             if (_highlightedRenderers.Count == 0 || Event.current.type != EventType.Repaint) {
//                 return;
//             }
//
// #if UNITY_6000_1_OR_NEWER
//             Handles.DrawOutline(_highlightedRenderers.ToArray(), HighlightWireColor, HighlightWireColor, 0.5f);
// #else
//             using (new Handles.DrawingScope(HighlightWireColor)) {
//                 foreach (var renderer in _highlightedRenderers) {
//                     if (renderer == null) {
//                         continue;
//                     }
//
//                     var bounds = renderer.bounds;
//                     Handles.DrawWireCube(bounds.center, bounds.size);
//                 }
//             }
// #endif
//         }
// #endif
//
//         private GameObject HighlightedObject {
//             set {
//                 if (_highlightedObject == value) {
//                     return;
//                 }
//
//                 _highlightedObject = value;
//                 SceneView.RepaintAll();
//
//                 if (_highlightedObject != null) {
//                     EditorGUIUtility.PingObject(_highlightedObject);
//                 }
//
//                 UpdateHighlightedRenderers();
//             }
//         }
//
//         private readonly struct DisplayEntry {
//             internal DisplayEntry(GameObject gameObject, int depth) {
//                 GameObject = gameObject;
//                 Depth = depth;
//             }
//
//             internal GameObject GameObject { get; }
//             internal int Depth { get; }
//         }
//
//         private static class Style {
//             internal static readonly GUIStyle ButtonStyle;
//
//             static Style() {
//                 ButtonStyle = new GUIStyle(GUI.skin.button) {
//                     alignment = TextAnchor.MiddleLeft
//                 };
//             }
//         }
//     }
// }