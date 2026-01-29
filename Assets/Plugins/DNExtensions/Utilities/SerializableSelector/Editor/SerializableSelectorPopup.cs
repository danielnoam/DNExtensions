#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.Utilities.SerializableSelector.Editor
{
    using TypeInfo = SerializableSelectorUtility.TypeInfo;
    
    public class SerializableSelectorPopup : EditorWindow
    {
        private TypeInfo[] _allTypes;
        private TypeInfo[] _filteredTypes;
        private HashSet<Type> _existingTypes;
        private string _searchQuery = "";
        private Vector2 _scrollPosition;
        private Action<Type> _onTypeSelected;
        private bool _allowNull;
        private bool _showSearch;
        private bool _showNamespaceHeaders;
        private int _selectedIndex = -1;
        private bool _focusSearchField = true;
        
        private const float WindowMaxHeight = 800f;
        private const float ItemHeight = 20f;
        
        private static GUIStyle _headerStyle;
        private static GUIStyle _itemStyle;
        private static GUIStyle _itemStyleSelected;
        
        public static void Show(Rect buttonRect, TypeInfo[] types, bool allowNull, bool showSearch, bool showNamespaceHeaders, HashSet<Type> existingTypes, Action<Type> onTypeSelected)
        {
            var window = CreateInstance<SerializableSelectorPopup>();
            window._allTypes = types ?? Array.Empty<TypeInfo>();
            window._filteredTypes = types ?? Array.Empty<TypeInfo>();
            window._existingTypes = existingTypes ?? new HashSet<Type>();
            window._allowNull = allowNull;
            window._showSearch = showSearch;
            window._showNamespaceHeaders = showNamespaceHeaders;
            window._onTypeSelected = onTypeSelected;
    
            // Use dropdown button width
            float windowWidth = buttonRect.width;
    
            // Calculate window height based on content
            float windowHeight = CalculateInitialWindowHeight(types ?? Array.Empty<TypeInfo>(), allowNull, showSearch);
    
            Vector2 windowPosition = GUIUtility.GUIToScreenPoint(
                new Vector2(buttonRect.x, buttonRect.y + buttonRect.height)
            );
    
            // Ensure window stays on screen
            float screenHeight = Screen.currentResolution.height;
            if (windowPosition.y + windowHeight > screenHeight)
            {
                windowPosition.y = buttonRect.y - windowHeight;
            }
    
            window.position = new Rect(
                windowPosition.x,
                windowPosition.y,
                windowWidth,
                windowHeight
            );
    
            window.ShowPopup();
            window.Focus();
        }

        
        private static float CalculateInitialWindowHeight(TypeInfo[] types, bool allowNull, bool showSearch)
        {
            float searchAreaHeight;
            if (types == null || types.Length == 0)
            {
                searchAreaHeight = showSearch ? 30f : 0f; // Reserve space for search + possible results count
                return Mathf.Min(WindowMaxHeight, searchAreaHeight); 
            }
    
            // Count items
            int itemCount = types.Length;
            if (allowNull) itemCount++; // +1 for null option
    
            // Count category headers
            int categoryCount = types
                .Select(t => t.Category)
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Distinct()
                .Count();
    
            // Calculate heights
            searchAreaHeight = showSearch ? 30f : 0f; // Fixed space for search field + potential results
            float nullItemHeight = allowNull ? ItemHeight + 2 : 0f; // Null item + spacing
            float separatorHeight = allowNull ? 4f : 0f; // Separator after null
            float itemsHeight = itemCount * ItemHeight;
            float categoryHeight = categoryCount * ItemHeight;
            float groupSpacing = categoryCount * 2f; // Small spacing between groups
    
            float totalHeight = searchAreaHeight + nullItemHeight + separatorHeight + itemsHeight + categoryHeight + groupSpacing;
    
            return Mathf.Min(WindowMaxHeight, totalHeight);
        }
        
        
        private void InitializeStyles()
        {
            // Only initialize if not already done AND EditorStyles is ready
            if (_headerStyle != null) return;
            
            try
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    padding = new RectOffset(6, 6, 2, 2)
                };
                
                _itemStyle = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(12, 6, 2, 2),
                    margin = new RectOffset(0, 0, 0, 0)
                };
                
                _itemStyleSelected = new GUIStyle(_itemStyle)
                {
                    normal = new GUIStyleState
                    {
                        background = CreateColorTexture(new Color(0.24f, 0.48f, 0.90f, 0.4f)),
                        textColor = EditorStyles.label.normal.textColor
                    }
                };
            }
            catch
            {
                // If EditorStyles isn't ready yet, styles will be null,
                // and we'll use fallback rendering
            }
        }
        
        private void OnGUI()
        {
            // Initialize styles on first GUI call (when EditorStyles is ready)
            if (_headerStyle == null)
            {
                InitializeStyles();
            }
            
            // Safety check
            _filteredTypes ??= _allTypes ?? Array.Empty<TypeInfo>();
            
            // Handle keyboard shortcuts
            HandleKeyboardInput();
            
            // Draw with Unity's default background
            DrawBackground();
            
            // Draw search field if enabled
            if (_showSearch)
            {
                DrawSearchField();
            }
            
            // Draw type list
            DrawTypeList();
        }
        
        private void DrawBackground()
        {
            // Draw outline (larger rect, darker color)
            Rect bgRectOutline = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRectOutline, EditorGUIUtility.isProSkin 
                ? new Color(0.1f, 0.1f, 0.1f) 
                : new Color(0.5f, 0.5f, 0.5f));
    
            // Draw inner background (smaller rect, inset by 1 pixel on all sides)
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
                UpdateFilteredTypes();
                _selectedIndex = -1;
            }
            
            // Autofocus search field
            if (_focusSearchField)
            {
                EditorGUI.FocusTextInControl("SearchField");
                _focusSearchField = false;
            }
            
            // Show result count
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                Rect countRect = new Rect(4, 24, position.width - 8, 14);
                GUI.Label(countRect, $"{_filteredTypes.Length} of {_allTypes.Length} types", EditorStyles.miniLabel);
            }
        }
        
        private void DrawTypeList()
        {
            float yOffset = _showSearch ? (string.IsNullOrEmpty(_searchQuery) ? 26f : 40f) : 4f;
            Rect scrollViewRect = new Rect(0, yOffset, position.width, position.height - yOffset);
            
            float contentHeight = CalculateContentHeight();
            Rect contentRect = new Rect(0, 0, position.width - 20, contentHeight);
            
            _scrollPosition = GUI.BeginScrollView(scrollViewRect, _scrollPosition, contentRect, false, true);
            
            float currentY = 0;
            int currentIndex = 0;
            
            // Draw null option
            if (_allowNull)
            {
                bool isSelected = currentIndex == _selectedIndex;
                if (DrawTypeItem(new Rect(0, currentY, contentRect.width, ItemHeight), 
                    null, "<null>", "Clear the reference", isSelected, currentIndex, false)) 
                {
                    _onTypeSelected?.Invoke(null);
                    Close();
                }
                currentY += ItemHeight + 2;
                currentIndex++;
                
                // Separator
                DrawSeparator(new Rect(4, currentY, contentRect.width - 8, 1));
                currentY += 3;
            }
            
            // Safety check
            if (_filteredTypes == null || _filteredTypes.Length == 0)
            {
                GUI.EndScrollView();
                return;
            }
            
            // Group by Category
            var grouped = _filteredTypes
                .GroupBy(t => string.IsNullOrEmpty(t.Category) ? "" : t.Category)
                .OrderBy(g => g.Key);
            
            foreach (var group in grouped)
            {
                // Draw Category header if enabled and not empty
                if (_showNamespaceHeaders && !string.IsNullOrEmpty(group.Key))
                {
                    Rect headerRect = new Rect(0, currentY, contentRect.width, ItemHeight);
                    
                    // Use fallback style if _headerStyle is null
                    GUIStyle headerStyle = _headerStyle ?? EditorStyles.boldLabel;
                    GUI.Label(headerRect, group.Key, headerStyle);
                    currentY += ItemHeight;
                }
                
                // Draw types in group
                foreach (TypeInfo typeInfo in group.OrderBy(t => t.DisplayName))
                {
                    bool isSelected = currentIndex == _selectedIndex;
                    Rect itemRect = new Rect(0, currentY, contentRect.width, ItemHeight);
                    
                    if (DrawTypeItem(itemRect, typeInfo.Type, typeInfo.DisplayName, typeInfo.Tooltip, isSelected, currentIndex, typeInfo.AllowOnce))  // ← Pass allowOnce flag
                    {
                        _onTypeSelected?.Invoke(typeInfo.Type);
                        Close();
                    }
                    
                    currentY += ItemHeight;
                    currentIndex++;
                }
                
                // Small spacing between groups
                currentY += 2;
            }
            
            GUI.EndScrollView();
        }
        
        
        private bool DrawTypeItem(Rect rect, Type type, string displayName, string tooltip, bool isSelected, int index, bool allowOnce)  // ← NEW parameter
        {
            Event e = Event.current;
            bool isHovered = rect.Contains(e.mousePosition);
            
            // Check if this type is already in the list and marked AllowOnce
            bool isDisabled = allowOnce && type != null && _existingTypes.Contains(type);
            
            // Update selected index on hover (but not for disabled items)
            if (isHovered && e.type == EventType.MouseMove && !isDisabled)
            {
                _selectedIndex = index;
                Repaint();
            }
            
            // Draw background for selection/hover
            if (!isDisabled && (isSelected || isHovered))
            {
                Color bgColor = isSelected 
                    ? (EditorGUIUtility.isProSkin ? new Color(0.24f, 0.48f, 0.90f, 0.8f) : new Color(0.24f, 0.48f, 0.90f, 0.4f))
                    : (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.1f));
                
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            // Draw type name with disabled color if needed
            GUIContent content = new GUIContent(displayName, tooltip);
            GUIStyle itemStyle = _itemStyle ?? EditorStyles.label;
            
            if (isDisabled)
            {
                // Create disabled style
                GUIStyle disabledStyle = new GUIStyle(itemStyle)
                {
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin 
                            ? new Color(0.5f, 0.5f, 0.5f) 
                            : new Color(0.6f, 0.6f, 0.6f)
                    }
                };

                GUI.Label(rect, content, disabledStyle);
                
                // Show "Already in list" tooltip
                if (isHovered)
                {
                    DrawTooltip(rect, "Already in list (marked as AllowOnce)");
                }
            }
            else
            {
                GUI.Label(rect, content, itemStyle);
                
                // Draw tooltip on hover
                if (isHovered && !string.IsNullOrEmpty(tooltip))
                {
                    DrawTooltip(rect, tooltip);
                }
            }
            
            // Handle click (disabled items can't be clicked)
            if (!isDisabled && e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            
            return false;
        }
        
        private void DrawTooltip(Rect itemRect, string tooltip)
        {
            GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 4, 4),
                fontSize = 11
            };
            
            GUIContent tooltipContent = new GUIContent(tooltip);
            Vector2 tooltipSize = tooltipStyle.CalcSize(tooltipContent);
            tooltipSize.x = Mathf.Min(tooltipSize.x, 250f);
            tooltipSize.y = Mathf.Max(tooltipSize.y, 20f);
            
            // Position tooltip to the right, or left if not enough space
            float tooltipX = itemRect.xMax + 8;
            if (tooltipX + tooltipSize.x > position.width)
            {
                tooltipX = itemRect.x - tooltipSize.x - 8;
            }
            
            Rect tooltipRect = new Rect(tooltipX, itemRect.y - 2, tooltipSize.x, tooltipSize.y);
            
            // Draw shadow
            Rect shadowRect = tooltipRect;
            shadowRect.x += 2;
            shadowRect.y += 2;
            EditorGUI.DrawRect(shadowRect, new Color(0, 0, 0, 0.3f));
            
            // Draw tooltip
            GUI.Box(tooltipRect, tooltipContent, tooltipStyle);
        }
        
        private void DrawSeparator(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.6f, 0.6f, 0.6f));
        }
        
        private float CalculateContentHeight()
        {
            // Safety check
            if (_filteredTypes == null || _filteredTypes.Length == 0)
                return ItemHeight * 2; // Minimum height
    
            int itemCount = _filteredTypes.Length;
            if (_allowNull)
            {
                itemCount++; // +1 for null option
            }
    
            // Add namespace headers
            int namespaceCount = _filteredTypes
                .Select(t => t.Category)
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Distinct()
                .Count();
    
            // Calculate total
            float nullSeparatorHeight = _allowNull ? 5f : 0f;
            float itemsHeight = itemCount * ItemHeight;
            float namespacesHeight = namespaceCount * ItemHeight;
            float groupSpacing = namespaceCount * 2f;
            float padding = 10f;
    
            return nullSeparatorHeight + itemsHeight + namespacesHeight + groupSpacing + padding;
        }
        
        private void HandleKeyboardInput()
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
                    if (_selectedIndex >= 0 && _selectedIndex < GetTotalItemCount())
                    {
                        Type selectedType = GetTypeAtIndex(_selectedIndex);
                        _onTypeSelected?.Invoke(selectedType);
                        Close();
                        e.Use();
                    }
                    break;
                    
                case KeyCode.UpArrow:
                    _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                    ScrollToSelected();
                    e.Use();
                    Repaint();
                    break;
                    
                case KeyCode.DownArrow:
                    _selectedIndex = Mathf.Min(GetTotalItemCount() - 1, _selectedIndex + 1);
                    ScrollToSelected();
                    e.Use();
                    Repaint();
                    break;
            }
        }
        
        private int GetTotalItemCount()
        {
            int count = _filteredTypes?.Length ?? 0;
            if (_allowNull) count++;
            return count;
        }
        
        private Type GetTypeAtIndex(int index)
        {
            if (_allowNull)
            {
                if (index == 0) return null;
                index--;
            }
            
            if (_filteredTypes != null && index >= 0 && index < _filteredTypes.Length)
                return _filteredTypes[index].Type;
            
            return null;
        }
        
        private void ScrollToSelected()
        {
            if (_selectedIndex < 0) return;
            
            float itemY = _selectedIndex * ItemHeight;
            float viewportHeight = position.height - (_showSearch ? 26f : 4f);
            
            if (itemY < _scrollPosition.y)
            {
                _scrollPosition.y = itemY;
            }
            else if (itemY + ItemHeight > _scrollPosition.y + viewportHeight)
            {
                _scrollPosition.y = itemY + ItemHeight - viewportHeight;
            }
        }
        
        private void UpdateFilteredTypes()
        {
            _filteredTypes = SerializableSelectorUtility.FilterBySearch(_allTypes ?? Array.Empty<TypeInfo>(), _searchQuery);
        }
        
        private void OnLostFocus()
        {
            Close();
        }
        
        private static Texture2D CreateColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
#endif