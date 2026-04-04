using UnityEngine;
using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>
    /// Instantly removes tail segments. Scales with snake length:
    /// removes max(3, 20% of current length) so it remains useful whether the
    /// snake is 6 segments long or 30.
    /// </summary>
    public class ShrinkEffect : PowerUpEffect
    {
        private const int   MIN_SEGMENTS   = 3;
        private const float LENGTH_PERCENT = 0.20f; // 20 % of current length

        public ShrinkEffect()
        {
            Type     = PowerUpType.ShrinkPill;
            Duration = 0f; // instant
        }

        public override void Apply(SnakeController snake, ScoreManager score)
        {
            int segments = Mathf.Max(MIN_SEGMENTS, Mathf.FloorToInt(snake.Length * LENGTH_PERCENT));
            snake.Shrink(segments);
        }

        public override void Remove(SnakeController snake, ScoreManager score)
        {
            // Instant effect — nothing to remove
        }
    }
}
