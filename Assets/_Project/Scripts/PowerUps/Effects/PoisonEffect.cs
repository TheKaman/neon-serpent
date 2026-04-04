using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>
    /// Slows the snake to half speed and zeroes out the next food score for 4 seconds.
    /// Applied automatically when the snake eats poison food.
    /// </summary>
    public class PoisonEffect : PowerUpEffect
    {
        private const float SPEED_MULT       = 0.5f;
        private const int   ZEROED_EAT_COUNT = 2;   // zeroes 2 eats instead of 1 for a more meaningful penalty

        public PoisonEffect()
        {
            Type     = PowerUpType.Poison;
            Duration = 6f;  // increased from 4s — flat 4s was negligible in World 3-4
        }

        public override void Apply(SnakeController snake, ScoreManager score)
        {
            snake.ApplySpeedMultiplier(SPEED_MULT);
            score.SetPoisonEatsRemaining(ZEROED_EAT_COUNT);
        }

        public override void Remove(SnakeController snake, ScoreManager score)
        {
            snake.RemoveSpeedMultiplier(SPEED_MULT);
            score.SetPoisonEatsRemaining(0);
        }
    }
}
