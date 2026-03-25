using UnityEngine;

namespace NeonSerpent.Snake
{
    /// <summary>
    /// Component on each snake segment prefab.
    /// Minimal data holder — visuals are driven by SnakeVisuals.
    /// </summary>
    public class SnakeSegment : MonoBehaviour
    {
        // Reserved for per-segment data (e.g., skin variant, animation state).
        // Currently a marker component used by the object pool.
    }
}
