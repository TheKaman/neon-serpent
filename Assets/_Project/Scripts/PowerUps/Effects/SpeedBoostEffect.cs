using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>
    /// Doubles snake movement speed for 5 seconds and adds a flat +5 pts per food eaten
    /// while active. The scoring bonus means SpeedBoost remains rewarding even in late
    /// worlds where the doubled speed would otherwise be a liability.
    /// </summary>
    public class SpeedBoostEffect : PowerUpEffect
    {
        private const float MULTIPLIER    = 2f;
        private const int   BONUS_PER_EAT = 5;

        public SpeedBoostEffect()
        {
            Type     = PowerUpType.SpeedBoost;
            Duration = 5f;
        }

        public override void Apply(SnakeController snake, ScoreManager score)
        {
            snake.ApplySpeedMultiplier(MULTIPLIER);
            score.SetSpeedBoostBonus(BONUS_PER_EAT);
        }

        public override void Remove(SnakeController snake, ScoreManager score)
        {
            snake.RemoveSpeedMultiplier(MULTIPLIER);
            score.SetSpeedBoostBonus(0);
        }
    }
}
