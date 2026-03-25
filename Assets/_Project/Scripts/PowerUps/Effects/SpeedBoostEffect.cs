using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>Doubles snake movement speed for 5 seconds.</summary>
    public class SpeedBoostEffect : PowerUpEffect
    {
        private const float MULTIPLIER = 2f;

        public SpeedBoostEffect()
        {
            Type     = PowerUpType.SpeedBoost;
            Duration = 5f;
        }

        public override void Apply(SnakeController snake, ScoreManager score) =>
            snake.ApplySpeedMultiplier(MULTIPLIER);

        public override void Remove(SnakeController snake, ScoreManager score) =>
            snake.RemoveSpeedMultiplier(MULTIPLIER);
    }
}
