using UnityEngine;

namespace NeonSerpent.Snake
{
    /// <summary>
    /// Component on each snake segment prefab.
    /// Minimal data holder — visuals are driven by SnakeVisuals.
    /// </summary>
    public class SnakeSegment : MonoBehaviour
    {
        /// <summary>Cached SpriteRenderer — avoids GetComponent calls on every move tick.</summary>
        public SpriteRenderer Renderer { get; private set; }

        private void Awake()
        {
            Renderer = GetComponent<SpriteRenderer>();
        }
    }
}
