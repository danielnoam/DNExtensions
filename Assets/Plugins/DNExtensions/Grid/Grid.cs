using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DNExtensions.GridSystem
{
    [System.Serializable]
    public class Grid
    {
        public Vector2Int size;
        public Vector3 origin;
        public Vector3 cellSize;
        public Vector3 cellSpacing;
        public bool[] cells;
        [SerializeReference] public CoordinateConverter coordinateConverter;
        
        public int Width => size.x;
        public int Height => size.y;
        public int TotalCellCount => Width * Height;
        public int ActiveCellCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i]) count++;
                }
                return count;
            }
        }
        
        /// <summary>
        /// Creates a new grid with default values (8x8, vertical orientation).
        /// </summary>
        /// <param name="width">The width of the grid in cells (default: 8)</param>
        /// <param name="height">The height of the grid in cells (default: 8)</param>
        public Grid(int width = 8, int height = 8)
        {
            size = new Vector2Int(width, height);
            origin =  Vector3.zero;
            cellSize = new Vector3(1f, 1f, 0);
            cellSpacing = new Vector3(0.1f, 0.1f, 0);
            coordinateConverter = new VerticalConvertor();
            
            InitializeCells();
        }
        
        /// <summary>
        /// Creates a new grid with custom parameters.
        /// </summary>
        /// <param name="width">The width of the grid in cells</param>
        /// <param name="height">The height of the grid in cells</param>
        /// <param name="origin">The world space origin point of the grid</param>
        /// <param name="cellSize">The size of each cell in world units</param>
        /// <param name="cellSpacing">The spacing between cells in world units</param>
        /// <param name="coordinateConverter">The coordinate converter to use (defaults to VerticalConvertor if null)</param>
        public Grid(int width, int height, Vector3 origin, Vector3 cellSize, Vector3 cellSpacing, CoordinateConverter coordinateConverter)
        {
            size = new Vector2Int(width, height);
            this.origin = origin;
            this.cellSize = cellSize;
            this.cellSpacing = cellSpacing;
            this.coordinateConverter = coordinateConverter ?? new VerticalConvertor();
            InitializeCells();
        }

        private void InitializeCells()
        {
            cells = new bool[TotalCellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = true;
            }
        }
        
        /// <summary>
        /// Sets the active state of a cell at the specified grid coordinates.
        /// </summary>
        /// <param name="x">The x coordinate of the cell</param>
        /// <param name="y">The y coordinate of the cell</param>
        /// <param name="active">The active state to set</param>
        public void SetCellActive(int x, int y, bool active)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            cells[y * Width + x] = active;
        }

        /// <summary>
        /// Activates a cell at the specified grid coordinates.
        /// </summary>
        /// <param name="x">The x coordinate of the cell</param>
        /// <param name="y">The y coordinate of the cell</param>
        public void ActivateCell(int x, int y)
        {
            SetCellActive(x, y, true);
        }

        /// <summary>
        /// Deactivates a cell at the specified grid coordinates.
        /// </summary>
        /// <param name="x">The x coordinate of the cell</param>
        /// <param name="y">The y coordinate of the cell</param>
        public void DeactivateTile(int x, int y)
        {
            SetCellActive(x, y, false);
        }

        /// <summary>
        /// Toggles the active state of a cell at the specified grid coordinates.
        /// </summary>
        /// <param name="x">The x coordinate of the cell</param>
        /// <param name="y">The y coordinate of the cell</param>
        public void ToggleCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            cells[y * Width + x] = !cells[y * Width + x];
        }

        /// <summary>
        /// Resizes the grid to new dimensions, preserving existing cell states where possible.
        /// New cells are initialized as active.
        /// </summary>
        /// <param name="newWidth">The new width of the grid in cells</param>
        /// <param name="newHeight">The new height of the grid in cells</param>
        public void Resize(int newWidth, int newHeight)
        {
            bool[] newCells = new bool[newWidth * newHeight];
            
            for (int y = 0; y < Mathf.Min(Height, newHeight); y++)
            {
                for (int x = 0; x < Mathf.Min(Width, newWidth); x++)
                {
                    newCells[y * newWidth + x] = cells[y * Width + x];
                }
            }

            // Fill new cells with active state
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    if (x >= Width || y >= Height)
                    {
                        newCells[y * newWidth + x] = true;
                    }
                }
            }
            
            size = new Vector2Int(newWidth, newHeight);
            cells = newCells;
        }

        /// <summary>
        /// Activates all cells in the grid.
        /// </summary>
        public void ActivateAll()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = true;
            }
        }

        /// <summary>
        /// Deactivates all cells in the grid.
        /// </summary>
        public void DeactivateAll()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = false;
            }
        }

        /// <summary>
        /// Inverts the active state of all cells in the grid.
        /// </summary>
        public void InvertAll()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = !cells[i];
            }
        }

        /// <summary>
        /// Gets an array of grid positions for all currently active cells.
        /// </summary>
        /// <returns>An array of Vector2Int positions representing active cells</returns>
        public Vector2Int[] GetActiveCellsPositions()
        {
            int count = ActiveCellCount;
            Vector2Int[] activeTiles = new Vector2Int[count];
            int index = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (IsCellActive(x, y))
                    {
                        activeTiles[index] = new Vector2Int(x, y);
                        index++;
                    }
                }
            }

            return activeTiles;
        }
        
        /// <summary>
        /// Converts a world position to grid coordinates.
        /// </summary>
        /// <param name="position">The world position to convert</param>
        /// <returns>The grid coordinates, or (-1, -1) if the position is outside the grid bounds</returns>
        public Vector2Int GetCell(Vector3 position)
        {
            Vector2Int gridPos = coordinateConverter.WorldToGrid(position, size, cellSize, cellSpacing, origin);
            
            if (gridPos.x < 0 || gridPos.x >= Width || gridPos.y < 0 || gridPos.y >= Height)
            {
                return new Vector2Int(-1, -1);
            }
        
            return gridPos;
        }
        
        /// <summary>
        /// Gets the world position at the center of a cell.
        /// </summary>
        /// <param name="x">The x coordinate of the cell</param>
        /// <param name="y">The y coordinate of the cell</param>
        /// <returns>The world position at the center of the cell</returns>
        public Vector3 GetCellWorldPosition(int x, int y)
        {
            return coordinateConverter.GridToWorldCenter(
                new Vector2Int(x, y), 
                size, 
                cellSize, 
                cellSpacing, 
                origin
            );
        }

        /// <summary>
        /// Gets the world position at the center of a cell.
        /// </summary>
        /// <param name="position">The grid coordinates of the cell</param>
        /// <returns>The world position at the center of the cell</returns>
        public Vector3 GetCellWorldPosition(Vector2Int position)
        {
            
            return coordinateConverter.GridToWorldCenter(
                position, 
                size, 
                cellSize, 
                cellSpacing, 
                origin
            );
        }
        
        /// <summary>
        /// Gets the neighboring cell in the specified direction.
        /// </summary>
        /// <param name="tile">The starting cell position</param>
        /// <param name="direction">The direction vector to the neighbor</param>
        /// <returns>The neighboring cell position, or (-1, -1) if the neighbor is outside grid bounds</returns>
        public Vector2Int GetNeighboringCell(Vector2Int tile, Vector2Int direction)
        {
            Vector2Int neighboringTile = tile + direction;

            if (neighboringTile.x < 0 || neighboringTile.x >= Width || neighboringTile.y < 0 || neighboringTile.y >= Height)
            {
                return new Vector2Int(-1, -1);
            }

            return neighboringTile;
        }
        
        /// <summary>
        /// Checks if a cell is touching any edge of the grid and returns the edge directions.
        /// </summary>
        /// <param name="tile">The cell position to check</param>
        /// <param name="directions">Output list of edge directions this cell is touching</param>
        /// <returns>True if the cell is touching any edge, false otherwise</returns>
        public bool IsTouchingEdge(Vector2Int tile, out List<Vector2Int> directions)
        {
            directions = new List<Vector2Int>();

            // Main directions
            if (tile.x == 0)
            {
                directions.Add(Vector2Int.left);
            }
            if (tile.x == Width - 1)
            {
                directions.Add(Vector2Int.right);
            }
            if (tile.y == 0)
            {
                directions.Add(Vector2Int.down);
            }
            if (tile.y == Height - 1)
            {
                directions.Add(Vector2Int.up);
            }
            
            // Diagonals
            if (tile.x == Width - 1 && tile.y == Height - 1)
            {
                directions.Add(new Vector2Int(1,1));
            }
            if (tile.x == Width - 1 && tile.y == 0)
            {
                directions.Add(new Vector2Int(1, -1));
            }
            if (tile.x == 0 && tile.y == Height - 1)
            {
                directions.Add(new Vector2Int(-1, 1));
            }
            if (tile is { x: 0, y: 0 })
            {
                directions.Add(new Vector2Int(-1, -1));
            }

            return directions.Count > 0;

        }
        
        /// <summary>
        /// Checks if two cells are orthogonally adjacent (Manhattan distance of 1).
        /// </summary>
        /// <param name="tile1">The first cell position</param>
        /// <param name="tile2">The second cell position</param>
        /// <returns>True if the cells are neighbors, false otherwise</returns>
        public bool AreCellsNeighbors(Vector2Int tile1, Vector2Int tile2)
        {
            Vector2Int difference = tile1 - tile2;
            int manhattanDistance = Mathf.Abs(difference.x) + Mathf.Abs(difference.y);
        
            return manhattanDistance == 1;
        }
        
        /// <summary>
        /// Checks if a cell at the specified coordinates is active.
        /// </summary>
        /// <param name="x">The x coordinate of the cell</param>
        /// <param name="y">The y coordinate of the cell</param>
        /// <returns>True if the cell is active, false if inactive or out of bounds</returns>
        public bool IsCellActive(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;

            return cells[y * Width + x];
        }

        /// <summary>
        /// Draws the grid using Gizmos for visualization in the Unity editor.
        /// Active cells are drawn in green, inactive cells in white.
        /// </summary>
        public void DrawGrid()
        {
            coordinateConverter ??= new VerticalConvertor();
            
            var labelStyle = new GUIStyle()
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var tileState = IsCellActive(x, y);
                    var tilePosition = GetCellWorldPosition(x, y);
                
                    Gizmos.color = tileState ? Color.green : Color.white;
                    
                    Gizmos.matrix = Matrix4x4.TRS(tilePosition, Quaternion.LookRotation(coordinateConverter.Forward), Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, cellSize);
                    Gizmos.matrix = Matrix4x4.identity;
                
                    #if UNITY_EDITOR
                    Handles.Label(tilePosition, $"{x},{y}", labelStyle);
                    #endif
                }
            }
        }
    }
}