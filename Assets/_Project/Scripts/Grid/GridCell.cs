using UnityEngine;

namespace NeonSerpent.Grid
{
    /// <summary>
    /// Value-type representation of a single grid cell.
    /// Deliberately a struct (not MonoBehaviour) for cache-friendly iteration.
    /// </summary>
    public struct GridCell
    {
        public Vector2Int Position;
        public GridCellType Type;

        public GridCell(Vector2Int position, GridCellType type)
        {
            Position = position;
            Type     = type;
        }
    }

    /// <summary>What a grid cell currently contains.</summary>
    public enum GridCellType
    {
        Empty,
        Snake,
        Food,
        PowerUp,
        Hazard,
        Wall
    }
}
