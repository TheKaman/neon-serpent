using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps
{
    /// <summary>
    /// Abstract base for all power-up effects.
    /// Every subclass MUST implement both Apply() and Remove() to avoid permanent state changes.
    /// Duration = 0 means instant (no timed removal needed).
    /// </summary>
    public abstract class PowerUpEffect
    {
        public PowerUpType Type     { get; protected set; }
        public float       Duration { get; protected set; }

        /// <summary>Apply this effect to the snake and/or score system.</summary>
        public abstract void Apply(SnakeController snake, ScoreManager score);

        /// <summary>Revert all changes made in Apply(). Must fully undo the effect.</summary>
        public abstract void Remove(SnakeController snake, ScoreManager score);

        /// <summary>Optional per-frame tick for effects with continuous behavior (e.g., DoT).</summary>
        public virtual void Tick(float deltaTime) { }
    }
}
