#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNExtensions.Utilities
{
    /// <summary>
    /// Generic popup window for selecting Unity assets with search and keyboard navigation.
    /// </summary>
    /// <typeparam name="T">Type of Unity asset to select</typeparam>
    public class AssetSelectorPopup<T> : EditorWindow where T : Object
    {
        private AssetInfo<T>[] _allAssets;
        private AssetInfo<T>[] _filteredAssets;
        private string _searchQuery = "";
        private Vector2 _scrollPosition;
        private Action<T> _onAssetSelected;
        private bool _allowNull;
        private bool _showSearch;
        private int _selectedIndex = -1;
        private bool _focusSearchField = true;
        
        private const float ItemHeight = 20f;
        private const float WindowMaxHeight = 400f;
        
        /// <summary>
        /// Shows the asset selector popup below the specified button rect.
        /// Called from concrete derived classes.
        /// </summary>
        protected static TWindow ShowPopup<TWindow>(Rect buttonRect, AssetInfo<T>[] assets, bool allowNull, bool showSearch, Action<T> onAssetSelected) 
            where TWindow : AssetSelectorPopup<T>
        {
            var window = ScriptableObject.CreateInstance<TWindow>();
            if (window == null)
            {
                Debug.LogError($"Failed to create {typeof(TWindow).Name} instance");
                return null;
            }
            
            window._allAssets = assets ?? Array.Empty<AssetInfo<T>>();
            window._filteredAssets = assets ?? Array.Empty<AssetInfo<T>>();
            window._allowNull = allowNull;
            window._showSearch = showSearch;
            window._onAssetSelected = onAssetSelected;
            
            float windowHeight = CalculateHeight(assets?.Length ?? 0, allowNull, showSearch);
            
            Rect screenRect = GUIUtility.GUIToScreenRect(buttonRect);
            window.ShowAsDropDown(screenRect, new Vector2(buttonRect.width, windowHeight));
            
            return window;
        }
        
        private static float CalculateHeight(int itemCount, bool allowNull, bool showSearch)
        {
            float height = showSearch ? 26f : 4f;
            
            if (allowNull)
                height += ItemHeight + 6;
            
            height += itemCount * ItemHeight + 10f;
            
            return Mathf.Min(WindowMaxHeight, height);
        }
        
        private void OnGUI()
        {
            HandleKeyboard();
            DrawBackground();
            
            if (_showSearch)
                DrawSearchField();
            
            DrawAssetList();
        }
        
        private void DrawBackground()
        {
            Rect bgRectOutline = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRectOutline, EditorGUIUtility.isProSkin 
                ? new Color(0.1f, 0.1f, 0.1f) 
                : new Color(0.5f, 0.5f, 0.5f));
            
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
                UpdateFilteredAssets();
                _selectedIndex = -1;
            }
            
            if (_focusSearchField)
            {
                EditorGUI.FocusTextInControl("SearchField");
                _focusSearchField = false;
            }
        }
        
        private void DrawAssetList()
        {
            float yOffset = _showSearch ? 26f : 4f;
            Rect scrollViewRect = new Rect(0, yOffset, position.width, position.height - yOffset);
            
            float contentHeight = CalculateContentHeight();
            Rect contentRect = new Rect(0, 0, position.width - 20, contentHeight);
            
            _scrollPosition = GUI.BeginScrollView(scrollViewRect, _scrollPosition, contentRect, false, false);
            
            float currentY = 0;
            int currentIndex = 0;
            
            if (_allowNull)
            {
                bool isSelected = currentIndex == _selectedIndex;
                if (DrawAssetItem(new Rect(0, currentY, contentRect.width, ItemHeight), default, "<null>", isSelected, currentIndex))
                {
                    _onAssetSelected?.Invoke(null);
                    Close();
                }
                currentY += ItemHeight + 2;
                currentIndex++;
                
                EditorGUI.DrawRect(new Rect(4, currentY, contentRect.width - 8, 1), new Color(0.5f, 0.5f, 0.5f));
                currentY += 4;
            }
            
            foreach (var assetInfo in _filteredAssets)
            {
                bool isSelected = currentIndex == _selectedIndex;
                if (DrawAssetItem(new Rect(0, currentY, contentRect.width, ItemHeight), assetInfo.Asset, assetInfo.DisplayName, isSelected, currentIndex))
                {
                    _onAssetSelected?.Invoke(assetInfo.Asset);
                    Close();
                }
                currentY += ItemHeight;
                currentIndex++;
            }
            
            GUI.EndScrollView();
        }
        
        private bool DrawAssetItem(Rect rect, T asset, string displayName, bool isSelected, int index)
        {
            Event e = Event.current;
            bool isHovered = rect.Contains(e.mousePosition);
            
            if (isHovered && e.type == EventType.MouseMove)
            {
                _selectedIndex = index;
                Repaint();
            }
            
            if (isSelected || isHovered)
            {
                Color bgColor = isSelected 
                    ? (EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.8f) : new Color(0.24f, 0.48f, 0.90f, 0.4f))
                    : (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f));
                
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            Rect iconRect = new Rect(rect.x + 4, rect.y + 2, 16, 16);
            Rect labelRect = new Rect(rect.x + 24, rect.y, rect.width - 24, rect.height);
            
            if (asset != null)
            {
                Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
                if (icon != null)
                    GUI.DrawTexture(iconRect, icon);
            }
            
            GUI.Label(labelRect, displayName, EditorStyles.label);
            
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            
            return false;
        }
        
        private float CalculateContentHeight()
        {
            int itemCount = _filteredAssets?.Length ?? 0;
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
                        T selected = GetAssetAtIndex(_selectedIndex);
                        _onAssetSelected?.Invoke(selected);
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
                    int totalItems = (_allowNull ? 1 : 0) + (_filteredAssets?.Length ?? 0);
                    _selectedIndex = Mathf.Min(totalItems - 1, _selectedIndex + 1);
                    e.Use();
                    Repaint();
                    break;
            }
        }
        
        private T GetAssetAtIndex(int index)
        {
            if (_allowNull)
            {
                if (index == 0) return null;
                index--;
            }
            
            if (_filteredAssets != null && index >= 0 && index < _filteredAssets.Length)
                return _filteredAssets[index].Asset;
            
            return null;
        }
        
        private void UpdateFilteredAssets()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                _filteredAssets = _allAssets;
                return;
            }
            
            string query = _searchQuery.ToLower();
            _filteredAssets = _allAssets
                .Where(a => a.DisplayName.ToLower().Contains(query))
                .ToArray();
        }
        
        private void OnLostFocus()
        {
            Close();
        }
    }
}
#endif