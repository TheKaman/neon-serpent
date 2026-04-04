using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>Doubles all score gains for 8 seconds.</summary>
    public class ScoreMultiplierEffect : PowerUpEffect
    {
        private const int MULTIPLIER = 2;

        // Store the multiplier value that was active before Apply() so Remove()
        // restores the exact previous state rather than hardcoding 1.
        private int _previousMultiplier = 1;

        public ScoreMultiplierEffect()
        {
            Type     = PowerUpType.ScoreMultiplier;
            Duration = 8f;
        }

        public override void Apply(SnakeController snake, ScoreManager score)
        {
            _previousMultiplier = score.Multiplier;
            score.SetMultiplier(_previousMultiplier * MULTIPLIER);
        }

        public override void Remove(SnakeController snake, ScoreManager score) =>
            score.SetMultiplier(_previousMultiplier);
    }
}
