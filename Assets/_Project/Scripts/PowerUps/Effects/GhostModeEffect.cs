using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps.Effects
{
    /// <summary>Lets the snake pass through its own body for 6 seconds.</summary>
    public class GhostModeEffect : PowerUpEffect
    {
        public GhostModeEffect()
        {
            Type     = PowerUpType.GhostMode;
            Duration = 6f;
        }

        public override void Apply(SnakeController snake, ScoreManager score)
        {
            snake.IsGhost = true;
            // Visual transparency is handled by SnakeVisuals.SetGhostMode()
            // which PowerUpManager calls after Apply()
        }

        public override void Remove(SnakeController snake, ScoreManager score)
        {
            snake.IsGhost = false;
        }
    }
}
