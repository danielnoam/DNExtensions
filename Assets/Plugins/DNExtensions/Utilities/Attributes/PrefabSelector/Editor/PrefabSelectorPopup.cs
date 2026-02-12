#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.PrefabSelector
{
    public class PrefabSelectorPopup : EditorWindow
    {
        private PrefabInfo[] _allPrefabs;
        private PrefabInfo[] _filteredPrefabs;
        private string _searchQuery = "";
        private Vector2 _scrollPosition;
        private Action<GameObject> _onPrefabSelected;
        private bool _allowNull;
        private bool _showSearch;
        private int _selectedIndex = -1;
        private bool _focusSearchField = true;
        
        private const float ItemHeight = 20f;
        private const float WindowMaxHeight = 400f;
        
        public static void Show(Rect buttonRect, PrefabInfo[] prefabs, bool allowNull, bool showSearch, Action<GameObject> onPrefabSelected)
        {
            var window = CreateInstance<PrefabSelectorPopup>();
            window._allPrefabs = prefabs ?? Array.Empty<PrefabInfo>();
            window._filteredPrefabs = prefabs ?? Array.Empty<PrefabInfo>();
            window._allowNull = allowNull;
            window._showSearch = showSearch;
            window._onPrefabSelected = onPrefabSelected;
            
            // Calculate height
            float windowHeight = CalculateHeight(prefabs?.Length ?? 0, allowNull, showSearch);
            
            // ShowAsDropDown handles everything - positioning, closing on click outside, etc.
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), new Vector2(buttonRect.width, windowHeight));
        }
        
        private static float CalculateHeight(int itemCount, bool allowNull, bool showSearch)
        {
            float height = showSearch ? 26f : 4f;
            
            if (allowNull)
                height += ItemHeight + 6; // null item + separator
            
            height += itemCount * ItemHeight + 10f;
            
            return Mathf.Min(WindowMaxHeight, height);
        }
        
        private void OnGUI()
        {
            HandleKeyboard();
            DrawBackground();
            
            if (_showSearch)
                DrawSearchField();
            
            DrawPrefabList();
        }
        
        private void DrawBackground()
        {
            // Draw outline
            Rect bgRectOutline = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRectOutline, EditorGUIUtility.isProSkin 
                ? new Color(0.1f, 0.1f, 0.1f) 
                : new Color(0.5f, 0.5f, 0.5f));
            
            // Draw inner background
            Rect bgRect = new Rect(1, 1, position.width - 2, position.height - 2);
            EditorGUI.DrawRect(bgRect, EditorGUIUtility.isProSkin 
                ? new Color(0.22f, 0.22f, 0.22f) 
                : new Color(0.76f, 0.76f, 0.76f));
        }
        
        private void DrawSearchField()
        {
            Rect searchRect = new Rect(4, 4, position.width - 8, 18);
            
            GUI.SetNextControlName("SearchField");
            string newSearch = EditorGUI.TextField(searchRect, _searchQuery, EditorStyles.toolbarSearchField);
            
            if (newSearch != _searchQuery)
            {
                _searchQuery = newSearch;
                UpdateFilteredPrefabs();
                _selectedIndex = -1;
            }
            
            if (_focusSearchField)
            {
                EditorGUI.FocusTextInControl("SearchField");
                _focusSearchField = false;
            }
        }
        
        private void DrawPrefabList()
        {
            float yOffset = _showSearch ? 26f : 4f;
            Rect scrollViewRect = new Rect(0, yOffset, position.width, position.height - yOffset);
            
            float contentHeight = CalculateContentHeight();
            Rect contentRect = new Rect(0, 0, position.width - 20, contentHeight);
            
            _scrollPosition = GUI.BeginScrollView(scrollViewRect, _scrollPosition, contentRect, false, false);
            
            float currentY = 0;
            int currentIndex = 0;
            
            // Draw null option
            if (_allowNull)
            {
                bool isSelected = currentIndex == _selectedIndex;
                if (DrawPrefabItem(new Rect(0, currentY, contentRect.width, ItemHeight), null, "<null>", isSelected, currentIndex))
                {
                    _onPrefabSelected?.Invoke(null);
                    Close();
                }
                currentY += ItemHeight + 2;
                currentIndex++;
                
                // Separator
                EditorGUI.DrawRect(new Rect(4, currentY, contentRect.width - 8, 1), new Color(0.5f, 0.5f, 0.5f));
                currentY += 4;
            }
            
            // Draw prefabs
            foreach (var prefabInfo in _filteredPrefabs)
            {
                bool isSelected = currentIndex == _selectedIndex;
                if (DrawPrefabItem(new Rect(0, currentY, contentRect.width, ItemHeight), prefabInfo.Prefab, prefabInfo.DisplayName, isSelected, currentIndex))
                {
                    _onPrefabSelected?.Invoke(prefabInfo.Prefab);
                    Close();
                }
                currentY += ItemHeight;
                currentIndex++;
            }
            
            GUI.EndScrollView();
        }
        
        private bool DrawPrefabItem(Rect rect, GameObject prefab, string displayName, bool isSelected, int index)
        {
            Event e = Event.current;
            bool isHovered = rect.Contains(e.mousePosition);
            
            if (isHovered && e.type == EventType.MouseMove)
            {
                _selectedIndex = index;
                Repaint();
            }
            
            // Draw background
            if (isSelected || isHovered)
            {
                Color bgColor = isSelected 
                    ? (EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.8f) : new Color(0.24f, 0.48f, 0.90f, 0.4f))
                    : (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f));
                
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            // Draw prefab icon and name
            Rect iconRect = new Rect(rect.x + 4, rect.y + 2, 16, 16);
            Rect labelRect = new Rect(rect.x + 24, rect.y, rect.width - 24, rect.height);
            
            if (prefab != null)
            {
                Texture2D icon = AssetPreview.GetMiniThumbnail(prefab);
                if (icon != null)
                    GUI.DrawTexture(iconRect, icon);
            }
            
            GUI.Label(labelRect, displayName, EditorStyles.label);
            
            // Handle click
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            
            return false;
        }
        
        private float CalculateContentHeight()
        {
            int itemCount = _filteredPrefabs?.Length ?? 0;
            if (_allowNull)
                itemCount++;
            
            float separatorHeight = _allowNull ? 6f : 0f;
            return itemCount * ItemHeight + separatorHeight + 10f;
        }
        
        private void HandleKeyboard()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
                    
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_selectedIndex >= 0)
                    {
                        GameObject selected = GetPrefabAtIndex(_selectedIndex);
                        _onPrefabSelected?.Invoke(selected);
                        Close();
                        e.Use();
                    }
                    break;
                    
                case KeyCode.UpArrow:
                    _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                    e.Use();
                    Repaint();
                    break;
                    
                case KeyCode.DownArrow:
                    int totalItems = (_allowNull ? 1 : 0) + (_filteredPrefabs?.Length ?? 0);
                    _selectedIndex = Mathf.Min(totalItems - 1, _selectedIndex + 1);
                    e.Use();
                    Repaint();
                    break;
            }
        }
        
        private GameObject GetPrefabAtIndex(int index)
        {
            if (_allowNull)
            {
                if (index == 0) return null;
                index--;
            }
            
            if (_filteredPrefabs != null && index >= 0 && index < _filteredPrefabs.Length)
                return _filteredPrefabs[index].Prefab;
            
            return null;
        }
        
        private void UpdateFilteredPrefabs()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                _filteredPrefabs = _allPrefabs;
                return;
            }
            
            string query = _searchQuery.ToLower();
            _filteredPrefabs = _allPrefabs
                .Where(p => p.DisplayName.ToLower().Contains(query))
                .ToArray();
        }
        
        private void OnLostFocus()
        {
            Close();
        }
    }
}
#endif