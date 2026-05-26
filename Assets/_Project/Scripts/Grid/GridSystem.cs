using System;
using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Grid
{
    /// <summary>
    /// The authority on grid state. All systems (snake, food, power-ups, hazards)
    /// must call SetCell() to register their presence and query this system for collisions.
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private int _width  = Constants.DEFAULT_GRID_WIDTH;
        [SerializeField] private int _height = Constants.DEFAULT_GRID_HEIGHT;

        private GridCell[,] _cells;

        public int Width  => _width;
        public int Height => _height;

        private void Awake()
        {
            InitializeGrid(_width, _height);
        }

        /// <summary>Initialize or re-initialize the grid (called by LevelManager when loading a level).</summary>
        public void InitializeGrid(int width, int height)
        {
            _width  = width;
            _height = height;
            _cells  = new GridCell[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y] = new GridCell(new Vector2Int(x, y), GridCellType.Empty);
        }

        /// <summary>Place wall cells from a level definition.</summary>
        public void SetWalls(Vector2Int[] wallPositions)
        {
            foreach (var pos in wallPositions)
                if (IsInBounds(pos))
                    _cells[pos.x, pos.y] = new GridCell(pos, GridCellType.Wall);
        }

        /// <summary>Returns true if the position is within grid bounds.</summary>
        public bool IsInBounds(Vector2Int pos) =>
            pos.x >= 0 && pos.x < _width && pos.y >= 0 && pos.y < _height;

        /// <summary>Returns true if the cell is occupied by anything other than Empty.</summary>
        public bool IsOccupied(Vector2Int pos) =>
            IsInBounds(pos) && _cells[pos.x, pos.y].Type != GridCellType.Empty;

        /// <summary>Returns the cell type at the given position. Returns Wall for out-of-bounds.</summary>
        public GridCellType GetCellType(Vector2Int pos) =>
            IsInBounds(pos) ? _cells[pos.x, pos.y].Type : GridCellType.Wall;

        /// <summary>Set the type of a cell.</summary>
        public void SetCell(Vector2Int pos, GridCellType type)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"[GridSystem] SetCell out of bounds: {pos}");
                return;
            }
            _cells[pos.x, pos.y] = new GridCell(pos, type);
        }

        /// <summary>Clear a cell back to Empty.</summary>
        public void ClearCell(Vector2Int pos) => SetCell(pos, GridCellType.Empty);

        /// <summary>
        /// Returns a random empty cell. Uses random probing first for O(1) average cost on
        /// sparse grids. Falls back to a full linear sweep when the grid is nearly full so
        /// spawners never softlock on a large snake.
        /// Returns Vector2Int(-1,-1) only when every cell is occupied.
        /// </summary>
        public Vector2Int GetRandomEmptyCell()
        {
            // Random probe pass — fast on typical grids
            int attempts = _width * _height;
            while (attempts-- > 0)
            {
                var pos = new Vector2Int(UnityEngine.Random.Range(0, _width), UnityEngine.Random.Range(0, _height));
                if (_cells[pos.x, pos.y].Type == GridCellType.Empty)
                    return pos;
            }

            // Random probing exhausted — do a deterministic linear sweep so we never miss
            // an empty cell on a nearly-full grid (prevents food-spawn softlock).
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_cells[x, y].Type == GridCellType.Empty)
                        return new Vector2Int(x, y);
                }
            }

            Debug.LogWarning("[GridSystem] GetRandomEmptyCell: no empty cell found — grid is completely full.");
            return new Vector2Int(-1, -1);
        }

        /// <summary>Convert a grid position to a world-space center point.</summary>
        public Vector3 GridToWorld(Vector2Int pos) =>
            new Vector3(pos.x * Constants.CELL_SIZE, pos.y * Constants.CELL_SIZE, 0f);

        /// <summary>Convert a world position to the nearest grid coordinate.</summary>
        public Vector2Int WorldToGrid(Vector3 worldPos) =>
            new Vector2Int(Mathf.RoundToInt(worldPos.x / Constants.CELL_SIZE),
                           Mathf.RoundToInt(worldPos.y / Constants.CELL_SIZE));

        /// <summary>
        /// Returns <paramref name="preferred"/> if it is in bounds and not a wall cell.
        /// Otherwise performs a spiral search outward from <paramref name="preferred"/>
        /// until a valid (in-bounds, non-wall, non-snake) cell is found.
        /// The snake needs at least <paramref name="snakeLength"/> consecutive horizontal
        /// cells (facing right) to spawn — this method checks that full run.
        /// Returns <paramref name="preferred"/> unchanged if validation cannot be performed
        /// (grid not initialised).
        /// </summary>
        /// <param name="preferred">The desired start position (grid coordinates).</param>
        /// <param name="snakeLength">Number of body segments to validate space for.</param>
        public Vector2Int GetValidatedStartPosition(Vector2Int preferred, int snakeLength = 3)
        {
            if (_cells == null) return preferred;

            // Check whether the full spawn run (head at preferred, tail extends left) is clear.
            if (IsSpawnRunClear(preferred, snakeLength))
                return preferred;

            // Spiral outward in expanding Manhattan rings until a clear run is found.
            // Cap at half the shorter dimension to avoid an infinite loop on tiny grids.
            int maxRadius = Mathf.Min(_width, _height) / 2;
            for (int r = 1; r <= maxRadius; r++)
            {
                // Walk the perimeter of the square ring at radius r around preferred.
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // inner cells
                        var candidate = new Vector2Int(preferred.x + dx, preferred.y + dy);
                        if (IsSpawnRunClear(candidate, snakeLength))
                            return candidate;
                    }
                }
            }

            // Absolute fallback: return the preferred position and log a warning.
            Debug.LogWarning($"[GridSystem] GetValidatedStartPosition: could not find a clear spawn " +
                $"run for snake length {snakeLength} — using preferred position {preferred}.");
            return preferred;
        }

        /// <summary>
        /// Returns true if <paramref name="headPos"/> and the <paramref name="length"/>-1
        /// cells extending left of it are all in bounds and free of Wall cells.
        /// </summary>
        private bool IsSpawnRunClear(Vector2Int headPos, int length)
        {
            for (int i = 0; i < length; i++)
            {
                var cell = new Vector2Int(headPos.x - i, headPos.y);
                if (!IsInBounds(cell)) return false;
                GridCellType t = _cells[cell.x, cell.y].Type;
                if (t == GridCellType.Wall) return false;
            }
            return true;
        }
    }
}
