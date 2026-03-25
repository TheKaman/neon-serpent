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
        private const float SPEED_MULT = 0.5f;

        public PoisonEffect()
        {
            Type     = PowerUpType.Poison;
            Duration = 4f;
        }

        public override void Apply(SnakeController snake, ScoreManager score)
        {
            snake.ApplySpeedMultiplier(SPEED_MULT);
            score.SetNextEatScoreZero(true);
        }

        public override void Remove(SnakeController snake, ScoreManager score)
        {
            snake.RemoveSpeedMultiplier(SPEED_MULT);
            score.SetNextEatScoreZero(false);
        }
    }
}
