using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>
    /// Grants one absorbed collision (wall or self).
    /// Duration is indefinite — the shield expires when it absorbs a hit,
    /// which SnakeController handles internally via IsShielded flag.
    /// </summary>
    public class ShieldEffect : PowerUpEffect
    {
        public ShieldEffect()
        {
            Type     = PowerUpType.Shield;
            Duration = 0f; // not time-based; consumed on next fatal hit
        }

        public override void Apply(SnakeController snake, ScoreManager score) =>
            snake.IsShielded = true;

        public override void Remove(SnakeController snake, ScoreManager score) =>
            snake.IsShielded = false;
    }
}
