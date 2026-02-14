#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Editor window for browsing and editing ScriptableObjects by type.
    /// Supports multi-selection and undo/redo.
    /// </summary>
    public class ScriptableObjectEditor : EditorWindow
    {
        private readonly List<Type> _allTypes = new List<Type>();
        private List<Type> _availableTypes = new List<Type>();
        private string[] _typeNames;
        private int _selectedTypeIndex;
        private Type _currentType;
        
        private List<ScriptableObject> _allAssets = new List<ScriptableObject>();
        private readonly HashSet<ScriptableObject> _selectedAssets = new HashSet<ScriptableObject>();
        
        private string _searchQuery = "";
        private Vector2 _listScrollPosition;
        private Vector2 _inspectorScrollPosition;
        
        private Editor _cachedEditor;
        private UnityEngine.Object[] _inspectorTargets;
        
        private ScriptableObject _renamingAsset;
        private string _renameText = "";
        private bool _renameNeedsFocus;
        private bool _isMultiRename;
        private int _lastSelectedIndex = -1;
        
        private const string RenameControlName = "SOEditor_RenameField";
        private const float ListWidth = 250f;
        private const float RowHeight = 20f;
        private const float ToggleWidth = 20f;
        
        [MenuItem("Tools/DNExtensions/ScriptableObject Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptableObjectEditor>("ScriptableObject Editor");
            window.minSize = new Vector2(600f, 400f);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshAvailableTypes();
            LoadPreferences();
            
            if (_availableTypes.Count > 0)
            {
                _selectedTypeIndex = Mathf.Clamp(_selectedTypeIndex, 0, _availableTypes.Count - 1);
                SelectType(_availableTypes[_selectedTypeIndex]);
            }
            
            Undo.undoRedoPerformed += OnUndoRedo;
        }
        
        private void OnDisable()
        {
            SavePreferences();
            DestroyEditor();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }
        
        private void OnUndoRedo()
        {
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    RefreshAssets();
                    Repaint();
                }
            };
        }
        
        private void OnGUI()
        {
            HandleRenameKeyboard();
            
            DrawToolbar();
            
            if (_currentType == null)
            {
                EditorGUILayout.HelpBox("No ScriptableObject types found in project.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            
            DrawListView();
            DrawInspectorView();
            
            EditorGUILayout.EndHorizontal();
            
            HandleGlobalKeyboard();
        }
        
        private void HandleRenameKeyboard()
        {
            if (_renamingAsset == null) return;
            if (Event.current.type != EventType.KeyDown) return;
            
            if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
            {
                FinishRename(_renamingAsset);
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Escape)
            {
                CancelRename();
                Event.current.Use();
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUI.BeginChangeCheck();
            _selectedTypeIndex = EditorGUILayout.Popup(_selectedTypeIndex, _typeNames, EditorStyles.toolbarPopup, GUILayout.Width(250f));
            if (EditorGUI.EndChangeCheck() && _availableTypes.Count > 0)
            {
                SelectType(_availableTypes[_selectedTypeIndex]);
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUI.BeginChangeCheck();
            _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200f));
            if (EditorGUI.EndChangeCheck())
            {
                FilterTypes();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawListView()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));
            
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);
            
            // Build multi-rename preview names if active
            Dictionary<ScriptableObject, string> renamePreview = null;
            if (_isMultiRename && _renamingAsset != null)
            {
                renamePreview = BuildRenamePreview();
            }
            
            foreach (ScriptableObject asset in _allAssets)
            {
                if (asset == null) continue;
                
                bool isSelected = _selectedAssets.Contains(asset);
                bool isRenaming = _renamingAsset == asset;
                
                Rect rowRect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
                
                if (isSelected)
                {
                    EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.4f));
                }
                
                if (isRenaming)
                {
                    DrawRenameRow(rowRect);
                }
                else if (_isMultiRename && isSelected && renamePreview != null && renamePreview.TryGetValue(asset, out string preview))
                {
                    DrawMultiRenamePreviewRow(rowRect, preview);
                }
                else
                {
                    DrawAssetRow(rowRect, asset, isSelected);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawListFooter();
            
            EditorGUILayout.EndVertical();
            
            Rect separatorRect = new Rect(ListWidth, 20f, 2f, position.height - 20f);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
        
        private void DrawRenameRow(Rect rowRect)
        {
            float fieldX = rowRect.x + 5f + ToggleWidth + 2f;
            Rect fieldRect = new Rect(fieldX, rowRect.y, rowRect.width - fieldX, rowRect.height);
            
            if (_isMultiRename)
            {
                Rect hintRect = new Rect(fieldX, rowRect.y - 14f, fieldRect.width, 14f);
                EditorGUI.LabelField(hintRect, "Use {0} for index", EditorStyles.miniLabel);
            }
            
            GUI.SetNextControlName(RenameControlName);
            _renameText = EditorGUI.TextField(fieldRect, _renameText);
            
            if (_renameNeedsFocus)
            {
                _renameNeedsFocus = false;
                EditorGUI.FocusTextInControl(RenameControlName);
                Repaint();
            }
        }
        
        private void DrawMultiRenamePreviewRow(Rect rowRect, string previewName)
        {
            Rect labelRect = new Rect(
                rowRect.x + 5f + ToggleWidth + 2f,
                rowRect.y,
                rowRect.width - 5f - ToggleWidth - 2f,
                rowRect.height);
            
            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            EditorGUI.LabelField(labelRect, previewName, EditorStyles.label);
            GUI.color = prevColor;
        }
        
        /// <summary>
        /// Builds a preview dictionary mapping each selected asset to its new name.
        /// Supports {0} as an index placeholder.
        /// </summary>
        private Dictionary<ScriptableObject, string> BuildRenamePreview()
        {
            var preview = new Dictionary<ScriptableObject, string>();
            
            List<ScriptableObject> ordered = _allAssets
                .Where(a => a != null && _selectedAssets.Contains(a))
                .ToList();
            
            for (int i = 0; i < ordered.Count; i++)
            {
                string previewName = _renameText.Contains("{0}")
                    ? _renameText.Replace("{0}", i.ToString())
                    : $"{_renameText}_{i}";
                
                preview[ordered[i]] = previewName;
            }
            
            return preview;
        }
        
        private void DrawAssetRow(Rect rowRect, ScriptableObject asset, bool isSelected)
        {
            Rect toggleRect = new Rect(rowRect.x + 5f, rowRect.y, ToggleWidth, rowRect.height);
            Rect labelRect = new Rect(toggleRect.xMax + 2f, rowRect.y, rowRect.width - toggleRect.xMax - 2f, rowRect.height);
            
            bool toggleValue = EditorGUI.Toggle(toggleRect, isSelected);
            if (toggleValue != isSelected)
            {
                if (toggleValue)
                    _selectedAssets.Add(asset);
                else
                    _selectedAssets.Remove(asset);
                
                UpdateInspector();
                Repaint();
            }
            
            EditorGUI.LabelField(labelRect, asset.name);
            
            if (_renamingAsset != null && _renamingAsset != asset)
            {
                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    FinishRename(_renamingAsset);
                }
            }
            
            HandleRowEvents(rowRect, asset);
        }
        
        private void HandleRowEvents(Rect rowRect, ScriptableObject asset)
        {
            Event e = Event.current;
            
            if (!rowRect.Contains(e.mousePosition)) return;
            
            if (e.type == EventType.ContextClick)
            {
                ShowAssetContextMenu(asset);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (e.clickCount == 2)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }
                else
                {
                    HandleSelection(asset, e);
                }
                e.Use();
            }
        }
        
        private void DrawListFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5f);
            GUILayout.Label($"Found: {_allAssets.Count}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(new GUIContent("+", "Create new asset"), EditorStyles.miniButtonLeft, GUILayout.Width(25f)))
            {
                CreateNewAsset();
            }
            if (GUILayout.Button(new GUIContent("↻", "Refresh asset list"), EditorStyles.miniButtonRight, GUILayout.Width(25f)))
            {
                RefreshAssets();
            }
            GUILayout.Space(5f);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }
        
        private void DrawInspectorView()
        {
            EditorGUILayout.BeginVertical();
            
            if (_selectedAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("Select one or more assets to edit.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Selected: {_selectedAssets.Count}", EditorStyles.boldLabel);
                
                EditorGUILayout.Space(5f);
                
                _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);
                
                if (_cachedEditor != null)
                {
                    try
                    {
                        _cachedEditor.OnInspectorGUI();
                    }
                    catch (Exception e)
                    {
                        EditorGUILayout.HelpBox(
                            "Unable to display inspector for the current selection. " +
                            "This may occur when selected objects have different array sizes in custom property drawers.\n\n" +
                            $"Error: {e.Message}", 
                            MessageType.Warning);
                        
                        if (GUILayout.Button("Select Single Object"))
                        {
                            ScriptableObject firstAsset = _selectedAssets.First();
                            _selectedAssets.Clear();
                            _selectedAssets.Add(firstAsset);
                            UpdateInspector();
                            Repaint();
                        }
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ShowAssetContextMenu(ScriptableObject asset)
        {
            // If right-clicking an unselected asset, select it
            if (!_selectedAssets.Contains(asset))
            {
                _selectedAssets.Clear();
                _selectedAssets.Add(asset);
                _lastSelectedIndex = _allAssets.IndexOf(asset);
                UpdateInspector();
            }
            
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Ping"), false, () =>
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            });
            
            string renameLabel = _selectedAssets.Count > 1
                ? $"Rename ({_selectedAssets.Count} assets)"
                : "Rename";
            
            menu.AddItem(new GUIContent(renameLabel), false, () =>
            {
                StartRename(asset);
            });
            
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                DuplicateAsset(asset);
            });
            
            menu.AddSeparator("");
            
            string deleteLabel = _selectedAssets.Count > 1
                ? $"Delete ({_selectedAssets.Count} assets)"
                : "Delete";
            
            menu.AddItem(new GUIContent(deleteLabel), false, () =>
            {
                DeleteAssets(_selectedAssets.Count > 1 ? _selectedAssets : new HashSet<ScriptableObject> { asset });
            });
            
            menu.ShowAsContext();
        }
        
        private void HandleSelection(ScriptableObject asset, Event evt)
        {
            if (evt.control || evt.command)
            {
                if (_selectedAssets.Contains(asset))
                    _selectedAssets.Remove(asset);
                else
                    _selectedAssets.Add(asset);
            }
            else if (evt.shift && _selectedAssets.Count > 0)
            {
                int startIndex = _lastSelectedIndex >= 0 ? _lastSelectedIndex : _allAssets.IndexOf(_selectedAssets.Last());
                int endIndex = _allAssets.IndexOf(asset);
                
                if (startIndex >= 0 && endIndex >= 0)
                {
                    int min = Mathf.Min(startIndex, endIndex);
                    int max = Mathf.Max(startIndex, endIndex);
                    
                    for (int i = min; i <= max; i++)
                    {
                        if (_allAssets[i] == null) continue;
                        _selectedAssets.Add(_allAssets[i]);
                    }
                }
            }
            else
            {
                _selectedAssets.Clear();
                _selectedAssets.Add(asset);
            }
            
            _lastSelectedIndex = _allAssets.IndexOf(asset);
            UpdateInspector();
            Repaint();
        }
        
        private void UpdateInspector()
        {
            DestroyEditor();
            
            if (_selectedAssets.Count > 0)
            {
                _inspectorTargets = _selectedAssets.Cast<UnityEngine.Object>().ToArray();
                _cachedEditor = Editor.CreateEditor(_inspectorTargets);
            }
        }
        
        private void HandleGlobalKeyboard()
        {
            if (_renamingAsset != null) return;
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
                    
                case KeyCode.F2 when _selectedAssets.Count > 0:
                    StartRename(_selectedAssets.Count == 1
                        ? _selectedAssets.First()
                        : _allAssets.First(a => _selectedAssets.Contains(a)));
                    Event.current.Use();
                    break;
                    
                case KeyCode.A when Event.current.control || Event.current.command:
                    _selectedAssets.Clear();
                    _selectedAssets.UnionWith(_allAssets.Where(a => a != null));
                    _lastSelectedIndex = _allAssets.Count - 1;
                    UpdateInspector();
                    Event.current.Use();
                    Repaint();
                    break;
                    
                case KeyCode.Delete when _selectedAssets.Count > 0:
                    DeleteAssets(_selectedAssets);
                    Event.current.Use();
                    break;
            }
        }
        
        /// <summary>
        /// Moves selection up or down in the asset list.
        /// Supports shift for extending selection.
        /// </summary>
        private void NavigateList(int direction, bool extendSelection)
        {
            if (_allAssets.Count == 0) return;
            
            int startIndex = _lastSelectedIndex >= 0 ? _lastSelectedIndex : 0;
            int newIndex = Mathf.Clamp(startIndex + direction, 0, _allAssets.Count - 1);
            
            if (newIndex == startIndex && _selectedAssets.Count > 0) return;
            
            ScriptableObject asset = _allAssets[newIndex];
            if (asset == null) return;
            
            if (extendSelection)
            {
                _selectedAssets.Add(asset);
            }
            else
            {
                _selectedAssets.Clear();
                _selectedAssets.Add(asset);
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
            float scrollBottom = scrollTop + (position.height - 40f);
            
            if (itemTop < scrollTop)
                _listScrollPosition.y = itemTop;
            else if (itemBottom > scrollBottom)
                _listScrollPosition.y = itemBottom - (position.height - 40f);
        }
        
        private void RefreshAvailableTypes()
        {
            _allTypes.Clear();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                
                if (assemblyName.Contains("Unity") || assemblyName.Contains("Editor"))
                    continue;
                
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(ScriptableObject)) && !t.IsAbstract)
                        .Where(t => !IsUnityOrEditorType(t))
                        .OrderBy(t => t.Name);
                    
                    _allTypes.AddRange(types);
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                }
            }
            
            FilterTypes();
        }
        
        private void FilterTypes()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                _availableTypes = new List<Type>(_allTypes);
            }
            else
            {
                string query = _searchQuery.ToLower();
                _availableTypes = _allTypes
                    .Where(t => t.Name.ToLower().Contains(query))
                    .ToList();
            }
            
            _typeNames = _availableTypes.Select(t => t.Name).ToArray();
            
            if (_availableTypes.Count > 0)
            {
                _selectedTypeIndex = Mathf.Clamp(_selectedTypeIndex, 0, _availableTypes.Count - 1);
            }
        }
        
        private static bool IsUnityOrEditorType(Type type)
        {
            if (type.Namespace == null)
                return false;
            
            return type.Namespace.StartsWith("Unity") || 
                   type.Namespace.StartsWith("UnityEngine") || 
                   type.Namespace.StartsWith("UnityEditor");
        }
        
        private void SelectType(Type type)
        {
            _currentType = type;
            RefreshAssets();
        }
        
        private void RefreshAssets()
        {
            _allAssets.Clear();
            _selectedAssets.Clear();
            _lastSelectedIndex = -1;
            DestroyEditor();
            
            if (_currentType == null)
                return;
            
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (!path.StartsWith("Assets/"))
                    continue;
                
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath(path, _currentType) as ScriptableObject;
                
                if (asset != null && asset.GetType() == _currentType)
                {
                    _allAssets.Add(asset);
                }
            }
            
            _allAssets = _allAssets.OrderBy(a => a.name).ToList();
        }
        
        private void DestroyEditor()
        {
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }
            _inspectorTargets = null;
        }
        
        /// <summary>
        /// Creates a new asset of the currently selected type in the active project folder.
        /// </summary>
        private void CreateNewAsset()
        {
            if (_currentType == null) return;
            
            ScriptableObject instance = CreateInstance(_currentType);
            
            string folder = "Assets";
            
            // Use the same folder as the first existing asset if available
            if (_allAssets.Count > 0 && _allAssets[0] != null)
            {
                string existingPath = AssetDatabase.GetAssetPath(_allAssets[0]);
                folder = System.IO.Path.GetDirectoryName(existingPath);
            }
            
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New {_currentType.Name}.asset");
            
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            
            Undo.RegisterCreatedObjectUndo(instance, $"Create {_currentType.Name}");
            
            RefreshAssets();
            
            // Select and start renaming the new asset
            ScriptableObject newAsset = _allAssets.FirstOrDefault(a => a == instance);
            if (newAsset != null)
            {
                _selectedAssets.Add(newAsset);
                _lastSelectedIndex = _allAssets.IndexOf(newAsset);
                UpdateInspector();
                StartRename(newAsset);
            }
        }
        
        /// <summary>
        /// Duplicates a ScriptableObject asset and refreshes the list.
        /// </summary>
        private void DuplicateAsset(ScriptableObject asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string directory = System.IO.Path.GetDirectoryName(path);
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);
            string extension = System.IO.Path.GetExtension(path);
            
            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{filename}{extension}");
            
            if (!AssetDatabase.CopyAsset(path, newPath)) return;
            
            AssetDatabase.ImportAsset(newPath);
            
            ScriptableObject newAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(newPath);
            if (newAsset != null)
            {
                Undo.RegisterCreatedObjectUndo(newAsset, "Duplicate ScriptableObject");
                EditorUtility.SetDirty(newAsset);
            }
            
            RefreshAssets();
        }
        
        /// <summary>
        /// Deletes the given assets after user confirmation and refreshes the list.
        /// </summary>
        private void DeleteAssets(IEnumerable<ScriptableObject> assets)
        {
            ScriptableObject[] toDelete = assets.Where(a => a != null).ToArray();
            if (toDelete.Length == 0) return;
            
            string message = toDelete.Length == 1
                ? $"Delete '{toDelete[0].name}'?"
                : $"Delete {toDelete.Length} selected asset(s)?";
            
            if (!EditorUtility.DisplayDialog("Delete Assets", message, "Delete", "Cancel")) return;
            
            foreach (ScriptableObject asset in toDelete)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.DeleteAsset(path);
            }
            
            RefreshAssets();
        }
        
        private void LoadPreferences()
        {
            string typeName = EditorPrefs.GetString("DNExtensions.SOEditor.LastType", "");
            if (!string.IsNullOrEmpty(typeName))
            {
                _selectedTypeIndex = Array.FindIndex(_typeNames, t => t == typeName);
                if (_selectedTypeIndex < 0)
                    _selectedTypeIndex = 0;
            }
        }
        
        private void SavePreferences()
        {
            if (_currentType != null)
            {
                EditorPrefs.SetString("DNExtensions.SOEditor.LastType", _currentType.Name);
            }
        }
        
        private void StartRename(ScriptableObject asset)
        {
            _renamingAsset = asset;
            _renameText = asset.name;
            _isMultiRename = _selectedAssets.Count > 1;
            _renameNeedsFocus = true;
            Repaint();
        }
        
        private void FinishRename(ScriptableObject asset)
        {
            string newName = _renameText;
            bool isMulti = _isMultiRename;
            
            // Capture selected assets before CancelRename clears state
            List<ScriptableObject> assetsToRename = isMulti
                ? _allAssets.Where(a => a != null && _selectedAssets.Contains(a)).ToList()
                : null;
            
            CancelRename();
            
            if (string.IsNullOrWhiteSpace(newName))
                return;
            
            if (isMulti)
            {
                FinishMultiRename(assetsToRename, newName);
            }
            else
            {
                FinishSingleRename(asset, newName);
            }
        }
        
        private void FinishSingleRename(ScriptableObject asset, string newName)
        {
            if (newName == asset.name) return;
            
            string path = AssetDatabase.GetAssetPath(asset);
            string error = AssetDatabase.RenameAsset(path, newName);
            
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Failed to rename asset: {error}");
                return;
            }
            
            RefreshAssets();
            
            ScriptableObject renamed = _allAssets.FirstOrDefault(a => a != null && a.name == newName);
            if (renamed != null)
            {
                _selectedAssets.Add(renamed);
                UpdateInspector();
            }
        }
        
        private void FinishMultiRename(List<ScriptableObject> assets, string pattern)
        {
            AssetDatabase.StartAssetEditing();
            
            try
            {
                var renamedNames = new List<string>();
                
                for (int i = 0; i < assets.Count; i++)
                {
                    ScriptableObject asset = assets[i];
                    string newName = pattern.Contains("{0}")
                        ? pattern.Replace("{0}", i.ToString())
                        : $"{pattern}_{i}";
                    
                    if (newName == asset.name) continue;
                    
                    string path = AssetDatabase.GetAssetPath(asset);
                    string error = AssetDatabase.RenameAsset(path, newName);
                    
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogError($"Failed to rename '{asset.name}': {error}");
                    else
                        renamedNames.Add(newName);
                }
                
                RefreshAssets();
                
                // Reselect all renamed assets
                foreach (string renamedName in renamedNames)
                {
                    ScriptableObject renamed = _allAssets.FirstOrDefault(a => a != null && a.name == renamedName);
                    if (renamed != null)
                        _selectedAssets.Add(renamed);
                }
                
                UpdateInspector();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
        
        private void CancelRename()
        {
            _renamingAsset = null;
            _renameText = "";
            _isMultiRename = false;
            Repaint();
        }
    }
}
#endif