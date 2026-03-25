using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>Instantly removes 3 tail segments. No timed removal needed.</summary>
    public class ShrinkEffect : PowerUpEffect
    {
        private const int SEGMENTS_TO_REMOVE = 3;

        public ShrinkEffect()
        {
            Type     = PowerUpType.ShrinkPill;
            Duration = 0f; // instant
        }

        public override void Apply(SnakeController snake, ScoreManager score) =>
            snake.Shrink(SEGMENTS_TO_REMOVE);

        public override void Remove(SnakeController snake, ScoreManager score)
        {
            // Instant effect — nothing to remove
        }
    }
}
