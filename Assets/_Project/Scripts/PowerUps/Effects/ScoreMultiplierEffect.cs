using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>Doubles all score gains for 8 seconds.</summary>
    public class ScoreMultiplierEffect : PowerUpEffect
    {
        private const int MULTIPLIER = 2;

        public ScoreMultiplierEffect()
        {
            Type     = PowerUpType.ScoreMultiplier;
            Duration = 8f;
        }

        public override void Apply(SnakeController snake, ScoreManager score) =>
            score.SetMultiplier(MULTIPLIER);

        public override void Remove(SnakeController snake, ScoreManager score) =>
            score.SetMultiplier(1);
    }
}
