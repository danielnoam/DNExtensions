using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

namespace DNExtensions.GridSystem
{
    [CustomPropertyDrawer(typeof(Grid))]
    public class GridDrawer : PropertyDrawer
    {
        private const int MinSize = 1;
        private const int MaxSize = 20;
        
        private const float CellSize = 16f;
        private const float ButtonHeight = 20f;
        private const float Spacing = 5f;
        private const float CellBorder = 1f;
        
        private static readonly Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color ActiveTileColor = new Color(0.3f, 0.7f, 0.3f);
        private static readonly Color InactiveTileColor = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color GridLineColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.3f);

        private bool _isDragging;
        private bool _dragState;
        private bool _foldout = true;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_foldout)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            
            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty cellSizeProp = property.FindPropertyRelative("cellSize");
            SerializedProperty originProp = property.FindPropertyRelative("origin");
            SerializedProperty cellSpacingProp = property.FindPropertyRelative("cellSpacing");
            
            int height = sizeProp.vector2IntValue.y;

            float gridHeight = height * CellSize;
            
            float cellSizeHeight = EditorGUI.GetPropertyHeight(cellSizeProp);
            float cellSpacingHeight = EditorGUI.GetPropertyHeight(cellSpacingProp);
            float originHeight = EditorGUI.GetPropertyHeight(originProp);
            
            float totalHeight = EditorGUIUtility.singleLineHeight + Spacing + 
                               EditorGUIUtility.singleLineHeight + Spacing + 
                               EditorGUIUtility.singleLineHeight + Spacing +
                               originHeight + Spacing +
                               cellSizeHeight + Spacing +
                               cellSpacingHeight + Spacing + 
                               EditorGUIUtility.singleLineHeight + Spacing + 
                               EditorGUIUtility.singleLineHeight + Spacing +
                               gridHeight + Spacing + 
                               ButtonHeight + Spacing +
                               ButtonHeight;  

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty cellsProp = property.FindPropertyRelative("cells");
            
            Vector2Int size = sizeProp.vector2IntValue;
            int activeCount = GetActiveCount(cellsProp);
            
            Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            string summaryText = $"{label.text} ({size.x}x{size.y}, {activeCount} active)";
            _foldout = EditorGUI.Foldout(currentRect, _foldout, summaryText, true);
            
            if (!_foldout)
            {
                EditorGUI.EndProperty();
                return;
            }
            
            currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

            SerializedProperty originProp = property.FindPropertyRelative("origin");
            SerializedProperty cellSizeProp = property.FindPropertyRelative("cellSize");
            SerializedProperty cellSpacingProp = property.FindPropertyRelative("cellSpacing");

            int width = size.x;
            int height = size.y;

            EditorGUI.BeginChangeCheck();
            int newWidth = EditorGUI.IntSlider(currentRect, "Width", width, MinSize, MaxSize);
            currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

            int newHeight = EditorGUI.IntSlider(currentRect, "Height", height, MinSize, MaxSize);
            currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

            if (EditorGUI.EndChangeCheck())
            {
                ResizeGrid(property, newWidth, newHeight);
                size = new Vector2Int(newWidth, newHeight);
                width = newWidth;
                height = newHeight;
            }
            
            EditorGUI.PropertyField(currentRect, originProp, new GUIContent("Origin"));
            currentRect.y += EditorGUI.GetPropertyHeight(originProp) + Spacing;

            EditorGUI.PropertyField(currentRect, cellSizeProp, new GUIContent("Cell Size"));
            currentRect.y += EditorGUI.GetPropertyHeight(cellSizeProp) + Spacing;

            EditorGUI.PropertyField(currentRect, cellSpacingProp, new GUIContent("Cell Spacing"));
            currentRect.y += EditorGUI.GetPropertyHeight(cellSpacingProp) + Spacing;

            SerializedProperty orientationProp = property.FindPropertyRelative("orientation");
            EditorGUI.PropertyField(currentRect, orientationProp, new GUIContent("Orientation"));
            currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;
            
            EditorGUI.LabelField(currentRect, $"Active Cells: {activeCount} / {width * height}");
            currentRect.y += EditorGUIUtility.singleLineHeight + Spacing;

            float gridWidth = width * CellSize;
            float gridHeight = height * CellSize;
            Rect gridRect = new Rect(currentRect.x + (currentRect.width - gridWidth) / 2, currentRect.y, gridWidth, gridHeight);

            DrawGrid(gridRect, width, height, cellsProp);

            currentRect.y += gridHeight + Spacing;

            Rect buttonRect = new Rect(currentRect.x, currentRect.y, currentRect.width / 3 - 5, ButtonHeight);

            if (GUI.Button(buttonRect, "Activate All"))
            {
                SetAllCells(cellsProp, true);
            }

            buttonRect.x += currentRect.width / 3 + 5;
            if (GUI.Button(buttonRect, "Deactivate All"))
            {
                SetAllCells(cellsProp, false);
            }

            buttonRect.x += currentRect.width / 3 + 5;
            if (GUI.Button(buttonRect, "Invert Cells"))
            {
                InvertGrid(cellsProp);
            }
            
            buttonRect.x = currentRect.x;
            buttonRect.y += ButtonHeight + Spacing;
            if (GUI.Button(buttonRect, "Flip Vertically"))
            {
                FlipGridVertically(cellsProp, width, height);
            }
            
            buttonRect.x += currentRect.width / 3 + 5;
            if (GUI.Button(buttonRect, "Flip Horizontally"))
            {
                FlipGridHorizontally(cellsProp, width, height);
            }

            EditorGUI.EndProperty();
        }

        private void DrawGrid(Rect gridRect, int width, int height, SerializedProperty cellsProp)
        {
            Event e = Event.current;

            EditorGUI.DrawRect(gridRect, BackgroundColor);
            
            int expectedSize = width * height;
            if (cellsProp.arraySize != expectedSize)
            {
                EditorGUI.LabelField(gridRect, "Multi-object editing not supported for grids with different sizes", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (e.type == EventType.MouseDown && gridRect.Contains(e.mousePosition))
            {
                int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellSize);
                int visualY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);
                int y = height - 1 - visualY;

                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    _isDragging = true;
                    int index = y * width + x;
                    if (index < cellsProp.arraySize)
                    {
                        _dragState = !cellsProp.GetArrayElementAtIndex(index).boolValue;
                        cellsProp.GetArrayElementAtIndex(index).boolValue = _dragState;
                        cellsProp.serializedObject.ApplyModifiedProperties();
                        GUI.changed = true;
                    }
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && _isDragging && gridRect.Contains(e.mousePosition))
            {
                int x = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellSize);
                int visualY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellSize);
                int y = height - 1 - visualY;

                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    int index = y * width + x;
                    if (index < cellsProp.arraySize && cellsProp.GetArrayElementAtIndex(index).boolValue != _dragState)
                    {
                        cellsProp.GetArrayElementAtIndex(index).boolValue = _dragState;
                        cellsProp.serializedObject.ApplyModifiedProperties();
                        GUI.changed = true;
                    }
                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp)
            {
                _isDragging = false;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    
                    if (index >= cellsProp.arraySize)
                        continue;

                    bool isActive = cellsProp.GetArrayElementAtIndex(index).boolValue;

                    int visualY = height - 1 - y;
                    Rect cellRect = new Rect(
                        gridRect.x + x * CellSize,
                        gridRect.y + visualY * CellSize,
                        CellSize - CellBorder,
                        CellSize - CellBorder
                    );

                    Color cellColor = isActive ? ActiveTileColor : InactiveTileColor;
                    EditorGUI.DrawRect(cellRect, cellColor);

                    if (cellRect.Contains(Event.current.mousePosition))
                    {
                        EditorGUI.DrawRect(cellRect, HoverColor);
                    }
                }
            }
            
            Handles.color = GridLineColor;
            for (int x = 0; x <= width; x++)
            {
                float xPos = gridRect.x + x * CellSize;
                Handles.DrawLine(new Vector3(xPos, gridRect.y), new Vector3(xPos, gridRect.y + gridRect.height));
            }
            for (int y = 0; y <= height; y++)
            {
                float yPos = gridRect.y + y * CellSize;
                Handles.DrawLine(new Vector3(gridRect.x, yPos), new Vector3(gridRect.x + gridRect.width, yPos));
            }

            if (gridRect.Contains(Event.current.mousePosition))
            {
                HandleUtility.Repaint();
            }
        }

        private void ResizeGrid(SerializedProperty property, int newWidth, int newHeight)
        {
            SerializedProperty sizeProp = property.FindPropertyRelative("size");
            SerializedProperty cellsProp = property.FindPropertyRelative("cells");

            Vector2Int oldSize = sizeProp.vector2IntValue;
            int oldWidth = oldSize.x;
            int oldHeight = oldSize.y;

            bool[] newCells = new bool[newWidth * newHeight];

            for (int y = 0; y < Mathf.Min(oldHeight, newHeight); y++)
            {
                for (int x = 0; x < Mathf.Min(oldWidth, newWidth); x++)
                {
                    int oldIndex = y * oldWidth + x;
                    int newIndex = y * newWidth + x;
                    if (oldIndex < cellsProp.arraySize)
                    {
                        newCells[newIndex] = cellsProp.GetArrayElementAtIndex(oldIndex).boolValue;
                    }
                }
            }

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    if (x >= oldWidth || y >= oldHeight)
                    {
                        newCells[y * newWidth + x] = true;
                    }
                }
            }

            sizeProp.vector2IntValue = new Vector2Int(newWidth, newHeight);
            cellsProp.arraySize = newWidth * newHeight;

            for (int i = 0; i < newCells.Length; i++)
            {
                cellsProp.GetArrayElementAtIndex(i).boolValue = newCells[i];
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private void SetAllCells(SerializedProperty cellsProp, bool value)
        {
            for (int i = 0; i < cellsProp.arraySize; i++)
            {
                cellsProp.GetArrayElementAtIndex(i).boolValue = value;
            }
            cellsProp.serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }

        private void InvertGrid(SerializedProperty cellsProp)
        {
            for (int i = 0; i < cellsProp.arraySize; i++)
            {
                SerializedProperty element = cellsProp.GetArrayElementAtIndex(i);
                element.boolValue = !element.boolValue;
            }
            cellsProp.serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }
        
        private void FlipGridVertically(SerializedProperty cellsProp, int width, int height)
        {
            bool[] newCells = new bool[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldIndex = y * width + x;
                    int newIndex = (height - 1 - y) * width + x;
                    if (oldIndex < cellsProp.arraySize)
                    {
                        newCells[newIndex] = cellsProp.GetArrayElementAtIndex(oldIndex).boolValue;
                    }
                }
            }

            for (int i = 0; i < newCells.Length; i++)
            {
                cellsProp.GetArrayElementAtIndex(i).boolValue = newCells[i];
            }

            cellsProp.serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }
        
        private void FlipGridHorizontally(SerializedProperty cellsProp, int width, int height)
        {
            bool[] newCells = new bool[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldIndex = y * width + x;
                    int newIndex = y * width + (width - 1 - x);
                    if (oldIndex < cellsProp.arraySize)
                    {
                        newCells[newIndex] = cellsProp.GetArrayElementAtIndex(oldIndex).boolValue;
                    }
                }
            }

            for (int i = 0; i < newCells.Length; i++)
            {
                cellsProp.GetArrayElementAtIndex(i).boolValue = newCells[i];
            }

            cellsProp.serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }

        private int GetActiveCount(SerializedProperty cellsProp)
        {
            int count = 0;
            for (int i = 0; i < cellsProp.arraySize; i++)
            {
                if (cellsProp.GetArrayElementAtIndex(i).boolValue)
                    count++;
            }
            return count;
        }
    }
}

#endif